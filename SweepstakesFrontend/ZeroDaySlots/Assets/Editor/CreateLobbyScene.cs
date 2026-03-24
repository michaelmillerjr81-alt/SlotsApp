using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Builds the LobbyScene and a reusable GameCard prefab from scratch.
/// Run via: ZeroDay Platform > Create Lobby Scene
/// </summary>
public static class CreateLobbyScene
{
    // ── Colour palette (matches LoginScene) ───────────────────────────────
    static readonly Color ColBg         = new Color(0.04f, 0.04f, 0.09f, 1f);
    static readonly Color ColPanel      = new Color(0.07f, 0.07f, 0.14f, 0.97f);
    static readonly Color ColCard       = new Color(0.09f, 0.09f, 0.18f, 1f);
    static readonly Color ColAccent     = new Color(0.00f, 1.00f, 0.90f, 1f);
    static readonly Color ColGold       = new Color(1.00f, 0.85f, 0.00f, 1f);
    static readonly Color ColDim        = new Color(0.45f, 0.45f, 0.55f, 1f);
    static readonly Color ColBtnPrimary = new Color(0.00f, 0.75f, 0.70f, 1f);
    static readonly Color ColBtnDanger  = new Color(0.70f, 0.10f, 0.10f, 1f);
    static readonly Color ColSepLine    = new Color(0.00f, 1.00f, 0.90f, 0.18f);
    static readonly Color ColGCBadge    = new Color(1.00f, 0.82f, 0.00f, 1f);
    static readonly Color ColSCBadge    = new Color(0.00f, 0.80f, 1.00f, 1f);

    const string FontPath      = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string SceneSavePath = "Assets/Scenes/LobbyScene.unity";
    const string PrefabDir     = "Assets/Prefabs";
    const string PrefabPath    = "Assets/Prefabs/GameCard.prefab";

    // ── Entry point ───────────────────────────────────────────────────────
    [MenuItem("ZeroDay Platform/Create Lobby Scene")]
    public static void Build()
    {
        TMP_FontAsset font = LoadFont();

        // Build the GameCard prefab first (LobbyController needs the reference)
        GameObject cardPrefab = BuildGameCardPrefab(font);

        // Now build the scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Platform Services (already exists from LoginScene load, but
        //    we add it here too so the scene works if opened standalone) ──
        var services = new GameObject("PlatformServices");
        services.AddComponent<PlatformManager>();
        services.AddComponent<PlatformNetworkManager>();

        // ── Event System ──────────────────────────────────────────────────
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // ── Canvas ────────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Background ────────────────────────────────────────────────────
        var bg = MakeRT(canvasGO.transform, "Background");
        bg.gameObject.AddComponent<Image>().color = ColBg;
        FullStretch(bg);

        // ── Top header bar ────────────────────────────────────────────────
        var header = MakeRT(canvasGO.transform, "Header");
        header.gameObject.AddComponent<Image>().color = ColPanel;
        //  top edge, full width, 90px tall
        header.anchorMin        = new Vector2(0f, 1f);
        header.anchorMax        = new Vector2(1f, 1f);
        header.pivot            = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta        = new Vector2(0f, 90f);

        var headerOutline = header.gameObject.AddComponent<Outline>();
        headerOutline.effectColor    = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.25f);
        headerOutline.effectDistance = new Vector2(0f, -2f);

        // Platform title (left)
        var platformTMP = AddTMP(header, "PlatformTitle", "ZERO DAY PLATFORM",
                                 font, 22f, ColAccent, FontStyles.Bold, TextAlignmentOptions.Left);
        platformTMP.rectTransform.anchorMin        = new Vector2(0f, 0f);
        platformTMP.rectTransform.anchorMax        = new Vector2(0f, 1f);
        platformTMP.rectTransform.pivot            = new Vector2(0f, 0.5f);
        platformTMP.rectTransform.anchoredPosition = new Vector2(28f, 0f);
        platformTMP.rectTransform.sizeDelta        = new Vector2(400f, 0f);

        // Welcome text (centre)
        var welcomeTMP = AddTMP(header, "WelcomeText", "WELCOME BACK, AGENT",
                                font, 16f, ColDim, FontStyles.Normal, TextAlignmentOptions.Center);
        welcomeTMP.rectTransform.anchorMin        = new Vector2(0.5f, 0f);
        welcomeTMP.rectTransform.anchorMax        = new Vector2(0.5f, 1f);
        welcomeTMP.rectTransform.pivot            = new Vector2(0.5f, 0.5f);
        welcomeTMP.rectTransform.anchoredPosition = Vector2.zero;
        welcomeTMP.rectTransform.sizeDelta        = new Vector2(500f, 0f);

