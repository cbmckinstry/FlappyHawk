using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;   // <-- added for UI control
using UnityEngine.UI;              // <-- added for button access

public class PauseManager : MonoBehaviour
{
    [Header("UI Roots")]
    public GameObject PauseMenu;
    public GameObject PauseMenuButtons;
    public GameObject PauseButton;

    [Header("Sub Panels")]
    public GameObject SettingsPanel;
    public GameObject ControlsPanel;
    public GameObject GameOverPanel;

    // NEW: default highlighted button when pause opens
    public Button pauseResumeButton;

    private bool isPaused = false;

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

        // -------------------------------
        // NEW: Controller activate selected button with A
        // -------------------------------
        if (isPaused)
        {
            var pad = Gamepad.current;
            if (pad != null && pad.buttonSouth.wasPressedThisFrame)   // A button
            {
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                if (selected != null)
                {
                    Button b = selected.GetComponent<Button>();
                    if (b != null)
                        b.onClick.Invoke();
                }
            }
        }
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

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

        if (GameOverPanel != null)
            GameOverPanel.SetActive(false);

        AudioManager.Instance?.PauseMusic(true);

        // ------------------------------------------
        // NEW: Select the Resume button automatically
        // ------------------------------------------
        if (pauseResumeButton != null)
            EventSystem.current?.SetSelectedGameObject(pauseResumeButton.gameObject);
    }

    public void Restart()
    {
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

        // Auto-select first button inside settings panel if desired
    }

    public void OpenControls()
    {
        PauseMenuButtons?.SetActive(false);
        SettingsPanel?.SetActive(false);
        ControlsPanel?.SetActive(true);

        // Auto-select first button inside controls panel if desired
    }

    public void BackToPauseMenu()
    {
        SettingsPanel?.SetActive(false);
        ControlsPanel?.SetActive(false);
        PauseMenuButtons?.SetActive(true);

        // Re-select resume button
        if (pauseResumeButton != null)
            EventSystem.current?.SetSelectedGameObject(pauseResumeButton.gameObject);
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
