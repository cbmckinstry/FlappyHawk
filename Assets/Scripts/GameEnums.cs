// GameEnums.cs
using UnityEngine;

/// <summary>
/// Shared enums for difficulty, mode, and game type.
/// Used across IowaManager, GameDayManager, and GameManager.
/// </summary>
public enum Difficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}

public enum GameMode
{
    Iowa = 0,
    GameDay = 1
}

public enum GameDayDifficulty
{
    College = 0,
    Pro = 1
}