        // Balance badges (right side)
        var gcBadge = BuildBadge(header, "GCBadge", "GC", "10,000", font, ColGCBadge);
        gcBadge.anchorMin        = new Vector2(1f, 0.5f);
        gcBadge.anchorMax        = new Vector2(1f, 0.5f);
        gcBadge.pivot            = new Vector2(1f, 0.5f);
        gcBadge.anchoredPosition = new Vector2(-200f, 0f);
        gcBadge.sizeDelta        = new Vector2(160f, 52f);

        var scBadge = BuildBadge(header, "SCBadge", "SC", "50", font, ColSCBadge);
        scBadge.anchorMin        = new Vector2(1f, 0.5f);
        scBadge.anchorMax        = new Vector2(1f, 0.5f);
        scBadge.pivot            = new Vector2(1f, 0.5f);
        scBadge.anchoredPosition = new Vector2(-28f, 0f);
        scBadge.sizeDelta        = new Vector2(156f, 52f);

        // Logout button (top-right corner of header)
        var logoutBtnRT = MakeButton(header, "LogoutButton", "LOGOUT",
                                     font, ColBtnDanger, Color.white, false);
        logoutBtnRT.anchorMin        = new Vector2(1f, 0.5f);
        logoutBtnRT.anchorMax        = new Vector2(1f, 0.5f);
        logoutBtnRT.pivot            = new Vector2(1f, 0.5f);
        logoutBtnRT.anchoredPosition = new Vector2(-28f, 0f);
        logoutBtnRT.sizeDelta        = new Vector2(110f, 44f);

        // ── Section label ─────────────────────────────────────────────────
        var sectionLabelTMP = AddTMP(canvasGO.transform, "SectionLabel", "SELECT YOUR GAME",
                                     font, 14f, ColAccent, FontStyles.Bold, TextAlignmentOptions.Left);
        sectionLabelTMP.rectTransform.anchorMin        = new Vector2(0f, 1f);
        sectionLabelTMP.rectTransform.anchorMax        = new Vector2(0f, 1f);
        sectionLabelTMP.rectTransform.pivot            = new Vector2(0f, 1f);
        sectionLabelTMP.rectTransform.anchoredPosition = new Vector2(48f, -108f);
        sectionLabelTMP.rectTransform.sizeDelta        = new Vector2(400f, 28f);

        var sectionSep = AddTMP(canvasGO.transform, "SectionSep",
                                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
                                font, 9f, ColSepLine, FontStyles.Normal, TextAlignmentOptions.Left);
        sectionSep.rectTransform.anchorMin        = new Vector2(0f, 1f);
        sectionSep.rectTransform.anchorMax        = new Vector2(1f, 1f);
        sectionSep.rectTransform.pivot            = new Vector2(0f, 1f);
        sectionSep.rectTransform.anchoredPosition = new Vector2(48f, -138f);
        sectionSep.rectTransform.sizeDelta        = new Vector2(-96f, 18f);

        // ── Game card scroll area ─────────────────────────────────────────
        var scrollRT = MakeRT(canvasGO.transform, "GameScrollArea");
        scrollRT.anchorMin        = new Vector2(0f, 0f);
        scrollRT.anchorMax        = new Vector2(1f, 1f);
        scrollRT.offsetMin        = new Vector2(40f, 60f);
        scrollRT.offsetMax        = new Vector2(-40f, -160f);

        var scrollRect = scrollRT.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;
        scrollRect.scrollSensitivity = 30f;

