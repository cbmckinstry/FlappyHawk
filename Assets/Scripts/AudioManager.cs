using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources (Persistent)")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Clips")]
    public AudioClip clickSound;
    public AudioClip menuMusic;
    public AudioClip iowaMusic;
    public AudioClip gameDayMusic;

    [Header("Volume (0–10) Defaults")]
    [Range(0f, 10f)] public float masterVolume = 10f;
    [Range(0f, 10f)] public float sfxVolume = 10f;
    [Range(0f, 10f)] public float musicVolume = 5f;

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!musicSource) return;

        switch (scene.name)
        {
            case "MenuScreen":
            case "MainMenu":
                PlayMusic(menuMusic);
                break;

            case "IowaScene":
            case "IowaMode":
                PlayMusic(iowaMusic);
                break;

            case "GameDayScene":
            case "GamedayMode":
                PlayMusic(gameDayMusic);
                break;

            default:
                PlayMusic(menuMusic); // fallback
                break;
        }
    }

    // ————————— MUSIC —————————
    private void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayClickSound()
    {
        if (sfxSource && clickSound)
        {
            sfxSource.pitch = Random.Range(0.95f, 1.05f);
            sfxSource.PlayOneShot(clickSound, (sfxVolume / 10f) * (masterVolume / 10f));
        }
    }

    // ————————— VOLUME —————————
    public void ApplyVolumes()
    {
        float master = masterVolume / 10f;
        if (musicSource)
            musicSource.volume = (musicVolume / 10f) * master;
        if (sfxSource)
            sfxSource.volume = (sfxVolume / 10f) * master;
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
