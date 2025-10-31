using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioManager audioManager;

    public void PlayGame()
    {
        audioManager.PlayClickSound();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        audioManager.PlayClickSound();
        Debug.Log("Game Quit!");
        Application.Quit();
    }
}
