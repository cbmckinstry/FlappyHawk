using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioManager audioManager;

    public void PlayGame()
    {
        if (audioManager != null)
            audioManager.PlayClickSound();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        if (audioManager != null)
            audioManager.PlayClickSound();

        Application.Quit();
    }
}
