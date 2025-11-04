using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject iowaMenuPanel;
    public GameObject gamedayMenuPanel;
    public GameObject settingsMenuPanel;
    public GameObject howToPlayMenuPanel;
    public GameObject creditsMenuPanel;

    private void PlayClick()
    {
        AudioManager.Instance?.PlayClickSound();
    }

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
        Application.Quit();
    }
}
