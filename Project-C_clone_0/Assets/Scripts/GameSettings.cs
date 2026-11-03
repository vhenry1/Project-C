using UnityEngine;

public enum Difficulty { Easy, Normal, Hard }

/// <summary>
/// Choices made in the lobby (game mode select). Static so they survive scene loads.
/// Only the host's values matter; the host's game systems read them.
/// </summary>
public static class GameSettings
{
    public static Difficulty Current = Difficulty.Normal;
    public static int MaxPlayers = 4;
    public static int GoalNights = 5;   // MVP goal from the game doc

    public static float MonsterCountMultiplier
    {
        get { return Current == Difficulty.Easy ? 0.7f : Current == Difficulty.Hard ? 1.5f : 1f; }
    }

    public static float DayLength
    {
        get { return Current == Difficulty.Easy ? 110f : Current == Difficulty.Hard ? 80f : 95f; }
    }

    public static float NightLength
    {
        get { return Current == Difficulty.Easy ? 55f : Current == Difficulty.Hard ? 85f : 70f; }
    }
}
