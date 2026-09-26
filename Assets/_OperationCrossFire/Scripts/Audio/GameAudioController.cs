using UnityEngine;

public class GameAudioController : MonoBehaviour
{
    const int FireVoiceCount = 2;
    const int PriorityVoiceCount = 2;

    [SerializeField] RoundManager _roundManager;
    [SerializeField] WeaponController _weaponController;
    [SerializeField] ShipHealth _shipHealth;
    [SerializeField] AudioClip _musicClip;
    [SerializeField] AudioClip _fireClip;
    [SerializeField] AudioClip _enemyDestroyedClip;
    [SerializeField] AudioClip _spaceshipHitClip;
    [SerializeField] AudioClip _gameOverClip;
    [SerializeField] AudioClip _victoryClip;
    [SerializeField] float _musicVolume = 0.35f;
    [SerializeField] float _fireVolume = 0.18f;
    [SerializeField] float _enemyDestroyedVolume = 0.45f;
    [SerializeField] float _spaceshipHitVolume = 0.65f;
    [SerializeField] float _gameOverVolume = 0.7f;
    [SerializeField] float _victoryVolume = 0.65f;
    [SerializeField] float _musicFadeInSeconds = 0.6f;
    [SerializeField] float _musicFadeOutSeconds = 0.4f;

    AudioSource _musicSource;
    AudioSource _gameOverSource;
    AudioSource _victorySource;
    AudioSource[] _fireVoices;
    AudioSource[] _priorityVoices;
    int _nextPriorityVoice;
    bool _musicStarted;
    bool _roundEnded;
    bool _resultCuePlayed;
    float _musicFadeStart;
    float _musicFadeFrom;
    float _musicFadeTo;
    float _musicFadeDuration;

    void Awake()
    {
        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(GameAudioController)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_weaponController == null)
        {
            Debug.LogError($"{nameof(GameAudioController)} requires a {nameof(WeaponController)} reference.", this);
        }

        if (_shipHealth == null)
        {
            Debug.LogError($"{nameof(GameAudioController)} requires a {nameof(ShipHealth)} reference.", this);
        }

