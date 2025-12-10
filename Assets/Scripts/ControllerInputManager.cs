using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerInputManager : MonoBehaviour
{
    public static ControllerInputManager Instance { get; private set; }

    // Two explicitly assigned controllers
    private Gamepad p1Controller;
    private Gamepad p2Controller;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        AssignControllers();
    }

    private void AssignControllers()
    {
        var pads = Gamepad.all;

        if (pads.Count == 0)
        {
            p1Controller = null;
            p2Controller = null;
            return;
        }

        // Always assign first controller to Player 1
        p1Controller = pads[0];

        // Assign next controller to Player 2 (if exists)
        p2Controller = pads.Count > 1 ? pads[1] : null;

        Debug.Log($"[ControllerInputManager] P1={p1Controller} P2={p2Controller}");
    }


    // Call this when scene loads
    public void RecheckControllers() => AssignControllers();

    public bool HasTwoControllers()
    {
        return p1Controller != null && p2Controller != null;
    }

    // ============================
    // INPUT — FLAP
    // ============================
    public bool GetFlap(Player.PlayerID id)
{
    // ============================
    // KEYBOARD INPUT
    // ============================
    if (Keyboard.current != null)
    {
        if (id == Player.PlayerID.Player1 &&
            Keyboard.current.wKey.wasPressedThisFrame)
        {
            return true;    // P1 flap = W
        }

        if (id == Player.PlayerID.Player2 &&
            Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            return true;    // P2 flap = Up Arrow
        }
    }

    // ============================
    // CONTROLLER INPUT
    // ============================
    var pad = MultiplayerManager.Instance.GetControllerForPlayer(id);
    return pad != null && pad.buttonSouth.wasPressedThisFrame;
}

public bool GetDrop(Player.PlayerID id)
{
    // ============================
    // KEYBOARD INPUT
    // ============================
    if (Keyboard.current != null)
    {
        if (id == Player.PlayerID.Player1 &&
            Keyboard.current.sKey.wasPressedThisFrame)
        {
            return true;    // P1 drop = S
        }

        if (id == Player.PlayerID.Player2 &&
            Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            return true;    // P2 drop = Down Arrow
        }
    }

    // ============================
    // CONTROLLER INPUT (right shoulder)
    // ============================
    var pad = MultiplayerManager.Instance.GetControllerForPlayer(id);
    return pad != null && pad.rightShoulder.wasPressedThisFrame;
}




    // ============================
    // Optional Pause input
    // ============================
    public bool GetPause(Player.PlayerID id)
    {
        if (id == Player.PlayerID.Player1 && p1Controller != null)
            return p1Controller.startButton.wasPressedThisFrame;

        if (id == Player.PlayerID.Player2 && p2Controller != null)
            return p2Controller.startButton.wasPressedThisFrame;

        return false;
    }

    // "Are there controllers connected?"
    public bool HasAnyControllers()
    {
        return Gamepad.all.Count > 0;
    }

    // Vertical menu navigation: -1, 0, +1
    public float GetMenuVertical()
    {
        if (Gamepad.all.Count == 0)
            return 0;

        var g = Gamepad.all[0]; // Use first controller for menu navigation

        if (g.dpad.up.wasPressedThisFrame) return 1;
        if (g.dpad.down.wasPressedThisFrame) return -1;

        if (g.leftStick.up.wasPressedThisFrame) return 1;
        if (g.leftStick.down.wasPressedThisFrame) return -1;

        return 0;
    }
    public float GetMenuHorizontal()
    {
        if (Gamepad.all.Count == 0)
            return 0;

        var g = Gamepad.all[0];

        if (g.dpad.left.wasPressedThisFrame) return -1;
        if (g.dpad.right.wasPressedThisFrame) return 1;

        if (g.leftStick.left.wasPressedThisFrame) return -1;
        if (g.leftStick.right.wasPressedThisFrame) return 1;

        return 0;
    }


    // Submit (A / X button)
    public bool GetMenuSubmit()
    {
        if (Gamepad.all.Count == 0)
            return false;

        var g = Gamepad.all[0];

        return g.buttonSouth.wasPressedThisFrame; // A / X
    }

}
