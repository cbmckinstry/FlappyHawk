using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject iowaMenuPanel;
    public GameObject gamedayMenuPanel;
    public GameObject settingsMenuPanel;
    public GameObject howToPlayMenuPanel;
    public GameObject creditsMenuPanel;

    private Button selectedButton;
    private Button[] allButtons;

    private void PlayClick()
    {
        AudioManager.Instance?.PlayClickSound();
    }

    private void Start()
    {
        FindAllButtons();
        SelectFirstButton();
    }

    private void FindAllButtons()
    {
        allButtons = FindObjectsOfType<Button>(includeInactive: true);
    }

    private void SelectFirstButton()
    {
        if (allButtons.Length > 0)
        {
            selectedButton = allButtons[0];
            EventSystem.current.SetSelectedGameObject(selectedButton.gameObject);
        }
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (ControllerInputManager.Instance == null || !ControllerInputManager.Instance.IsControllerConnected())
            return;

        if (!IsButtonValid(selectedButton))
        {
            SelectFirstButton();
            return;
        }

        Vector2 dpadInput = ControllerInputManager.Instance.GetMenuInputDPad();
        Vector2 stickInput = ControllerInputManager.Instance.GetMenuInputLeftStick();
        Vector2 input = dpadInput + stickInput;

        if (input != Vector2.zero)
        {
            HandleMenuNavigation(input);
        }

        if (Input.GetButtonDown("Submit"))
        {
            if (selectedButton != null && IsButtonValid(selectedButton))
                selectedButton.onClick.Invoke();
        }
    }

    private bool IsButtonValid(Button button)
    {
        return button != null && button.gameObject != null && button.gameObject.scene.isLoaded;
    }

    private void HandleMenuNavigation(Vector2 direction)
    {
        if (!IsButtonValid(selectedButton))
            return;

        Selectable nextSelectable = null;

        if (direction.y > 0)
            nextSelectable = selectedButton?.FindSelectableOnUp();
        else if (direction.y < 0)
            nextSelectable = selectedButton?.FindSelectableOnDown();
        else if (direction.x > 0)
            nextSelectable = selectedButton?.FindSelectableOnRight();
        else if (direction.x < 0)
            nextSelectable = selectedButton?.FindSelectableOnLeft();

        if (nextSelectable != null && nextSelectable is Button button && IsButtonValid(button))
        {
            selectedButton = button;
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
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

        // Clear UI selection before transitioning
        EventSystem.current?.SetSelectedGameObject(null);
        selectedButton = null;

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