        CreateSources();
    }

    void OnEnable()
    {
        if (_roundManager != null)
        {
            _roundManager.PhaseChanged += HandlePhaseChanged;
            _roundManager.RoundEnded += HandleRoundEnded;
        }

        if (_weaponController != null)
        {
            _weaponController.ShotFired += HandleShotFired;
        }

        PlayerProjectile.ThreatDestroyed += HandleThreatDestroyed;

        if (_shipHealth != null)
        {
            _shipHealth.HullLost += HandleHullLost;
        }

        if (_roundManager != null && _roundManager.IsRunning)
        {
            BeginMusic();
        }
    }

    void OnDisable()
    {
        if (_roundManager != null)
        {
            _roundManager.PhaseChanged -= HandlePhaseChanged;
            _roundManager.RoundEnded -= HandleRoundEnded;
        }

        if (_weaponController != null)
        {
            _weaponController.ShotFired -= HandleShotFired;
        }

        PlayerProjectile.ThreatDestroyed -= HandleThreatDestroyed;

        if (_shipHealth != null)
        {
            _shipHealth.HullLost -= HandleHullLost;
        }
    }

    void Update()
    {
        if (_musicSource == null || _musicFadeDuration <= 0f)
        {
            return;
        }

        float t = Mathf.Clamp01((Time.unscaledTime - _musicFadeStart) / _musicFadeDuration);
        _musicSource.volume = Mathf.Lerp(_musicFadeFrom, _musicFadeTo, t);
        if (t >= 1f)
        {
            _musicFadeDuration = 0f;
            if (_musicFadeTo <= 0f)
            {
                _musicSource.Stop();
            }
        }
    }

    void CreateSources()
    {
        _musicSource = CreateSource(loop: true, volume: 0f);
        _gameOverSource = CreateSource(loop: false, volume: _gameOverVolume);
        _victorySource = CreateSource(loop: false, volume: _victoryVolume);
        _fireVoices = CreateVoiceBank(FireVoiceCount);
        _priorityVoices = CreateVoiceBank(PriorityVoiceCount);
    }

    AudioSource CreateSource(bool loop, float volume)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.pitch = 1f;
        return source;
    }

    void HandlePhaseChanged(RoundPhase phase)
    {
        if (_roundManager != null && _roundManager.IsRunning)
        {
            BeginMusic();
        }
    }

    void BeginMusic()
    {
        if (_musicStarted || _musicSource == null || _musicClip == null)
        {
            return;
        }

        _musicStarted = true;
        _musicSource.clip = _musicClip;
        _musicSource.volume = 0f;
        _musicSource.Play();
        StartMusicFade(0f, _musicVolume, _musicFadeInSeconds);
    }

    void HandleShotFired()
    {
        if (_roundEnded)
        {
            return;
        }

        TryPlayFire(_fireClip, _fireVolume, 1f);
    }

    void HandleThreatDestroyed(Vector2 position, ThreatKind kind)
    {
        if (_roundEnded)
        {
            return;
        }

        float pitch = 1f;
        switch (kind)
        {
            case ThreatKind.Debris:
                pitch = 0.92f;
                break;
            case ThreatKind.Breach:
                pitch = 0.82f;
                break;
        }

        PlayPriority(_enemyDestroyedClip, _enemyDestroyedVolume, pitch);
    }

    void HandleHullLost(int hullRemaining)
    {
        if (_roundEnded || hullRemaining <= 0)
        {
            return;
        }

        PlayPriority(_spaceshipHitClip, _spaceshipHitVolume, 1f);
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        _roundEnded = true;
        StartMusicFade(_musicSource != null ? _musicSource.volume : 0f, 0f, _musicFadeOutSeconds);

        if (_resultCuePlayed)
        {
            return;
        }

        _resultCuePlayed = true;

        if (reason == RoundEndReason.Victory)
        {
            PlayVictory();
            return;
        }

        if (reason == RoundEndReason.HullDepleted || reason == RoundEndReason.BreachReached)
        {
            PlayGameOver();
        }
    }

    void PlayGameOver()
    {
        if (_gameOverSource == null || _gameOverClip == null)
        {
            return;
        }

        _gameOverSource.Stop();
        _gameOverSource.pitch = 1f;
        _gameOverSource.volume = _gameOverVolume;
        _gameOverSource.PlayOneShot(_gameOverClip, 1f);
    }

    void PlayVictory()
    {
        if (_victorySource == null || _victoryClip == null)
        {
            return;
        }

        _victorySource.Stop();
        _victorySource.pitch = 1f;
        _victorySource.volume = _victoryVolume;
        _victorySource.PlayOneShot(_victoryClip, 1f);
    }

    AudioSource[] CreateVoiceBank(int count)
    {
        AudioSource[] voices = new AudioSource[count];
        for (int i = 0; i < count; i++)
        {
            voices[i] = CreateSource(loop: false, volume: 1f);
        }

        return voices;
    }

    void TryPlayFire(AudioClip clip, float volume, float pitch)
    {
        if (clip == null || _fireVoices == null)
        {
            return;
        }

        for (int i = 0; i < _fireVoices.Length; i++)
        {
            AudioSource voice = _fireVoices[i];
            if (voice == null || voice.isPlaying)
            {
                continue;
            }

            voice.clip = clip;
            voice.pitch = pitch;
            voice.volume = volume;
            voice.Play();
            return;
        }
    }

    void PlayPriority(AudioClip clip, float volume, float pitch)
    {
        if (clip == null || _priorityVoices == null || _priorityVoices.Length == 0)
        {
            return;
        }

        AudioSource voice = _priorityVoices[_nextPriorityVoice];
        _nextPriorityVoice = (_nextPriorityVoice + 1) % _priorityVoices.Length;
        if (voice == null)
        {
            return;
        }

        voice.Stop();
        voice.clip = clip;
        voice.pitch = pitch;
        voice.volume = volume;
        voice.Play();
    }

    void StartMusicFade(float from, float to, float duration)
    {
        if (_musicSource == null)
        {
            return;
        }

        _musicFadeFrom = from;
        _musicFadeTo = to;
        _musicFadeDuration = Mathf.Max(0.01f, duration);
        _musicFadeStart = Time.unscaledTime;
        _musicSource.volume = from;
    }
}
