using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Menu scene: Start, High Scores and Credits menus. Credits list every asset used.
/// Built automatically by ProjectCRuntime, so nothing needs wiring in the scene.
/// </summary>
public class MenuSceneUI : MonoBehaviour
{
    private GameObject main, credits, scores;
    private RectTransform scoreContent;

    private const string CreditsText =
        "<b>Game by Jenna</b> - Co-op Farm Defense\n\n" +
        "<b>Pixel art:</b> Cainos - Pixel Art Top Down Basic and Village Props (Unity Asset Store)\n" +
        "<b>Characters:</b> Clembod - Warrior free set (itch.io, free license)\n" +
        "<b>Materials:</b> Shaded Spectrum - Free Realistic Outdoor Materials\n" +
        "<b>Tools:</b> Unity Netcode for GameObjects, Unity Multiplayer Services, Unity Leaderboards, ParrelSync (VeriorPies)\n" +
        "<b>Music and sound effects:</b> generated in code (AudioManager.cs)\n\n" +
        "Code written for this project is commented. Netcode patterns follow the Unity documentation.";

    private void Start()
    {
        UIKit.HideOriginalCanvases();
        Canvas canvas = UIKit.MakeCanvas("ProjectC_UI");
        AudioManager.Music(false);

        RectTransform m = UIKit.MakePanel(canvas.transform, "Co-op Farm Defense", new Vector2(520, 520), out main);
        UIKit.MakeButton(m, "Play", UIKit.Green, () => UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby"), new Vector2(380, 70));
        UIKit.MakeButton(m, "High Scores", UIKit.Gold, ShowScores, new Vector2(380, 70));
        UIKit.MakeButton(m, "Credits", UIKit.Sky, () => { main.SetActive(false); credits.SetActive(true); }, new Vector2(380, 70));
#if !UNITY_WEBGL
        UIKit.MakeButton(m, "Quit", UIKit.Peach, Application.Quit, new Vector2(380, 70));
#endif

        RectTransform c = UIKit.MakePanel(canvas.transform, "Credits", new Vector2(900, 560), out credits);
        TMP_Text body = UIKit.Label(c, CreditsText, 24, UIKit.Ink, TextAlignmentOptions.TopLeft, FontStyles.Normal);
        body.rectTransform.sizeDelta = new Vector2(820, 340);
        UIKit.MakeButton(c, "Back", UIKit.Peach, () => { credits.SetActive(false); main.SetActive(true); }, new Vector2(240, 60));
        credits.SetActive(false);

        scoreContent = UIKit.MakePanel(canvas.transform, "High Scores", new Vector2(700, 640), out scores);
        scores.SetActive(false);
    }

    private async void ShowScores()
    {
        main.SetActive(false);
        scores.SetActive(true);
        UIKit.Clear(scoreContent);

        AddScoreRows("This computer", HighScores.Local());
        TMP_Text status = UIKit.Label(scoreContent, "Loading global scores...", 22, UIKit.Ink, TextAlignmentOptions.Center, FontStyles.Italic);
        status.rectTransform.sizeDelta = new Vector2(600, 30);

        bool ok = SessionManager.Instance != null && await SessionManager.Instance.InitAsync();
        List<ScoreEntry> global = ok ? await HighScores.GlobalAsync() : new List<ScoreEntry>();
        if (this == null || scoreContent == null) return;

        status.text = global.Count > 0 ? "" : "Global scores unavailable (offline or not set up).";
        if (global.Count > 0) AddScoreRows("Global top 5", global.GetRange(0, Mathf.Min(5, global.Count)));

        UIKit.MakeButton(scoreContent, "Back", UIKit.Peach, () => { scores.SetActive(false); main.SetActive(true); }, new Vector2(240, 56));
    }

    private void AddScoreRows(string title, List<ScoreEntry> list)
    {
        TMP_Text h = UIKit.Label(scoreContent, title, 28, UIKit.Ink);
        h.rectTransform.sizeDelta = new Vector2(600, 36);
        if (list.Count == 0)
        {
            TMP_Text none = UIKit.Label(scoreContent, "No scores yet", 22, UIKit.Ink, TextAlignmentOptions.Center, FontStyles.Italic);
            none.rectTransform.sizeDelta = new Vector2(600, 28);
            return;
        }
        for (int i = 0; i < Mathf.Min(5, list.Count); i++)
        {
            TMP_Text row = UIKit.Label(scoreContent, (i + 1) + ".  " + list[i].name + "  -  " + list[i].nights + " nights", 24, UIKit.Ink, TextAlignmentOptions.Center, FontStyles.Normal);
            row.rectTransform.sizeDelta = new Vector2(600, 30);
        }
    }
}