        // Viewport (masks the content)
        var viewportRT = MakeRT(scrollRT, "Viewport");
        FullStretch(viewportRT);
        viewportRT.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0); // invisible mask carrier
        viewportRT.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewportRT;

        // Content container (Grid Layout Group — cards snap into rows)
        var contentRT = MakeRT(viewportRT, "GameCardContainer");
        contentRT.anchorMin        = new Vector2(0f, 1f);
        contentRT.anchorMax        = new Vector2(1f, 1f);
        contentRT.pivot            = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta        = new Vector2(0f, 0f);

        var grid = contentRT.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(340f, 220f);
        grid.spacing         = new Vector2(24f, 24f);
        grid.padding         = new RectOffset(16, 16, 16, 16);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment  = TextAnchor.UpperLeft;

        // Content size fitter so the container grows with cards
        var csf = contentRT.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        // ── Status bar (bottom) ───────────────────────────────────────────
        var footer = MakeRT(canvasGO.transform, "Footer");
        footer.anchorMin        = new Vector2(0f, 0f);
        footer.anchorMax        = new Vector2(1f, 0f);
        footer.pivot            = new Vector2(0.5f, 0f);
        footer.anchoredPosition = Vector2.zero;
        footer.sizeDelta        = new Vector2(0f, 52f);
        footer.gameObject.AddComponent<Image>().color = ColPanel;

        var statusTMP = AddTMP(footer, "StatusText", "LOADING GAME CATALOG...",
                               font, 12f, ColAccent, FontStyles.Normal, TextAlignmentOptions.Left);
        statusTMP.rectTransform.anchorMin        = new Vector2(0f, 0f);
        statusTMP.rectTransform.anchorMax        = new Vector2(1f, 1f);
        statusTMP.rectTransform.offsetMin        = new Vector2(28f, 0f);
        statusTMP.rectTransform.offsetMax        = new Vector2(-28f, 0f);

        // ── Wire LobbyController ──────────────────────────────────────────
        var lc = canvasGO.AddComponent<LobbyController>();
        lc.welcomeText       = welcomeTMP;
        lc.gcBalanceText     = gcBadge.Find("BalanceValue").GetComponent<TextMeshProUGUI>();
        lc.scBalanceText     = scBadge.Find("BalanceValue").GetComponent<TextMeshProUGUI>();
        lc.statusText        = statusTMP;
        lc.logoutButton      = logoutBtnRT.GetComponent<Button>();
        lc.gameCardContainer = contentRT;
        lc.gameCardPrefab    = cardPrefab;

        // ── Save scene ────────────────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(scene, SceneSavePath);
        AssetDatabase.Refresh();
        Debug.Log($"[ZeroDay] LobbyScene saved to {SceneSavePath}");
        Debug.Log($"[ZeroDay] GameCard prefab saved to {PrefabPath}");
    }

    // ── GameCard prefab ───────────────────────────────────────────────────

    static GameObject BuildGameCardPrefab(TMP_FontAsset font)
    {
        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // Root card
        var card = new GameObject("GameCard");
        var cardImg = card.AddComponent<Image>();
        cardImg.color = ColCard;
        card.AddComponent<GameCardController>();

        var cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor    = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.30f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        // Top accent stripe
        var stripe = new GameObject("AccentStripe");
        stripe.transform.SetParent(card.transform, false);
        stripe.AddComponent<Image>().color = ColAccent;
        var stripeRT = stripe.GetComponent<RectTransform>();
        stripeRT.anchorMin        = new Vector2(0f, 1f);
        stripeRT.anchorMax        = new Vector2(1f, 1f);
        stripeRT.pivot            = new Vector2(0.5f, 1f);
        stripeRT.anchoredPosition = Vector2.zero;
        stripeRT.sizeDelta        = new Vector2(0f, 4f);

        // Game name
        var nameTMP = AddTMP(card.GetComponent<RectTransform>(), "GameName",
                             "GAME TITLE", font, 18f, Color.white, FontStyles.Bold,
                             TextAlignmentOptions.Left);
        nameTMP.rectTransform.anchorMin        = new Vector2(0f, 1f);
        nameTMP.rectTransform.anchorMax        = new Vector2(1f, 1f);
        nameTMP.rectTransform.pivot            = new Vector2(0.5f, 1f);
        nameTMP.rectTransform.anchoredPosition = new Vector2(0f, -20f);
        nameTMP.rectTransform.sizeDelta        = new Vector2(-32f, 30f);

        // Description
        var descTMP = AddTMP(card.GetComponent<RectTransform>(), "Description",
                             "Game description goes here.", font, 12f, ColDim,
                             FontStyles.Normal, TextAlignmentOptions.Left);
        descTMP.enableWordWrapping = true;
        descTMP.rectTransform.anchorMin        = new Vector2(0f, 0f);
        descTMP.rectTransform.anchorMax        = new Vector2(1f, 1f);
        descTMP.rectTransform.offsetMin        = new Vector2(16f, 60f);
        descTMP.rectTransform.offsetMax        = new Vector2(-16f, -56f);

        // Play button (bottom of card)
        var playBtnRT = MakeButton(card.GetComponent<RectTransform>(), "PlayButton", "▶  PLAY",
                                   font, ColBtnPrimary, Color.black, false);
        playBtnRT.anchorMin        = new Vector2(0f, 0f);
        playBtnRT.anchorMax        = new Vector2(1f, 0f);
        playBtnRT.pivot            = new Vector2(0.5f, 0f);
        playBtnRT.anchoredPosition = new Vector2(0f, 14f);
        playBtnRT.sizeDelta        = new Vector2(-32f, 44f);

        // Wire GameCardController
        var gcc = card.GetComponent<GameCardController>();
        gcc.gameNameText   = nameTMP;
        gcc.descriptionText = descTMP;
        gcc.playButton     = playBtnRT.GetComponent<Button>();

        // Save as prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(card, PrefabPath);
        Object.DestroyImmediate(card);
        return prefab;
    }

    // ── Balance badge helper ──────────────────────────────────────────────

    /// Creates a small pill-shaped badge showing currency label + amount.
    static RectTransform BuildBadge(RectTransform parent, string name,
                                    string currencyLabel, string value,
                                    TMP_FontAsset font, Color accentCol)
    {
        var badge = MakeRT(parent, name);
        var img   = badge.gameObject.AddComponent<Image>();
        img.color = new Color(0.05f, 0.05f, 0.10f, 1f);

        var outl = badge.gameObject.AddComponent<Outline>();
        outl.effectColor    = new Color(accentCol.r, accentCol.g, accentCol.b, 0.50f);
        outl.effectDistance = new Vector2(1f, -1f);

        // Currency label (e.g. "GC")
        var labelTMP = AddTMP(badge, "CurrencyLabel", currencyLabel,
                              font, 11f, accentCol, FontStyles.Bold, TextAlignmentOptions.Left);
        labelTMP.rectTransform.anchorMin        = new Vector2(0f, 0.5f);
        labelTMP.rectTransform.anchorMax        = new Vector2(0f, 0.5f);
        labelTMP.rectTransform.pivot            = new Vector2(0f, 0.5f);
        labelTMP.rectTransform.anchoredPosition = new Vector2(10f, 0f);
        labelTMP.rectTransform.sizeDelta        = new Vector2(32f, 20f);

        // Value
        var valueTMP = AddTMP(badge, "BalanceValue", value,
                              font, 18f, Color.white, FontStyles.Bold, TextAlignmentOptions.Right);
        valueTMP.rectTransform.anchorMin  = Vector2.zero;
        valueTMP.rectTransform.anchorMax  = Vector2.one;
        valueTMP.rectTransform.offsetMin  = new Vector2(40f, 0f);
        valueTMP.rectTransform.offsetMax  = new Vector2(-10f, 0f);

        return badge;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static RectTransform MakeRT(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static TextMeshProUGUI AddTMP(RectTransform parent, string name, string text,
                                  TMP_FontAsset font, float size, Color color,
                                  FontStyles style, TextAlignmentOptions alignment)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.font      = font;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        return tmp;
    }

    // Overload that accepts a Transform (for non-RectTransform parents)
    static TextMeshProUGUI AddTMP(Transform parent, string name, string text,
                                  TMP_FontAsset font, float size, Color color,
                                  FontStyles style, TextAlignmentOptions alignment)
        => AddTMP(parent.GetComponent<RectTransform>() ??
                  parent.gameObject.AddComponent<RectTransform>(),
                  name, text, font, size, color, style, alignment);

    static RectTransform MakeButton(RectTransform parent, string name, string label,
                                    TMP_FontAsset font, Color bgColor, Color textColor,
                                    bool addBorder)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        go.AddComponent<Image>().color = bgColor;

        var btn = go.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = bgColor;
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.15f);
        cb.pressedColor     = Color.Lerp(bgColor, Color.black, 0.25f);
        btn.colors = cb;

        var labelGO  = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text      = label;
        labelTMP.font      = font;
        labelTMP.fontSize  = 14f;
        labelTMP.color     = textColor;
        labelTMP.fontStyle = FontStyles.Bold;
        labelTMP.alignment = TextAlignmentOptions.Center;
        FullStretch(labelGO.GetComponent<RectTransform>());

        if (addBorder)
        {
            var outl = go.AddComponent<Outline>();
            outl.effectColor    = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.55f);
            outl.effectDistance = new Vector2(1f, -1f);
        }

        return go.GetComponent<RectTransform>();
    }

    static void FullStretch(RectTransform rt)
    {
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    static TMP_FontAsset LoadFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font != null) return font;
        var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        Debug.LogWarning("[CreateLobbyScene] Could not find a TMP_FontAsset.");
        return null;
    }
}
