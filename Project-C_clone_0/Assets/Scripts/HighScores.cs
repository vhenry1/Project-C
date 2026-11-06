using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using UnityEngine;

[Serializable]
public class ScoreEntry
{
    public string name;
    public int nights;
}

[Serializable]
class ScoreList
{
    public List<ScoreEntry> entries = new List<ScoreEntry>();
}

/// <summary>
/// Arcade-style high scores. The local top 10 (with names) always works offline and is saved on this computer.
/// If Unity Gaming Services is connected, the score is also sent to a global leaderboard.
/// Create a leaderboard with the ID below in the Unity Cloud Dashboard (sort: descending, update: keep best).
/// </summary>
public static class HighScores
{
    public const string LeaderboardId = "nights_survived";
    private const string Key = "projectc_highscores";
    private const int MaxEntries = 10;

    public static string LastError { get; private set; } = "";

    public static List<ScoreEntry> Local()
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return new List<ScoreEntry>();
        try
        {
            ScoreList list = JsonUtility.FromJson<ScoreList>(json);
            return list != null ? list.entries : new List<ScoreEntry>();
        }
        catch (Exception)
        {
            return new List<ScoreEntry>();
        }
    }

    public static void AddLocal(string name, int nights)
    {
        List<ScoreEntry> entries = Local();
        entries.Add(new ScoreEntry { name = name, nights = nights });
        entries.Sort((a, b) => b.nights.CompareTo(a.nights));
        if (entries.Count > MaxEntries) entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(new ScoreList { entries = entries }));
        PlayerPrefs.Save();
    }

    public static async Task<bool> SubmitGlobalAsync(string name, int nights)
    {
        if (!SessionManager.Ready)
        {
            LastError = "Unity Gaming Services is not initialized.";
            return false;
        }

        try
        {
            string safe = name.Replace(' ', '_');
            if (safe.Length < 3) safe = safe.PadRight(3, '_');
            await AuthenticationService.Instance.UpdatePlayerNameAsync(safe);
            await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, nights);
            LastError = "";
            return true;
        }
        catch (Exception e)
        {
            LastError = DescribeGlobalError(e);
            Debug.LogWarning("Global score upload failed: " + LastError);
            return false;
        }
    }

    public static async Task<List<ScoreEntry>> GlobalAsync()
    {
        var result = new List<ScoreEntry>();
        if (!SessionManager.Ready)
        {
            LastError = "Unity Gaming Services is not initialized.";
            return result;
        }

        try
        {
            var page = await LeaderboardsService.Instance.GetScoresAsync(
                LeaderboardId, new GetScoresOptions { Limit = MaxEntries });
            foreach (var r in page.Results)
            {
                string n = r.PlayerName;
                int hash = n.IndexOf('#');
                if (hash > 0) n = n.Substring(0, hash);
                result.Add(new ScoreEntry { name = n, nights = (int)r.Score });
            }
            LastError = "";
        }
        catch (Exception e)
        {
            LastError = DescribeGlobalError(e);
            Debug.LogWarning("Could not load global scores: " + LastError);
        }
        return result;
    }

    private static string DescribeGlobalError(Exception e)
    {
        if (e.Message.IndexOf("Leaderboard config could not be found", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Create a Unity Cloud leaderboard with ID '" + LeaderboardId + "'.";
        }

        return e.Message;
    }
}
