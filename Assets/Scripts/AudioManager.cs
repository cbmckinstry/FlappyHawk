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

    [Header("Volume (0–10)")]
    public float masterVolume = 5f;
    public float sfxVolume = 5f;
    public float musicVolume = 5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved volumes
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 5f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 5f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 5f);

        ApplyVolumes();
        PlayMenuMusic();

        // Swap music when scenes change
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
        if (musicSource.clip == menuMusic && musicSource.isPlaying) return;
        musicSource.clip = menuMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

    private void PlayGameMusic()
    {
        if (gameMusic == null) return;
        if (musicSource.clip == gameMusic && musicSource.isPlaying) return;
        musicSource.clip = gameMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayClickSound()
    {
        if (sfxSource && clickSound)
            sfxSource.PlayOneShot(clickSound, (sfxVolume / 10f) * (masterVolume / 10f));
    }

    public void ApplyVolumes()
    {
        float master = masterVolume / 10f;
        if (musicSource) musicSource.volume = (musicVolume / 10f) * master;
        if (sfxSource) sfxSource.volume = (sfxVolume / 10f) * master;
    }

    public void SetMasterVolume(float v)
    {
        masterVolume = v;
        PlayerPrefs.SetFloat("MasterVolume", v);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = v;
        PlayerPrefs.SetFloat("SFXVolume", v);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = v;
        PlayerPrefs.SetFloat("MusicVolume", v);
        PlayerPrefs.Save();
        ApplyVolumes();
    }
}
