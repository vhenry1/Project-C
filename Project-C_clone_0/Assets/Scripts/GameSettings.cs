using UnityEngine;

public enum Difficulty { Easy, Normal, Hard }

/// <summary>
/// Shared game settings. Appearance choices persist per computer; only the host's game mode values matter.
/// </summary>
public static class GameSettings
{
    public static readonly string[] HairColorNames = { "Brown", "Blonde", "Black", "Auburn", "Rose" };
    public static readonly Color[] HairColors =
    {
        new Color(0.42f, 0.27f, 0.19f),
        new Color(0.92f, 0.66f, 0.25f),
        new Color(0.14f, 0.11f, 0.12f),
        new Color(0.67f, 0.25f, 0.15f),
        new Color(0.78f, 0.33f, 0.43f)
    };

    public static readonly string[] ClothesColorNames = { "Sage", "Blue", "Plum", "Red", "Gold" };
    public static readonly Color[] ClothesColors =
    {
        new Color(0.42f, 0.65f, 0.40f),
        new Color(0.29f, 0.51f, 0.76f),
        new Color(0.55f, 0.35f, 0.65f),
        new Color(0.77f, 0.30f, 0.28f),
        new Color(0.91f, 0.65f, 0.24f)
    };

    public static Difficulty Current = Difficulty.Normal;
    public static int MaxPlayers = 4;
    public static int GoalNights = 5;   // MVP goal from the game doc

    public static int HairColorIndex
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt("PlayerHairColor", 0), 0, HairColors.Length - 1); }
    }

    public static int ClothesColorIndex
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt("PlayerClothesColor", 0), 0, ClothesColors.Length - 1); }
    }

    public static void SetHairColor(int index)
    {
        PlayerPrefs.SetInt("PlayerHairColor", Mathf.Clamp(index, 0, HairColors.Length - 1));
        PlayerPrefs.Save();
    }

    public static void SetClothesColor(int index)
    {
        PlayerPrefs.SetInt("PlayerClothesColor", Mathf.Clamp(index, 0, ClothesColors.Length - 1));
        PlayerPrefs.Save();
    }

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
