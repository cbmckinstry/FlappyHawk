using UnityEngine;

/// <summary>
/// Interface for all collectible objects (e.g., CornKernel, Helmet, Football, etc.).
/// Allows Player to interact with any object implementing Collect().
/// </summary>
public interface ICollectible
{
    void Collect(Player player);
}
