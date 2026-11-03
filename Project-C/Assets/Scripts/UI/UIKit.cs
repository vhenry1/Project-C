using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Small helpers that build Unity UI (uGUI Canvas, Image, Button, TextMeshPro) in code,
/// using the soft pastel + rounded look from the game doc. Every menu in the game uses these.
/// </summary>
public static class UIKit
{
    public static readonly Color Cream = new Color(1f, 0.96f, 0.88f);
    public static readonly Color Panel = new Color(0.99f, 0.93f, 0.84f, 0.96f);
    public static readonly Color Ink = new Color(0.30f, 0.22f, 0.18f);
    public static readonly Color Green = new Color(0.56f, 0.80f, 0.55f);
    public static readonly Color Peach = new Color(0.98f, 0.72f, 0.62f);
    public static readonly Color Sky = new Color(0.62f, 0.80f, 0.92f);
    public static readonly Color Rose = new Color(0.90f, 0.50f, 0.52f);
    public static readonly Color Gold = new Color(0.98f, 0.82f, 0.40f);
    public static readonly Color Dim = new Color(0.1f, 0.08f, 0.12f, 0.6f);

    private static Sprite rounded;

    // A small rounded-rectangle sprite generated once, 9-sliced so it scales cleanly.
    public static Sprite Rounded
    {
        get
        {
            if (rounded != null) return rounded;
            const int size = 32, r = 8;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Max(r - x, 0, x - (size - 1 - r));
                    int dy = Mathf.Max(r - y, 0, y - (size - 1 - r));
                    tex.SetPixel(x, y, (dx * dx + dy * dy <= r * r) ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return rounded;
        }
    }

    public static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        UnityEngine.Object.DontDestroyOnLoad(es);
    }

    public static Canvas MakeCanvas(string name, int order = 100)
    {
        EnsureEventSystem();
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay stays sharp with the Pixel Perfect Camera
        canvas.sortingOrder = order;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
    }

    public static void Fill(RectTransform rt) { Anchor(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); }

    public static void Center(RectTransform rt, Vector2 size, Vector2 pos = default(Vector2))
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    public static Image Box(Transform parent, string name, Color color, bool rounded = true)
    {
        RectTransform rt = Rect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        if (rounded) { img.sprite = Rounded; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; }
        return img;
    }

    public static TMP_Text Label(Transform parent, string text, int size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Bold)
    {
        RectTransform rt = Rect("Text", parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    public static Button MakeButton(Transform parent, string label, Color color, Action onClick, Vector2 size)
    {
        Image img = Box(parent, "Button_" + label, color);
        img.rectTransform.sizeDelta = size;
        Button b = img.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.82f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        cb.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        b.colors = cb;
        b.targetGraphic = img;
        TMP_Text t = Label(img.transform, label, 26, Ink);
        Fill(t.rectTransform);
        b.onClick.AddListener(() => { AudioManager.Play("click", 0.5f); if (onClick != null) onClick(); });
        LayoutElement le = img.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = size.y; le.preferredWidth = size.x;
        return b;
    }

    // Centered card with a title; returns the content area (vertical layout).
    public static RectTransform MakePanel(Transform parent, string title, Vector2 size, out GameObject root)
    {
        Image dim = Box(parent, "Panel_" + title, Dim, false);
        Fill(dim.rectTransform);
        root = dim.gameObject;

        Image card = Box(dim.transform, "Card", Panel);
        Center(card.rectTransform, size);
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.55f, 0.42f); outline.effectDistance = new Vector2(4, -4);

        TMP_Text head = Label(card.transform, title, 40, Ink);
        Anchor(head.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -80), new Vector2(-20, -16));

        RectTransform content = Rect("Content", card.transform);
        Anchor(content, Vector2.zero, Vector2.one, new Vector2(30, 24), new Vector2(-30, -90));
        VerticalLayoutGroup v = content.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 12; v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = false; v.childControlHeight = false;
        v.childForceExpandWidth = false; v.childForceExpandHeight = false;
        return content;
    }

    public static void Clear(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
    }

    // Disable the original scene canvases so ours replace them.
    public static void HideOriginalCanvases()
    {
        foreach (Canvas c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.transform.parent == null && !c.name.StartsWith("ProjectC")) c.gameObject.SetActive(false);
        }
    }
}
