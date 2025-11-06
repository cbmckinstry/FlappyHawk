using UnityEngine;

/// <summary>
/// Balloon obstacle - inherits from Pipes for movement behavior
/// </summary>
public class Balloon : Pipes
{
    // Additional balloon-specific behavior can be added here
    // For example: bobbing animation, color changes, etc.

    private void Awake()
    {
        // Ensure the tag is set correctly for collision detection
        gameObject.tag = "Obstacle";
    }
}