using UnityEngine;

public class ThreatSpawner : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] PoolManager _poolManager;
    [SerializeField] Camera _gameplayCamera;
    [SerializeField] Transform _spawnPoint;
    [SerializeField] Collider2D _breachBoundary;
    [SerializeField] Collider2D _cleanupBottom;
    [SerializeField] float _baseSpawnIntervalSeconds = 1.5f;
    [SerializeField] float _initialDelaySeconds = 0.8f;
    [SerializeField] float _baseDownwardSpeed = 2f;
    [SerializeField] float _horizontalCameraInset = 0.8f;
    [SerializeField] float _widestThreatHalfWidth = 0.35f;
    [SerializeField] int _recentHistoryCount = 3;
    [SerializeField] int _candidateAttempts = 6;
    [SerializeField] float _preferredSeparationFraction = 0.28f;

    const int HistoryCapacity = 3;
    const float SpreadScoreWeight = 0.35f;
    const float ScoreNoise = 0.12f;

    readonly float[] _recentSpawnX = new float[HistoryCapacity];
    int _recentCount;
    int _recentNext;

    bool _spawning;
    float _nextSpawnTime;
    bool _configurationValid;
    bool _spawnRangeValid;
    bool _loggedUnsafeRange;
    float _spawnMinX;
    float _spawnMaxX;
    float _offscreenFallbackY;
    int _lastPixelWidth = -1;
    int _lastPixelHeight = -1;
    float _lastAspect = -1f;
    float _lastOrthographicSize = -1f;

    void Awake()
    {
        _configurationValid = ValidateConfiguration();
        if (_configurationValid)
        {
            RefreshPlayfield(force: true);
        }

        ResetRecentPositions();
    }

    void OnEnable()
    {
        if (_roundManager == null)
        {
            return;
        }

        _roundManager.PhaseChanged += HandlePhaseChanged;
        _roundManager.RoundEnded += HandleRoundEnded;
    }

    void OnDisable()
    {
        if (_roundManager == null)
        {
            return;
        }

        _roundManager.PhaseChanged -= HandlePhaseChanged;
        _roundManager.RoundEnded -= HandleRoundEnded;
    }

    void Update()
    {
        if (!_configurationValid || !_spawning || _roundManager == null || !_roundManager.IsRunning)
        {
            return;
        }

        RefreshPlayfield(force: false);

        if (Time.time < _nextSpawnTime)
        {
            return;
        }

        TrySpawnOne();
        _nextSpawnTime = Time.time + CurrentSpawnInterval();
    }

    void HandlePhaseChanged(RoundPhase phase)
    {
        if (phase == RoundPhase.Patrol)
        {
            ResetRecentPositions();
            _spawning = true;
            _nextSpawnTime = Time.time + _initialDelaySeconds;
            return;
        }

        if (phase == RoundPhase.Ended)
        {
            _spawning = false;
        }
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        _spawning = false;
    }

    void TrySpawnOne()
    {
        if (_poolManager == null || _spawnPoint == null)
        {
            return;
        }

        if (!TryGetSpawnX(out float spawnX))
        {
            return;
        }

        ThreatKind kind = ChooseKind(_roundManager.CurrentPhase);
        if (!_poolManager.TryAcquireThreat(kind, out FallingThreat threat))
        {
            return;
        }

        threat.SetOffscreenFallback(_offscreenFallbackY);
        Vector2 position = new Vector2(spawnX, _spawnPoint.position.y);
        if (!threat.TryLaunch(position, _baseDownwardSpeed))
        {
            threat.ClearOffscreenFallback();
            return;
        }

        RecordSpawnX(spawnX);
    }

    ThreatKind ChooseKind(RoundPhase phase)
    {
        float roll = Random.Range(0f, 1f);

        if (phase == RoundPhase.Patrol)
        {
            return roll < 0.6f ? ThreatKind.Enemy : ThreatKind.Debris;
        }

        if (roll < 0.5f)
        {
            return ThreatKind.Enemy;
        }

        if (roll < 0.8f)
        {
            return ThreatKind.Debris;
        }

        return ThreatKind.Breach;
    }

    float CurrentSpawnInterval()
    {
        if (_roundManager != null && _roundManager.CurrentPhase == RoundPhase.Critical)
        {
            return _baseSpawnIntervalSeconds * 0.7f;
        }

        return _baseSpawnIntervalSeconds;
    }

    bool TryGetSpawnX(out float spawnX)
    {
        spawnX = 0f;
        if (!_spawnRangeValid)
        {
            return false;
        }

        float range = _spawnMaxX - _spawnMinX;
        if (range <= 0.0001f)
        {
            spawnX = _spawnMinX;
            return true;
        }

        if (_recentCount == 0)
        {
            spawnX = Random.Range(_spawnMinX, _spawnMaxX);
            return true;
        }

        int attempts = Mathf.Max(1, _candidateAttempts);
        float preferredSeparation = range * Mathf.Clamp01(_preferredSeparationFraction);
        float bestScore = float.NegativeInfinity;
        float bestX = Random.Range(_spawnMinX, _spawnMaxX);

        for (int i = 0; i < attempts; i++)
        {
            float candidate = Random.Range(_spawnMinX, _spawnMaxX);
            float score = ScoreCandidate(candidate, preferredSeparation, range);
            if (score > bestScore)
            {
                bestScore = score;
                bestX = candidate;
            }
        }

        spawnX = bestX;
        return true;
    }

    float ScoreCandidate(float candidateX, float preferredSeparation, float range)
    {
        float nearest = float.MaxValue;
        float total = 0f;
        for (int i = 0; i < _recentCount; i++)
        {
            float distance = Mathf.Abs(candidateX - _recentSpawnX[i]);
            if (distance < nearest)
            {
                nearest = distance;
            }

            total += distance;
        }

        float nearestScore = preferredSeparation <= 0.0001f
            ? 1f
            : Mathf.Clamp01(nearest / preferredSeparation);
        float spreadScore = Mathf.Clamp01((total / _recentCount) / range);
        return nearestScore + (spreadScore * SpreadScoreWeight) + Random.Range(0f, ScoreNoise);
    }

    void RecordSpawnX(float spawnX)
    {
        int capacity = Mathf.Clamp(_recentHistoryCount, 2, HistoryCapacity);
        _recentSpawnX[_recentNext] = spawnX;
        _recentNext++;
        if (_recentNext >= capacity)
        {
            _recentNext = 0;
        }

        if (_recentCount < capacity)
        {
            _recentCount++;
        }
    }

    void ResetRecentPositions()
    {
        _recentCount = 0;
        _recentNext = 0;
    }

    void RefreshPlayfield(bool force)
    {
        if (_gameplayCamera == null || _breachBoundary == null || _cleanupBottom == null)
        {
            _spawnRangeValid = false;
            return;
        }

        int pixelWidth = _gameplayCamera.pixelWidth;
        int pixelHeight = _gameplayCamera.pixelHeight;
        float aspect = _gameplayCamera.aspect;
        float orthographicSize = _gameplayCamera.orthographicSize;

        if (!force
            && pixelWidth == _lastPixelWidth
            && pixelHeight == _lastPixelHeight
            && Mathf.Approximately(aspect, _lastAspect)
            && Mathf.Approximately(orthographicSize, _lastOrthographicSize))
        {
            return;
        }

        _lastPixelWidth = pixelWidth;
        _lastPixelHeight = pixelHeight;
        _lastAspect = aspect;
        _lastOrthographicSize = orthographicSize;

        float halfHeight = orthographicSize;
        float halfWidth = halfHeight * aspect;
        float centerX = _gameplayCamera.transform.position.x;
        float cameraMinX = centerX - halfWidth + _horizontalCameraInset;
        float cameraMaxX = centerX + halfWidth - _horizontalCameraInset;

        Bounds breach = _breachBoundary.bounds;
        Bounds cleanup = _cleanupBottom.bounds;
        float halfThreat = Mathf.Max(0f, _widestThreatHalfWidth);

        float minX = Mathf.Max(cameraMinX, breach.min.x + halfThreat, cleanup.min.x + halfThreat);
        float maxX = Mathf.Min(cameraMaxX, breach.max.x - halfThreat, cleanup.max.x - halfThreat);

        if (minX > maxX)
        {
            _spawnRangeValid = false;
            if (!_loggedUnsafeRange)
            {
                _loggedUnsafeRange = true;
                Debug.LogError(
                    $"{nameof(ThreatSpawner)} spawn X range is empty. Widen BreachBoundary and CleanupBottom to cover the camera view, then confirm both colliders are assigned.",
                    this);
            }

            return;
        }

        _spawnMinX = minX;
        _spawnMaxX = maxX;
        _spawnRangeValid = true;

        float cameraBottom = _gameplayCamera.transform.position.y - halfHeight;
        _offscreenFallbackY = Mathf.Min(breach.min.y, cameraBottom);
    }

    bool ValidateConfiguration()
    {
        bool valid = true;

        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(ThreatSpawner)} requires a {nameof(RoundManager)} reference.", this);
            valid = false;
        }

        if (_poolManager == null)
        {
            Debug.LogError($"{nameof(ThreatSpawner)} requires a {nameof(PoolManager)} reference.", this);
            valid = false;
        }

        if (_gameplayCamera == null)
        {
            Debug.LogError($"{nameof(ThreatSpawner)} requires a gameplay {nameof(Camera)} reference.", this);
            valid = false;
        }
        else if (!_gameplayCamera.orthographic)
        {
            Debug.LogError($"{nameof(ThreatSpawner)} gameplay camera must be orthographic.", this);
            valid = false;
        }

        if (_spawnPoint == null)
        {
            Debug.LogError($"{nameof(ThreatSpawner)} requires a SpawnPoint transform reference.", this);
            valid = false;
        }

        if (_breachBoundary == null)
        {
            Debug.LogError($"{nameof(ThreatSpawner)} requires the BreachBoundary {nameof(Collider2D)}.", this);
            valid = false;
        }

        if (_cleanupBottom == null)
        {
            Debug.LogError($"{nameof(ThreatSpawner)} requires the CleanupBottom {nameof(Collider2D)}.", this);
            valid = false;
        }

        if (_baseSpawnIntervalSeconds <= 0f || _initialDelaySeconds < 0f || _baseDownwardSpeed <= 0f)
        {
            Debug.LogError($"{nameof(ThreatSpawner)} spawn interval, delay, and base speed must be valid.", this);
            valid = false;
        }

        if (_horizontalCameraInset < 0f || _widestThreatHalfWidth < 0f)
        {
            Debug.LogError($"{nameof(ThreatSpawner)} camera inset and widest-threat half-width cannot be negative.", this);
            valid = false;
        }

        if (_candidateAttempts < 1 || _recentHistoryCount < 2 || _preferredSeparationFraction < 0f)
        {
            Debug.LogError($"{nameof(ThreatSpawner)} candidate attempts, recent history, and separation fraction must be valid.", this);
            valid = false;
        }

        return valid;
    }
}
