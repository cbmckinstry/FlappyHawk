using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioManager audioManager;

    [Header("Panels")]
    public GameObject mainMenuPanel;   // Canvas/MainMenu
    public GameObject iowaMenuPanel;   // Canvas/IowaMenu
    public GameObject settingsMenuPanel; // (optional)
    public GameObject gamedayMenuPanel;  // (optional)

    void PlayClick() { if (audioManager) audioManager.PlayClickSound(); }

    public void ShowIowaMenu()
    {
        PlayClick();
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (iowaMenuPanel) iowaMenuPanel.SetActive(true);
    }

    public void BackToMain()
    {
        PlayClick();
        if (iowaMenuPanel) iowaMenuPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        PlayClick();
       #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}


