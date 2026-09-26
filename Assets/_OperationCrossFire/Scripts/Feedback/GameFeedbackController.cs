using UnityEngine;

public class GameFeedbackController : MonoBehaviour
{
    const int BurstCapacity = 8;

    [SerializeField] RoundManager _roundManager;
    [SerializeField] WeaponController _weaponController;
    [SerializeField] ShipHealth _shipHealth;
    [SerializeField] BoostAbility _boostAbility;
    [SerializeField] ShieldAbility _shieldAbility;
    [SerializeField] Transform _firePoint;
    [SerializeField] SpriteRenderer _shipVisual;
    [SerializeField] SpriteRenderer _shieldRenderer;
    [SerializeField] RectTransform _scoreText;
    [SerializeField] RectTransform _hullPoint1;
    [SerializeField] RectTransform _hullPoint2;
    [SerializeField] RectTransform _hullPoint3;
    [SerializeField] string _effectsSortingLayer = "Effects";

    Transform _effectRoot;
    ParticleSystem[] _bursts;
    Material _burstMaterial;
    readonly ParticleSystem.Burst[] _burstEmit = { new ParticleSystem.Burst(0f, 12) };
    SpriteRenderer _muzzleFlash;
    Sprite _muzzleSprite;
    Color _shipBaseColor = Color.white;
    Color _shieldBaseColor = Color.white;
    bool _shipBaseCaptured;
    bool _roundEnded;
    float _muzzleUntil;
    float _shipFlashUntil;
    float _shieldFlashUntil;
    float _scorePulseUntil;
    float _hullPulseUntil;
    Vector3 _scoreBaseScale = Vector3.one;
    Vector3 _hull1BaseScale = Vector3.one;
    Vector3 _hull2BaseScale = Vector3.one;
    Vector3 _hull3BaseScale = Vector3.one;

    void Awake()
    {
        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(GameFeedbackController)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_weaponController == null)
        {
            Debug.LogError($"{nameof(GameFeedbackController)} requires a {nameof(WeaponController)} reference.", this);
        }

        if (_shipHealth == null)
        {
            Debug.LogError($"{nameof(GameFeedbackController)} requires a {nameof(ShipHealth)} reference.", this);
        }

        if (_boostAbility == null || _shieldAbility == null)
        {
            Debug.LogError($"{nameof(GameFeedbackController)} requires Boost and Shield ability references.", this);
        }

        if (_firePoint == null || _shipVisual == null)
        {
            Debug.LogError($"{nameof(GameFeedbackController)} requires FirePoint and ShipVisual SpriteRenderer.", this);
        }

        CaptureBaseScales();
        if (_shipVisual != null)
        {
            _shipBaseColor = _shipVisual.color;
            _shipBaseCaptured = true;
        }

        if (_shieldRenderer != null)
        {
            _shieldBaseColor = _shieldRenderer.color;
        }

