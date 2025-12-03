using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI Roots")]
    public GameObject PauseMenu;             // Entire pause menu container
    public GameObject PauseMenuButtons;      // Resume / Restart / Settings / Controls / Exit
    public GameObject PauseButton;           // Pause icon in gameplay HUD

    [Header("Sub Panels")]
    public GameObject SettingsPanel;
    public GameObject ControlsPanel;
    public GameObject GameOverPanel;


    private bool isPaused = false;

    // True only after hitting the Play button in Iowa/GameDay
    public static bool GameIsActive = false;

    private void Start()
    {
        PauseMenu?.SetActive(false);
        PauseMenuButtons?.SetActive(false);
        SettingsPanel?.SetActive(false);
        ControlsPanel?.SetActive(false);

        PauseButton?.SetActive(true);
    }

    private void Update()
    {
        bool pausePressed =
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        if (pausePressed)
            TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    // --------------------- PUBLIC BUTTON METHODS ---------------------

    public void Resume()
    {
        isPaused = false;

        PauseMenu?.SetActive(false);
        PauseMenuButtons?.SetActive(false);
        SettingsPanel?.SetActive(false);
        ControlsPanel?.SetActive(false);

        PauseButton?.SetActive(true);

        AudioManager.Instance?.PauseMusic(false);

        if (GameIsActive)
        {
            Time.timeScale = 1f;
        }
        else
        {
            // Restore GameOver UI in non-active state
            if (GameOverPanel != null)
                GameOverPanel.SetActive(true);

            Time.timeScale = 0f;
        }
    }


    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        PauseMenu?.SetActive(true);
        PauseMenuButtons?.SetActive(true);
        SettingsPanel?.SetActive(false);
        ControlsPanel?.SetActive(false);

        PauseButton?.SetActive(false);

        // hide game over no matter what
        if (GameOverPanel != null)
            GameOverPanel.SetActive(false);

        AudioManager.Instance?.PauseMusic(true);
    }


    public void Restart()
    {
        // Restart always starts a new round
        GameIsActive = true;

        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    public void OpenSettings()
    {
        PauseMenuButtons?.SetActive(false);
        ControlsPanel?.SetActive(false);
        SettingsPanel?.SetActive(true);
    }

    public void OpenControls()
    {
        PauseMenuButtons?.SetActive(false);
        SettingsPanel?.SetActive(false);
        ControlsPanel?.SetActive(true);
    }

    public void BackToPauseMenu()
    {
        SettingsPanel?.SetActive(false);
        ControlsPanel?.SetActive(false);
        PauseMenuButtons?.SetActive(true);
    }

    public void ExitToMainMenu()
    {
        GameIsActive = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuScreen");
    }

    public void PauseFromButton()
    {
        TogglePause();
    }
}
