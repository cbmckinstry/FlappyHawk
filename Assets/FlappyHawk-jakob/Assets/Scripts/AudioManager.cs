using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources (Persistent)")]
    [Tooltip("A dedicated AudioSource for SFX. Set to 'Spatial Blend: 0' (2D).")]
    public AudioSource sfxSource;

    [Tooltip("A dedicated AudioSource for background music. Set to 'Loop: true'.")]
    public AudioSource musicSource;

    [Header("Music Clips")]
    public AudioClip menuMusic;
    public AudioClip iowaMusic;
    public AudioClip gameDayMusic;

    [Header("SFX Clips")]
    public AudioClip clickSfx;
    public AudioClip cornCollectSfx;
    public AudioClip dieSfx;
    public AudioClip footballScoreSfx;
    public AudioClip wingFlapSfx;

    [Header("Volume (0–10) Defaults")]
    [Range(0f, 10f)] public float masterVolume = 10f;
    [Range(0f, 10f)] public float sfxVolume = 10f;
    [Range(0f, 10f)] public float musicVolume = 5f;

    // ————————————————— LIFECYCLE —————————————————
    private void Awake()
    {
        // Singleton pattern (persist across scenes)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved volumes
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", masterVolume);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", sfxVolume);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", musicVolume);
        ApplyVolumes();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ————————————————— SCENE → MUSIC —————————————————
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Scenes
        var name = scene.name;

        if (name == "MenuScreen")
        {
            PlayMusic(menuMusic);
        }
        else if (name == "IowaScene")
        {
            PlayMusic(iowaMusic);
        }
        else if (name == "GamedayScene")
        {
            PlayMusic(gameDayMusic);
        }
        else
        {
            // Fallback if you land on a utility scene
            PlayMusic(menuMusic);
        }
    }

    /// <summary>
    /// Optional: call this manually if you change modes without loading a new scene.
    /// </summary>
    public void SwitchMusicForMode(GameManager.GameMode mode)
    {
        switch (mode)
        {
            case GameManager.GameMode.Iowa:
                PlayMusic(iowaMusic);
                break;
            case GameManager.GameMode.GameDay:
                PlayMusic(gameDayMusic);
                break;
            default:
                PlayMusic(menuMusic);
                break;
        }
    }

    // ————————————————— MUSIC CONTROL —————————————————
    private void PlayMusic(AudioClip clip)
    {
        if (!musicSource || clip == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
        ApplyVolumes(); // ensure volume is correct on new clip
    }

    public void StopMusic()
    {
        if (musicSource) musicSource.Stop();
    }

    public void PauseMusic(bool pause)
    {
        if (!musicSource) return;
        if (pause) musicSource.Pause();
        else musicSource.UnPause();
    }

    // ————————————————— SFX HELPERS —————————————————
    private void PlaySfx(AudioClip clip, float pitchJitter = 0f)
    {
        if (!sfxSource || clip == null) return;

        // optional tiny pitch variation for “less robotic” feel
        if (pitchJitter > 0f)
        {
            sfxSource.pitch = Random.Range(1f - pitchJitter, 1f + pitchJitter);
        }
        else
        {
            sfxSource.pitch = 1f;
        }

        sfxSource.PlayOneShot(clip, (sfxVolume / 10f) * (masterVolume / 10f));
    }

    public void PlayClickSound() => PlaySfx(clickSfx, 0.05f);
    public void PlayCornCollect() => PlaySfx(cornCollectSfx, 0.03f);
    public void PlayDie() => PlaySfx(dieSfx);
    public void PlayFootballScore() => PlaySfx(footballScoreSfx);
    public void PlayWingFlap() => PlaySfx(wingFlapSfx, 0.06f);

    // ————————————————— VOLUME —————————————————
    public void ApplyVolumes()
    {
        float master = Mathf.Clamp01(masterVolume / 10f);
        if (musicSource)
            musicSource.volume = Mathf.Clamp01(musicVolume / 10f) * master;
        if (sfxSource)
            sfxSource.volume = Mathf.Clamp01(sfxVolume / 10f) * master;
    }

    public void SetMasterVolume(float v)
    {
        masterVolume = Mathf.Clamp(v, 0f, 10f);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp(v, 0f, 10f);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp(v, 0f, 10f);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }
}
