using UnityEngine;

/// <summary>
/// Silo obstacle - inherits from Pipes for movement behavior
/// Can be extended with specific silo behaviors (e.g., collapsing animation, etc.)
/// </summary>
public class Silo : Pipes
{
    // Additional silo-specific behavior can be added here
    // For example: state changes, explosions, damage zones, etc.

    private void Awake()
    {
        // Ensure the tag is set correctly for collision detection
        gameObject.tag = "Obstacle";
    }
}