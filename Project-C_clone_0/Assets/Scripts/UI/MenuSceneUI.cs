using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu scene: Start, character customization, High Scores and Credits menus.
/// Built automatically by ProjectCRuntime, so nothing needs wiring in the scene.
/// </summary>
public class MenuSceneUI : MonoBehaviour
{
    private GameObject main, credits, scores, customization;
    private RectTransform scoreContent;
    private Image characterPreview;
    private Sprite characterPreviewSprite;
    private readonly List<Outline> hairOutlines = new List<Outline>();
    private readonly List<Outline> clothesOutlines = new List<Outline>();

    private const string CreditsText =
        "<b>Game by Jenna</b> - Co-op Farm Defense\n\n" +
        "<b>Pixel art:</b> Cainos - Pixel Art Top Down - Basic; Pixel Art Platformer - Village Props\n" +
        "<b>Player art:</b> Clembod - Warrior Free Set (itch.io; credit appreciated)\n" +
        "<b>Monster art:</b> Monsters Creatures Fantasy 2 (Unity Asset Store)\n" +
        "<b>Font:</b> Liberation Sans - Google and Red Hat (SIL Open Font License 1.1)\n" +
        "<b>Music and sound effects:</b> generated in code for this game\n" +
        "<b>Technology:</b> Unity, Netcode for GameObjects, Unity Multiplayer Services, Authentication, Leaderboards";

