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

    // ---------------- MENU NAVIGATION ----------------
    public void ShowIowaMenu() => SwitchPanel(mainMenuPanel, iowaMenuPanel);
    public void ShowGamedayMenu() => SwitchPanel(mainMenuPanel, gamedayMenuPanel);
    public void ShowSettings() => SwitchPanel(mainMenuPanel, settingsMenuPanel);
    public void ShowHowToPlay() => SwitchPanel(mainMenuPanel, howToPlayMenuPanel);
    public void ShowCredits() => SwitchPanel(mainMenuPanel, creditsMenuPanel);
    public void BackToMain() => ResetToMain();

    private void SwitchPanel(GameObject from, GameObject to)
    {
        PlayClick();
        if (from) from.SetActive(false);
        if (to) to.SetActive(true);
    }

    private void ResetToMain()
    {
        PlayClick();
        iowaMenuPanel.SetActive(false);
        gamedayMenuPanel.SetActive(false);
        settingsMenuPanel.SetActive(false);
        howToPlayMenuPanel.SetActive(false);
        creditsMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // ---------------- START BUTTONS ----------------
    public void StartEasy() => StartGame(GameManager.Difficulty.Easy, "IowaScene");
    public void StartNormal() => StartGame(GameManager.Difficulty.Normal, "IowaScene");
    public void StartHard() => StartGame(GameManager.Difficulty.Hard, "IowaScene");

    public void StartCollege() => StartGame(GameManager.Difficulty.Easy, "GamedayScene", GameManager.GameDayDifficulty.College);
    public void StartPro() => StartGame(GameManager.Difficulty.Normal, "GamedayScene", GameManager.GameDayDifficulty.Pro);

    private void StartGame(GameManager.Difficulty difficulty, string sceneName, GameManager.GameDayDifficulty? gameDayDiff = null)
    {
        PlayClick();

        // Set Iowa difficulty
        GameManager.StartDifficulty = difficulty;

        // Set GameDay difficulty if provided
        if (gameDayDiff.HasValue)
        {
            if (GameManager.GameDayInstance != null)
            {
                GameManager.GameDayInstance.SetGameDayDifficulty(gameDayDiff.Value);
            }
            else
            {
                // Persist for scene load (in case GameDayManager isn't active yet)
                PlayerPrefs.SetInt("GameDayDifficulty", (int)gameDayDiff.Value);
                PlayerPrefs.Save();
            }
        }

        // Load the appropriate scene
        SceneManager.LoadScene(sceneName);
    }

    // ---------------- SYSTEM ----------------
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
