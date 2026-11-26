using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("1. Música (En Bucle)")]
    public AudioClip normalMusic;
    public AudioClip bossMusic;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    public float musicFadeDuration = 1.2f;

    [Header("2. Efectos Puntuales (SFX)")]
    public AudioClip finalBossAlertSfx;
    public AudioClip deathSfx;
    public AudioClip powerUpSfx;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;

    [Header("3. Alerta de Poca Vida (En Bucle)")]
    public AudioClip lowHealthLoopSfx;
    [Range(0.05f, 0.5f)] public float lowHealthThreshold = 0.30f; // 30% de vida
    [Range(0f, 1f)] public float lowHealthVolume = 0.7f;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private AudioSource _lowHealthSource;

    private PlayerHealth _playerHealth;
    private ExperienceManager _experienceManager;
    private Coroutine _musicFadeRoutine;
    private int _activeBossesCount;
    private bool _isGameEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Crea los 3 canales de audio automáticamente
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.playOnAwake = false;
        _musicSource.volume = musicVolume;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop = false;
        _sfxSource.playOnAwake = false;
        _sfxSource.volume = sfxVolume;

        _lowHealthSource = gameObject.AddComponent<AudioSource>();
        _lowHealthSource.loop = true;
        _lowHealthSource.playOnAwake = false;
        _lowHealthSource.volume = lowHealthVolume;
    }

    private void Start()
    {
        _playerHealth = FindFirstObjectByType<PlayerHealth>();
        _experienceManager = FindFirstObjectByType<ExperienceManager>();

        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged += HandleHealthChanged;
            _playerHealth.OnPlayerDied += HandlePlayerDied;
        }

        if (_experienceManager != null)
        {
            _experienceManager.OnUpgradeApplied += PlayPowerUpSound;
        }

        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.OnBossSpawned += HandleBossSpawned;
            GameRunManager.Instance.OnFinalBossSpawned += HandleFinalBossSpawned;
            GameRunManager.Instance.OnGameWon += HandleGameWon;
        }

        EnemyController.OnBossKilled += HandleBossKilled;

        // Arranca la música normal al iniciar la run
        PlayMusic(normalMusic);
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged -= HandleHealthChanged;
            _playerHealth.OnPlayerDied -= HandlePlayerDied;
        }

        if (_experienceManager != null)
        {
            _experienceManager.OnUpgradeApplied -= PlayPowerUpSound;
        }

        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.OnBossSpawned -= HandleBossSpawned;
            GameRunManager.Instance.OnFinalBossSpawned -= HandleFinalBossSpawned;
            GameRunManager.Instance.OnGameWon -= HandleGameWon;
        }

        EnemyController.OnBossKilled -= HandleBossKilled;
    }

    private void HandleBossSpawned(string bossName)
    {
        if (_isGameEnded) return;

        _activeBossesCount++;
        if (bossMusic != null && _musicSource.clip != bossMusic)
        {
            PlayMusic(bossMusic);
        }
    }

    private void HandleFinalBossSpawned()
    {
        if (_isGameEnded) return;
        PlaySfx(finalBossAlertSfx);
    }

    private void HandleBossKilled()
    {
        if (_isGameEnded) return;

        _activeBossesCount = Mathf.Max(0, _activeBossesCount - 1);
        if (_activeBossesCount == 0 && normalMusic != null && _musicSource.clip != normalMusic)
        {
            PlayMusic(normalMusic);
        }
    }

    private void HandleHealthChanged(float currentHp, float maxHp)
    {
        if (_isGameEnded || lowHealthLoopSfx == null || maxHp <= 0f) return;

        float hpRatio = currentHp / maxHp;
        bool isLowHealth = currentHp > 0f && hpRatio <= lowHealthThreshold;

        if (isLowHealth)
        {
            if (!_lowHealthSource.isPlaying)
            {
                _lowHealthSource.clip = lowHealthLoopSfx;
                _lowHealthSource.volume = lowHealthVolume;
                _lowHealthSource.Play();
            }
        }
        else if (_lowHealthSource.isPlaying)
        {
            _lowHealthSource.Stop();
        }
    }

    private void HandlePlayerDied()
    {
        _isGameEnded = true;
        StopAllLoops();
        PlaySfx(deathSfx);
    }

    private void HandleGameWon()
    {
        _isGameEnded = true;
        if (_lowHealthSource.isPlaying)
            _lowHealthSource.Stop();
    }

    public void PlayPowerUpSound()
    {
        PlaySfx(powerUpSfx);
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayMusic(AudioClip newClip)
    {
        if (newClip == null || _musicSource == null) return;
        if (_musicSource.clip == newClip && _musicSource.isPlaying) return;

        if (_musicFadeRoutine != null)
            StopCoroutine(_musicFadeRoutine);

        _musicFadeRoutine = StartCoroutine(CrossfadeMusicRoutine(newClip));
    }

    private IEnumerator CrossfadeMusicRoutine(AudioClip nextClip)
    {
        float halfDuration = musicFadeDuration * 0.5f;

        if (_musicSource.isPlaying && halfDuration > 0f)
        {
            float startVol = _musicSource.volume;
            float t = 0f;
            while (t < halfDuration)
            {
                t += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(startVol, 0f, t / halfDuration);
                yield return null;
            }
        }

        _musicSource.clip = nextClip;
        _musicSource.Play();

        if (halfDuration > 0f)
        {
            float t = 0f;
            while (t < halfDuration)
            {
                t += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(0f, musicVolume, t / halfDuration);
                yield return null;
            }
        }

        _musicSource.volume = musicVolume;
    }

    private void StopAllLoops()
    {
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        if (_musicSource != null) _musicSource.Stop();
        if (_lowHealthSource != null) _lowHealthSource.Stop();
    }
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (_musicSource != null)
            _musicSource.volume = musicVolume;
    }
    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        lowHealthVolume = Mathf.Clamp01(volume * 0.85f);
        if (_sfxSource != null)
            _sfxSource.volume = sfxVolume;
        if (_lowHealthSource != null)
            _lowHealthSource.volume = lowHealthVolume;
    }
}
