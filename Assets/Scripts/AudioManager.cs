using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Audio Clips")]
    public AudioClip clickSound;
    public AudioClip menuMusic;

    [Header("Volume Settings")]
    public float masterVolume = 5f;
    public float sfxVolume = 5f;
    public float musicVolume = 5f;

    private void Start()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 5f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 5f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 5f);

        ApplyVolumes();
        PlayBackgroundMusic();
    }

    private void PlayBackgroundMusic()
    {
        if (musicSource != null && menuMusic != null)
        {
            musicSource.clip = menuMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlayClickSound()
    {
        if (sfxSource != null && clickSound != null)
        {
            sfxSource.volume = (sfxVolume / 10f) * (masterVolume / 10f);
            sfxSource.PlayOneShot(clickSound);
        }
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = (musicVolume / 10f) * (masterVolume / 10f);
        if (sfxSource != null)
            sfxSource.volume = (sfxVolume / 10f) * (masterVolume / 10f);
    }

    // === Volume Slider Hooks ===
    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        ApplyVolumes();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
        ApplyVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
        ApplyVolumes();
    }
}