        CreateMuzzleFlash();
        CreateBurstPool();
    }

    void OnEnable()
    {
        if (_roundManager != null)
        {
            _roundManager.RoundEnded += HandleRoundEnded;
            _roundManager.FluxStarting += HandleFluxStarting;
        }

        if (_weaponController != null)
        {
            _weaponController.ShotFired += HandleShotFired;
        }

        PlayerProjectile.ThreatDamaged += HandleThreatDamaged;
        PlayerProjectile.ThreatDestroyed += HandleThreatDestroyed;

        if (_shipHealth != null)
        {
            _shipHealth.HullLost += HandleHullLost;
            _shipHealth.ShieldIntercepted += HandleShieldCue;
        }

        if (_boostAbility != null)
        {
            _boostAbility.Activated += HandleBoostActivated;
        }

        if (_shieldAbility != null)
        {
            _shieldAbility.Activated += HandleShieldCue;
        }
    }

    void OnDisable()
    {
        if (_roundManager != null)
        {
            _roundManager.RoundEnded -= HandleRoundEnded;
            _roundManager.FluxStarting -= HandleFluxStarting;
        }

        if (_weaponController != null)
        {
            _weaponController.ShotFired -= HandleShotFired;
        }

        PlayerProjectile.ThreatDamaged -= HandleThreatDamaged;
        PlayerProjectile.ThreatDestroyed -= HandleThreatDestroyed;

        if (_shipHealth != null)
        {
            _shipHealth.HullLost -= HandleHullLost;
            _shipHealth.ShieldIntercepted -= HandleShieldCue;
        }

        if (_boostAbility != null)
        {
            _boostAbility.Activated -= HandleBoostActivated;
        }

        if (_shieldAbility != null)
        {
            _shieldAbility.Activated -= HandleShieldCue;
        }
    }

    void OnDestroy()
    {
        if (_muzzleFlash != null)
        {
            Destroy(_muzzleFlash.gameObject);
            _muzzleFlash = null;
        }

        if (_muzzleSprite != null)
        {
            Destroy(_muzzleSprite);
            _muzzleSprite = null;
        }

        if (_burstMaterial != null)
        {
            Destroy(_burstMaterial);
            _burstMaterial = null;
        }
    }

    void LateUpdate()
    {
        if (_muzzleFlash != null && _muzzleUntil > 0f && Time.time >= _muzzleUntil)
        {
            _muzzleFlash.enabled = false;
            _muzzleUntil = 0f;
        }

        if (_shipFlashUntil > 0f && Time.time >= _shipFlashUntil)
        {
            RestoreShipColor();
        }

        if (_shieldFlashUntil > 0f && Time.time >= _shieldFlashUntil)
        {
            RestoreShieldColor();
        }

        UpdatePulse(_scoreText, _scoreBaseScale, ref _scorePulseUntil, 0.18f, 1.12f);
        UpdateHullPointPulse();
    }

    void HandleShotFired()
    {
        if (_roundEnded || _muzzleFlash == null)
        {
            return;
        }

        _muzzleFlash.enabled = true;
        _muzzleUntil = Time.time + 0.07f;
    }

    void HandleThreatDamaged(FallingThreat threat)
    {
        if (_roundEnded || threat == null || !threat.IsInUse)
        {
            return;
        }

        threat.PlayHitFlash();
    }

    void HandleThreatDestroyed(Vector2 position, ThreatKind kind)
    {
        if (_roundEnded)
        {
            return;
        }

        PlayBurst(position, kind);
        _scorePulseUntil = Time.time + 0.18f;
    }

    void HandleHullLost(int _)
    {
        FlashShip(new Color(1f, 0.45f, 0.45f, 1f), 0.12f);
        _hullPulseUntil = Time.time + 0.14f;
    }

    void HandleBoostActivated()
    {
        FlashShip(new Color(0.35f, 0.95f, 1f, 1f), 0.2f);
    }

    void HandleShieldCue()
    {
        if (_shieldRenderer == null || !_shieldRenderer.gameObject.activeInHierarchy)
        {
            return;
        }

        _shieldRenderer.color = Color.Lerp(_shieldBaseColor, Color.white, 0.7f);
        _shieldFlashUntil = Time.time + 0.12f;
    }

    void HandleFluxStarting()
    {
        RestoreShieldColor();
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        _roundEnded = true;
        RestoreShieldColor();
        if (_muzzleFlash != null)
        {
            _muzzleFlash.enabled = false;
        }

        if (_bursts == null)
        {
            return;
        }

        for (int i = 0; i < _bursts.Length; i++)
        {
            if (_bursts[i] != null)
            {
                _bursts[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    void FlashShip(Color flashColor, float duration)
    {
        if (_shipVisual == null)
        {
            return;
        }

        if (!_shipBaseCaptured)
        {
            _shipBaseColor = _shipVisual.color;
            _shipBaseCaptured = true;
        }

        _shipVisual.color = flashColor;
        _shipFlashUntil = Time.time + duration;
    }

    void RestoreShipColor()
    {
        _shipFlashUntil = 0f;
        if (_shipVisual != null && _shipBaseCaptured)
        {
            _shipVisual.color = _shipBaseColor;
        }
    }

    void RestoreShieldColor()
    {
        _shieldFlashUntil = 0f;
        if (_shieldRenderer != null)
        {
            _shieldRenderer.color = _shieldBaseColor;
        }
    }

    void PlayBurst(Vector2 position, ThreatKind kind)
    {
        ParticleSystem burst = AcquireBurst();
        if (burst == null)
        {
            return;
        }

        Color colorA;
        Color colorB;
        int count;
        ResolveBurstStyle(kind, out colorA, out colorB, out count);

        var main = burst.main;
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        _burstEmit[0] = new ParticleSystem.Burst(0f, (short)count);
        var emission = burst.emission;
        emission.SetBursts(_burstEmit);

        burst.transform.position = new Vector3(position.x, position.y, 0f);
        burst.Clear();
        burst.Play();
    }

    ParticleSystem AcquireBurst()
    {
        if (_bursts == null)
        {
            return null;
        }

        for (int i = 0; i < _bursts.Length; i++)
        {
            ParticleSystem candidate = _bursts[i];
            if (candidate != null && !candidate.isPlaying)
            {
                return candidate;
            }
        }

        return null;
    }

    static void ResolveBurstStyle(ThreatKind kind, out Color colorA, out Color colorB, out int count)
    {
        switch (kind)
        {
            case ThreatKind.Debris:
                colorA = new Color(1f, 0.72f, 0.28f, 1f);
                colorB = new Color(0.95f, 0.45f, 0.12f, 1f);
                count = 10;
                break;
            case ThreatKind.Breach:
                colorA = new Color(1f, 0.2f, 0.18f, 1f);
                colorB = new Color(0.85f, 0.08f, 0.08f, 1f);
                count = 14;
                break;
            default:
                colorA = new Color(0.3f, 0.95f, 1f, 1f);
                colorB = new Color(1f, 0.25f, 0.25f, 1f);
                count = 12;
                break;
        }
    }

    void CreateMuzzleFlash()
    {
        if (_firePoint == null)
        {
            return;
        }

        GameObject flashObject = new GameObject("MuzzleFlash");
        flashObject.transform.SetParent(_firePoint, false);
        flashObject.transform.localPosition = Vector3.zero;
        flashObject.transform.localRotation = Quaternion.identity;
        flashObject.transform.localScale = new Vector3(0.38f, 0.22f, 1f);

        Texture2D white = Texture2D.whiteTexture;
        _muzzleSprite = Sprite.Create(
            white,
            new Rect(0f, 0f, white.width, white.height),
            new Vector2(0.5f, 0.5f),
            white.width);

        _muzzleFlash = flashObject.AddComponent<SpriteRenderer>();
        _muzzleFlash.sprite = _muzzleSprite;
        _muzzleFlash.color = new Color(0.45f, 0.95f, 1f, 0.9f);
        _muzzleFlash.sortingLayerName = _effectsSortingLayer;
        _muzzleFlash.sortingOrder = 8;
        _muzzleFlash.enabled = false;
    }

    void CreateBurstPool()
    {
        _effectRoot = new GameObject("FeedbackEffects").transform;
        _effectRoot.SetParent(transform, false);

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            _burstMaterial = new Material(shader);
        }

        _bursts = new ParticleSystem[BurstCapacity];
        for (int i = 0; i < BurstCapacity; i++)
        {
            _bursts[i] = CreateBurst($"KillBurst_{i}");
        }
    }

    ParticleSystem CreateBurst(string objectName)
    {
        GameObject burstObject = new GameObject(objectName);
        burstObject.transform.SetParent(_effectRoot, false);
        ParticleSystem system = burstObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.25f;
        main.startLifetime = 0.25f;
        main.startSpeed = 2.4f;
        main.startSize = 0.11f;
        main.maxParticles = 16;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var emission = system.emission;
        emission.rateOverTime = 0f;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        ParticleSystemRenderer renderer = burstObject.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = _effectsSortingLayer;
            renderer.sortingOrder = 6;
            if (_burstMaterial != null)
            {
                renderer.sharedMaterial = _burstMaterial;
            }
        }

        return system;
    }

    void CaptureBaseScales()
    {
        if (_scoreText != null)
        {
            _scoreBaseScale = _scoreText.localScale;
        }

        if (_hullPoint1 != null)
        {
            _hull1BaseScale = _hullPoint1.localScale;
        }

        if (_hullPoint2 != null)
        {
            _hull2BaseScale = _hullPoint2.localScale;
        }

        if (_hullPoint3 != null)
        {
            _hull3BaseScale = _hullPoint3.localScale;
        }
    }

    void UpdateHullPointPulse()
    {
        if (_hullPulseUntil <= 0f)
        {
            return;
        }

        float remaining = _hullPulseUntil - Time.time;
        if (remaining <= 0f)
        {
            _hullPulseUntil = 0f;
            SetHullPointScale(1f);
            return;
        }

        float t = 1f - (remaining / 0.14f);
        float scale = Mathf.Lerp(1f, 1.16f, Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
        SetHullPointScale(scale);
    }

    void SetHullPointScale(float scale)
    {
        if (_hullPoint1 != null)
        {
            _hullPoint1.localScale = _hull1BaseScale * scale;
        }

        if (_hullPoint2 != null)
        {
            _hullPoint2.localScale = _hull2BaseScale * scale;
        }

        if (_hullPoint3 != null)
        {
            _hullPoint3.localScale = _hull3BaseScale * scale;
        }
    }

    static void UpdatePulse(RectTransform target, Vector3 baseScale, ref float until, float duration, float peak)
    {
        if (target == null || until <= 0f)
        {
            return;
        }

        float remaining = until - Time.time;
        if (remaining <= 0f)
        {
            until = 0f;
            target.localScale = baseScale;
            return;
        }

        float t = 1f - (remaining / duration);
        float scale = Mathf.Lerp(1f, peak, Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
        target.localScale = baseScale * scale;
    }
}
