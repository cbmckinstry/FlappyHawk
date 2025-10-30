using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip hoverSound;
    public AudioClip clickSound;


    public void PlayGame()
    {
        PlayClickSound();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        PlayClickSound();
        Debug.Log("Game Quit!");
        Application.Quit();
    }

    public void PlayHoverSound()
    {
        if (sfxSource != null && hoverSound != null)
            sfxSource.PlayOneShot(hoverSound);
    }

    public void PlayClickSound()
    {
        if (sfxSource != null && clickSound != null)
            sfxSource.PlayOneShot(clickSound);
    }
}
