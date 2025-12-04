using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ControllerInputManager : MonoBehaviour
{
    private static ControllerInputManager _instance;

    public static ControllerInputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ControllerInputManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("ControllerInputManager");
                    _instance = obj.AddComponent<ControllerInputManager>();
                }
            }
            return _instance;
        }
    }

    [SerializeField] private float dpadDeadzone = 0.5f;
    [SerializeField] private float stickDeadzone = 0.5f;

    private Gamepad currentGamepad;   // still only used for UI navigation

    // NEW — two controller support
    private Gamepad pad1;
    private Gamepad pad2;

    private float stickInputCooldown = 0f;
    private const float STICK_INPUT_DELAY = 0.3f;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_instance == null)
            _instance = this;
    }

    private void Update()
    {
        // Update connected controllers
        var pads = Gamepad.all;

        pad1 = pads.Count > 0 ? pads[0] : null;
        pad2 = pads.Count > 1 ? pads[1] : null;

        // UI still uses controller #1
        currentGamepad = pad1;

        if (stickInputCooldown > 0)
            stickInputCooldown -= Time.deltaTime;

        HandleUISubmit();
    }

    // ============================================================
    // UI NAVIGATION (UNCHANGED)
    // ============================================================

    private void HandleUISubmit()
    {
        if (currentGamepad == null)
            return;

        if (currentGamepad.buttonSouth.wasPressedThisFrame)
        {
            if (EventSystem.current == null)
                return;

            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

            // Auto-select a button if none selected
            if (selectedObject == null || !selectedObject.scene.isLoaded)
            {
                SelectFirstButton();
                selectedObject = EventSystem.current.currentSelectedGameObject;
            }

            // Submit
            if (selectedObject != null && selectedObject.scene.isLoaded)
            {
                ExecuteEvents.Execute(
                    selectedObject,
                    new BaseEventData(EventSystem.current),
                    ExecuteEvents.submitHandler
                );
            }
        }
    }

    private void SelectFirstButton()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject != null &&
                button.gameObject.scene.isLoaded && button.interactable)
            {
                EventSystem.current?.SetSelectedGameObject(button.gameObject);
                break;
            }
        }
    }

    // ============================================================
    // ORIGINAL PUBLIC API
    // ============================================================

    public bool IsControllerConnected()
    {
        return Gamepad.current != null;
    }

    public Vector2 GetMenuInputDPad()
    {
        if (currentGamepad == null) return Vector2.zero;

        Vector2 input = Vector2.zero;
        if (currentGamepad.dpad.up.isPressed) input.y = 1f;
        if (currentGamepad.dpad.down.isPressed) input.y = -1f;
        if (currentGamepad.dpad.left.isPressed) input.x = -1f;
        if (currentGamepad.dpad.right.isPressed) input.x = 1f;

        return input;
    }

    public Vector2 GetMenuInputLeftStick()
    {
        if (currentGamepad == null) return Vector2.zero;

        Vector2 stick = currentGamepad.leftStick.value;

        if (stick.magnitude > stickDeadzone && stickInputCooldown <= 0)
        {
            stickInputCooldown = STICK_INPUT_DELAY;
            return new Vector2(Mathf.Sign(stick.x), Mathf.Sign(stick.y));
        }

        return Vector2.zero;
    }

    public bool IsPausePressed()
    {
        if (currentGamepad == null) return false;
        return currentGamepad.startButton.wasPressedThisFrame;
    }

    public bool IsSelectPressed()
    {
        if (currentGamepad == null) return false;
        return currentGamepad.selectButton.wasPressedThisFrame;
    }

    // ============================================================
    // NEW — CONTROLLER INPUT FOR MULTIPLAYER
    // ============================================================

    private Gamepad GetPadForPlayer(Player.PlayerID id)
    {
        return id == Player.PlayerID.Player1 ? pad1 : pad2;
    }

    public bool HasController(Player.PlayerID id)
    {
        return GetPadForPlayer(id) != null;
    }

    public bool GetFlap(Player.PlayerID id)
    {
        Gamepad pad = GetPadForPlayer(id);
        if (pad == null) return false;

        return pad.buttonSouth.wasPressedThisFrame;  // A / Cross
    }

    public bool GetDrop(Player.PlayerID id)
    {
        Gamepad pad = GetPadForPlayer(id);
        if (pad == null) return false;

        return pad.rightShoulder.wasPressedThisFrame ||
               pad.leftShoulder.wasPressedThisFrame;  // bumpers
    }
}
