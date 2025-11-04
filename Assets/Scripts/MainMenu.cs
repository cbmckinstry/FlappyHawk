using UnityEngine;
using UnityEngine.SceneManagement;

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

    // ===== MENU NAVIGATION =====

    public void ShowIowaMenu()
    {
        PlayClick();
        TogglePanels(mainMenuPanel, iowaMenuPanel);
    }

    public void ShowGamedayMenu()
    {
        PlayClick();
        TogglePanels(mainMenuPanel, gamedayMenuPanel);
    }

    public void ShowSettings()
    {
        PlayClick();
        TogglePanels(mainMenuPanel, settingsMenuPanel);
    }

    public void ShowHowToPlay()
    {
        PlayClick();
        TogglePanels(mainMenuPanel, howToPlayMenuPanel);
    }

    public void ShowCredits()
    {
        PlayClick();
        TogglePanels(mainMenuPanel, creditsMenuPanel);
    }

    public void BackToMain()
    {
        PlayClick();
        HideAllMenus();
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }

    private void TogglePanels(GameObject from, GameObject to)
    {
        if (from) from.SetActive(false);
        if (to) to.SetActive(true);
    }

    private void HideAllMenus()
    {
        if (iowaMenuPanel) iowaMenuPanel.SetActive(false);
        if (gamedayMenuPanel) gamedayMenuPanel.SetActive(false);
        if (settingsMenuPanel) settingsMenuPanel.SetActive(false);
        if (howToPlayMenuPanel) howToPlayMenuPanel.SetActive(false);
        if (creditsMenuPanel) creditsMenuPanel.SetActive(false);
    }

    // ===== DIFFICULTY START BUTTONS =====

    public void StartEasy()
    {
        StartGame(Difficulty.Easy, "IowaMode");
    }

    public void StartNormal()
    {
        StartGame(Difficulty.Normal, "IowaMode");
    }

    public void StartHard()
    {
        StartGame(Difficulty.Hard, "IowaMode");
    }

    public void StartJV()
    {
        StartGame(Difficulty.JV, "GamedayMode");
    }

    public void StartVarsity()
    {
        StartGame(Difficulty.Varsity, "GamedayMode");
    }

    private void StartGame(Difficulty difficulty, string sceneName)
    {
        PlayClick();
        GameManager.StartDifficulty = difficulty;
        SceneManager.LoadScene(sceneName);
    }

    // Quit Game

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
