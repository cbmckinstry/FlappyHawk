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
    public AudioClip gameMusic;

    [Header("Volume (0–10) Defaults")]
    [Range(0f, 10f)] public float masterVolume = 10f;
    [Range(0f, 10f)] public float sfxVolume = 10f;
    [Range(0f, 10f)] public float musicVolume = 5f;

    private void Awake()
    {
        // Ensure only one instance persists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved volumes (use defaults if none exist)
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", masterVolume);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", sfxVolume);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", musicVolume);

        ApplyVolumes();
        PlayMenuMusic();

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

        if (scene.name == "MenuScreen")
            PlayMenuMusic();
        else
            PlayGameMusic();
    }

    private void PlayMenuMusic()
    {
        if (menuMusic == null || (musicSource.clip == menuMusic && musicSource.isPlaying))
            return;

        musicSource.clip = menuMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

    private void PlayGameMusic()
    {
        if (gameMusic == null || (musicSource.clip == gameMusic && musicSource.isPlaying))
            return;

        musicSource.clip = gameMusic;
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
