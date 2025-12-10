using UnityEngine;

public static class CustomSpawnSettings
{
    // Is the next Iowa run a custom one?
    public static bool IsCustomIowa = false;

    // Base difficulty to use for health / speed / spawn rate
    public static GameManager.Difficulty BaseDifficulty = GameManager.Difficulty.Normal;

    // Spawn weights (all 0–1)
    public static float obstacleSpawnChance = 0.8f;

    public static float balloonWeight = 0.2f;
    public static float siloWeight = 0.2f;
    public static float turbineWeight = 0.2f;
    public static float cycloneBirdWeight = 0f;
    public static float tornadoWeight = 0f;

    public static float cornKernelWeight = 0.4f;
    public static float helmetWeight = 0f;
    public static float windBoostWeight = 0.2f;
    public static float cornMagnetWeight = 0.2f;

    public static float gravityFlipChance = 0.3f;
}

