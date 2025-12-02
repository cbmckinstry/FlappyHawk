using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources (Persistent)")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Music Clips")]
    public AudioClip menuMusic;
    public AudioClip iowaMusic;
    public AudioClip gameDayMusic;

    [Header("SFX Clips")]
    public AudioClip clickSfx;
    public AudioClip cornCollectSfx;
    public AudioClip gameOverSfx;
    public AudioClip fieldGoalScoreSfx;
    public AudioClip touchdownScoreSfx;
    public AudioClip tackleSfx;
    public AudioClip wingFlapSfx;
    public AudioClip enemyScoreSfx;
    public AudioClip helmetCollectSfx;
    public AudioClip hoverSfx;
    public AudioClip magnetCollectSfx;
    public AudioClip speedBoostSfx;
    public AudioClip whistleSfx;
    public AudioClip gruntSfx;
    public AudioClip splatSfx;

    [Header("Volume (0–10) Defaults")]
    [Range(0f, 10f)] public float masterVolume = 10f;
    [Range(0f, 10f)] public float sfxVolume = 10f;
    [Range(0f, 10f)] public float musicVolume = 2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

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
    }

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

    private void PlayMusic(AudioClip clip)
    {
        if (!musicSource || clip == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
        ApplyVolumes();
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

    private void PlaySfx(AudioClip clip, float pitchJitter = 0f)
    {
        if (!sfxSource || clip == null) return;

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

    public void PlayClickSound() => PlaySfx(clickSfx);
    public void PlayCornCollect() => PlaySfx(cornCollectSfx);
    public void PlayDie() => PlaySfx(gameOverSfx);
    public void PlayGameOver() => PlaySfx(gameOverSfx);
    public void PlayTouchdown() => PlaySfx(touchdownScoreSfx);
    public void PlayFieldGoal() => PlaySfx(fieldGoalScoreSfx);
    public void PlayTackle() => PlaySfx(tackleSfx);
    public void PlayWingFlap() => PlaySfx(wingFlapSfx);
    public void PlayEnemyScore() => PlaySfx(enemyScoreSfx);
    public void PlayHelmetCollect() => PlaySfx(helmetCollectSfx);
    public void PlayHover() => PlaySfx(hoverSfx);
    public void PlayMagnetCollect() => PlaySfx(magnetCollectSfx);
    public void PlaySpeedBoost() => PlaySfx(speedBoostSfx);
    public void PlayWhistle() => PlaySfx(whistleSfx);
    public void PlayGrunt() => PlaySfx(gruntSfx);
    public void PlaySplat() => PlaySfx(splatSfx);

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
