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
        // Detects all connected gamepads and assigns first two
        if (Gamepad.all.Count > 0)
            p1Controller = Gamepad.all[0];

        if (Gamepad.all.Count > 1)
            p2Controller = Gamepad.all[1];

        Debug.Log($"[ControllerInputManager] Controllers assigned: " +
                  $"P1={(p1Controller != null)} P2={(p2Controller != null)}");
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
        if (id == Player.PlayerID.Player1 && p1Controller != null)
            return p1Controller.buttonSouth.wasPressedThisFrame; // A / X

        if (id == Player.PlayerID.Player2 && p2Controller != null)
            return p2Controller.buttonSouth.wasPressedThisFrame;

        return false;
    }

    // ============================
    // INPUT — DROP
    // ============================
    public bool GetDrop(Player.PlayerID id)
    {
        if (id == Player.PlayerID.Player1 && p1Controller != null)
            return p1Controller.rightShoulder.wasPressedThisFrame;

        if (id == Player.PlayerID.Player2 && p2Controller != null)
            return p2Controller.rightShoulder.wasPressedThisFrame;

        return false;
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

    // Submit (A / X button)
    public bool GetMenuSubmit()
    {
        if (Gamepad.all.Count == 0)
            return false;

        var g = Gamepad.all[0];

        return g.buttonSouth.wasPressedThisFrame; // A / X
    }

}