    private void Start()
    {
        UIKit.HideOriginalCanvases();
        Canvas canvas = UIKit.MakeCanvas("ProjectC_UI");
        AudioManager.Music(false);

        RectTransform m = UIKit.MakePanel(canvas.transform, "Co-op Farm Defense", new Vector2(520, 520), out main);
        UIKit.MakeButton(m, "Play", UIKit.Green, () => UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby"), new Vector2(380, 56));
        UIKit.MakeButton(m, "Customize Character", UIKit.Sky, ShowCustomization, new Vector2(380, 56));
        UIKit.MakeButton(m, "High Scores", UIKit.Gold, ShowScores, new Vector2(380, 56));
        UIKit.MakeButton(m, "Credits", UIKit.Sky, () => { main.SetActive(false); credits.SetActive(true); }, new Vector2(380, 56));
#if !UNITY_WEBGL
        UIKit.MakeButton(m, "Quit", UIKit.Peach, Application.Quit, new Vector2(380, 56));
#endif

        RectTransform c = UIKit.MakePanel(canvas.transform, "Credits", new Vector2(900, 560), out credits);
        TMP_Text body = UIKit.Label(c, CreditsText, 24, UIKit.Ink, TextAlignmentOptions.TopLeft, FontStyles.Normal);
        body.rectTransform.sizeDelta = new Vector2(820, 340);
        UIKit.MakeButton(c, "Back", UIKit.Peach, () => { credits.SetActive(false); main.SetActive(true); }, new Vector2(240, 60));
        credits.SetActive(false);

        scoreContent = UIKit.MakePanel(canvas.transform, "High Scores", new Vector2(700, 640), out scores);
        scores.SetActive(false);

        RectTransform appearance = UIKit.MakePanel(canvas.transform, "Customize Your Farmer", new Vector2(820, 600), out customization);
        TMP_Text appearanceHint = UIKit.Label(appearance, "Colors save automatically and are visible to other players.", 18,
            UIKit.Ink, TextAlignmentOptions.Center, FontStyles.Italic);
        appearanceHint.rectTransform.sizeDelta = new Vector2(700, 30);
        TMP_Text previewLabel = UIKit.Label(appearance, "Your Farmer", 24, UIKit.Ink);
        previewLabel.rectTransform.sizeDelta = new Vector2(700, 30);
        characterPreview = UIKit.Box(appearance, "CharacterPreview", Color.white, false);
        characterPreview.rectTransform.sizeDelta = new Vector2(150, 155);
        characterPreview.type = Image.Type.Simple;
        characterPreview.preserveAspect = true;
        characterPreview.raycastTarget = false;
        LayoutElement previewLayout = characterPreview.gameObject.AddComponent<LayoutElement>();
        previewLayout.preferredWidth = 150;
        previewLayout.preferredHeight = 155;

        GameObject previewPrefab = Resources.Load<GameObject>("CharacterPreview");
        SpriteRenderer previewRenderer = previewPrefab != null ? previewPrefab.GetComponent<SpriteRenderer>() : null;
        if (previewRenderer == null || previewRenderer.sprite == null)
        {
            Debug.LogError("Project C: character preview sprite is missing from Resources/CharacterPreview.prefab.");
        }
        else
        {
            characterPreviewSprite = previewRenderer.sprite;
        }

        AddColorRow(appearance, "Hair", GameSettings.HairColorNames, GameSettings.HairColors,
            hairOutlines, GameSettings.SetHairColor);
        AddColorRow(appearance, "Clothes", GameSettings.ClothesColorNames, GameSettings.ClothesColors,
            clothesOutlines, GameSettings.SetClothesColor);
        UIKit.MakeButton(appearance, "Back", UIKit.Peach, () =>
        {
            customization.SetActive(false);
            main.SetActive(true);
        }, new Vector2(240, 56));
        customization.SetActive(false);
        RefreshColorSelection();
        RefreshCharacterPreview();
    }

    private void ShowCustomization()
    {
        main.SetActive(false);
        customization.SetActive(true);
        RefreshColorSelection();
    }

    private void AddColorRow(Transform parent, string title, string[] names, Color[] colors,
        List<Outline> outlines, System.Action<int> onSelect)
    {
        RectTransform row = UIKit.Rect(title + "Colors", parent);
        row.sizeDelta = new Vector2(700, 70);
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredWidth = 700;
        rowLayout.preferredHeight = 70;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TMP_Text label = UIKit.Label(row, title, 24, UIKit.Ink, TextAlignmentOptions.MidlineLeft);
        label.rectTransform.sizeDelta = new Vector2(120, 56);

        for (int i = 0; i < names.Length; i++)
        {
            int colorIndex = i;
            Image swatch = UIKit.Box(row, title + "_" + names[i], colors[i]);
            swatch.rectTransform.sizeDelta = new Vector2(100, 54);
            Button button = swatch.gameObject.AddComponent<Button>();
            button.targetGraphic = swatch;

            TMP_Text name = UIKit.Label(swatch.transform, names[i], 16,
                colors[i].grayscale < 0.45f ? Color.white : UIKit.Ink);
            UIKit.Fill(name.rectTransform);

            Outline outline = swatch.gameObject.AddComponent<Outline>();
            outline.effectColor = UIKit.Ink;
            outline.effectDistance = new Vector2(3, -3);
            outline.enabled = false;
            outlines.Add(outline);

            button.onClick.AddListener(() =>
            {
                AudioManager.Play("click", 0.5f);
                onSelect(colorIndex);
                RefreshColorSelection();
                RefreshCharacterPreview();
            });
        }
    }

    private void RefreshCharacterPreview()
    {
        if (characterPreview == null || characterPreviewSprite == null) return;
        characterPreview.sprite = PlayerController.CreateCustomizedSprite(
            characterPreviewSprite, GameSettings.HairColorIndex, GameSettings.ClothesColorIndex);
    }

    private void RefreshColorSelection()
    {
        SetSelectedOutline(hairOutlines, GameSettings.HairColorIndex);
        SetSelectedOutline(clothesOutlines, GameSettings.ClothesColorIndex);
    }

    private static void SetSelectedOutline(List<Outline> outlines, int selected)
    {
        for (int i = 0; i < outlines.Count; i++)
        {
            if (outlines[i] != null) outlines[i].enabled = i == selected;
        }
    }

    private async void ShowScores()
    {
        main.SetActive(false);
        scores.SetActive(true);
        UIKit.Clear(scoreContent);

        AddScoreRows("This computer", HighScores.Local());
        TMP_Text status = UIKit.Label(scoreContent, "Loading global scores...", 22, UIKit.Ink, TextAlignmentOptions.Center, FontStyles.Italic);
        status.rectTransform.sizeDelta = new Vector2(600, 30);

        SessionManager sessionManager = SessionManager.Instance;
        bool ok = sessionManager != null && await sessionManager.InitAsync();
        List<ScoreEntry> global = ok ? await HighScores.GlobalAsync() : new List<ScoreEntry>();
        if (this == null || scoreContent == null) return;

        status.text = global.Count > 0 ? "" :
            !ok ? "Global scores unavailable: " + (sessionManager != null && !string.IsNullOrEmpty(sessionManager.LastError)
                ? sessionManager.LastError : "Unity Gaming Services could not initialize.") :
            !string.IsNullOrEmpty(HighScores.LastError) ? "Global scores unavailable: " + HighScores.LastError :
            "No global scores yet.";
        status.rectTransform.sizeDelta = new Vector2(600, 60);
        status.fontSize = 18;
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
