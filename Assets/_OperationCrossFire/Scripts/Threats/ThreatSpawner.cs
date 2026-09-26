using UnityEngine;

public class ThreatSpawner : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] PoolManager _poolManager;
    [SerializeField] Camera _gameplayCamera;
    [SerializeField] Transform _spawnPoint;
    [SerializeField] float _baseSpawnIntervalSeconds = 1.5f;
    [SerializeField] float _initialDelaySeconds = 0.8f;
    [SerializeField] float _baseDownwardSpeed = 2f;
    [SerializeField] float _horizontalCameraInset = 0.8f;

    bool _spawning;
    float _nextSpawnTime;
    bool _configurationValid;

    void Awake()
    {
        _configurationValid = ValidateConfiguration();
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

        Vector2 position = new Vector2(spawnX, _spawnPoint.position.y);
        threat.TryLaunch(position, _baseDownwardSpeed);
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

        if (_gameplayCamera == null)
        {
            return false;
        }

        float halfHeight = _gameplayCamera.orthographicSize;
        float halfWidth = halfHeight * _gameplayCamera.aspect;
        float centerX = _gameplayCamera.transform.position.x;
        float minX = centerX - halfWidth + _horizontalCameraInset;
        float maxX = centerX + halfWidth - _horizontalCameraInset;
        if (minX > maxX)
        {
            return false;
        }

        spawnX = Random.Range(minX, maxX);
        return true;
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

        if (_baseSpawnIntervalSeconds <= 0f || _initialDelaySeconds < 0f || _baseDownwardSpeed <= 0f)
        {
            Debug.LogError($"{nameof(ThreatSpawner)} spawn interval, delay, and base speed must be valid.", this);
            valid = false;
        }

        if (_horizontalCameraInset < 0f)
        {
            Debug.LogError($"{nameof(ThreatSpawner)} horizontal camera inset cannot be negative.", this);
            valid = false;
        }

        return valid;
    }
}
