using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class CreateLobbyScene
{
    // ── Casino palette ────────────────────────────────────────────────────
    static readonly Color ColBg         = new Color(0.06f, 0.02f, 0.03f, 1f);
    static readonly Color ColHeader     = new Color(0.10f, 0.04f, 0.05f, 1f);
    static readonly Color ColCard       = new Color(0.12f, 0.05f, 0.06f, 1f);
    static readonly Color ColGold       = new Color(0.95f, 0.78f, 0.20f, 1f);
    static readonly Color ColGoldDim    = new Color(0.70f, 0.55f, 0.12f, 1f);
    static readonly Color ColCream      = new Color(1.00f, 0.95f, 0.85f, 1f);
    static readonly Color ColDim        = new Color(0.65f, 0.55f, 0.42f, 1f);
    static readonly Color ColRed        = new Color(0.78f, 0.10f, 0.14f, 1f);
    static readonly Color ColGCBadge    = new Color(0.95f, 0.78f, 0.20f, 1f);
    static readonly Color ColSCBadge    = new Color(0.20f, 0.70f, 0.95f, 1f);
    static readonly Color ColSep        = new Color(0.95f, 0.78f, 0.20f, 0.22f);

    const string FontPath      = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string SceneSavePath = "Assets/Scenes/LobbyScene.unity";
    const string PrefabDir     = "Assets/Prefabs";
    const string PrefabPath    = "Assets/Prefabs/GameCard.prefab";

    [MenuItem("ZeroDay Platform/Create Lobby Scene")]
    public static void Build()
    {
        TMP_FontAsset font = LoadFont();
        BuildGameCardPrefab(font); // still build prefab for reference

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var services = new GameObject("PlatformServices");
        services.AddComponent<PlatformManager>();
        services.AddComponent<PlatformNetworkManager>();

        // Camera
        var camGO = new GameObject("Main Camera"); camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = ColBg;
        cam.orthographic = true; cam.depth = -1;
        camGO.AddComponent<AudioListener>();

        // EventSystem
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background
        var bg = MakeRT(canvasGO.transform, "Background");
        bg.gameObject.AddComponent<Image>().color = ColBg;
        FullStretch(bg);

        // ── Header bar (100px) ────────────────────────────────────────────
        var header = MakeRT(canvasGO.transform, "Header");
        header.gameObject.AddComponent<Image>().color = ColHeader;
        header.anchorMin = new Vector2(0f, 1f); header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero; header.sizeDelta = new Vector2(0f, 100f);

        // Gold bottom border on header
        var headerBorder = MakeRT(header, "GoldBorder");
        headerBorder.gameObject.AddComponent<Image>().color = ColGold;
        headerBorder.anchorMin = new Vector2(0f, 0f); headerBorder.anchorMax = new Vector2(1f, 0f);
        headerBorder.pivot = new Vector2(0.5f, 0f);
        headerBorder.anchoredPosition = Vector2.zero; headerBorder.sizeDelta = new Vector2(0f, 2f);

        // Casino name (left)
        var casinoTMP = AddTMP(header, "CasinoTitle", "SCARLET SANDS",
                               font, 26f, ColGold, FontStyles.Bold, TextAlignmentOptions.Left);
        casinoTMP.rectTransform.anchorMin = new Vector2(0f, 0f);
        casinoTMP.rectTransform.anchorMax = new Vector2(0f, 1f);
        casinoTMP.rectTransform.pivot = new Vector2(0f, 0.5f);
        casinoTMP.rectTransform.anchoredPosition = new Vector2(32f, 0f);
        casinoTMP.rectTransform.sizeDelta = new Vector2(340f, 0f);

        // Welcome (centre)
        var welcomeTMP = AddTMP(header, "WelcomeText", "WELCOME BACK, PLAYER",
                                font, 15f, ColDim, FontStyles.Normal, TextAlignmentOptions.Center);
        welcomeTMP.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        welcomeTMP.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        welcomeTMP.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        welcomeTMP.rectTransform.anchoredPosition = Vector2.zero;
        welcomeTMP.rectTransform.sizeDelta = new Vector2(500f, 0f);

        // GC badge
        var gcBadge = BuildBadge(header, "GCBadge", "GC", "10,000", font, ColGCBadge);
        gcBadge.anchorMin = new Vector2(1f, 0.5f); gcBadge.anchorMax = new Vector2(1f, 0.5f);
        gcBadge.pivot = new Vector2(1f, 0.5f);
        gcBadge.anchoredPosition = new Vector2(-230f, 0f); gcBadge.sizeDelta = new Vector2(175f, 56f);

        // SC badge
        var scBadge = BuildBadge(header, "SCBadge", "SC", "50", font, ColSCBadge);
        scBadge.anchorMin = new Vector2(1f, 0.5f); scBadge.anchorMax = new Vector2(1f, 0.5f);
        scBadge.pivot = new Vector2(1f, 0.5f);
        scBadge.anchoredPosition = new Vector2(-36f, 0f); scBadge.sizeDelta = new Vector2(175f, 56f);

        // Logout button (overlaps right edge — reposition to avoid badges)
        var logoutBtnRT = MakeButton(header, "LogoutButton", "LOG OUT",
                                     font, ColRed, ColCream, false);
        logoutBtnRT.anchorMin = new Vector2(1f, 0.5f); logoutBtnRT.anchorMax = new Vector2(1f, 0.5f);
        logoutBtnRT.pivot = new Vector2(1f, 0.5f);
        logoutBtnRT.anchoredPosition = new Vector2(-430f, 0f); logoutBtnRT.sizeDelta = new Vector2(110f, 48f);

        // ── Section label ─────────────────────────────────────────────────
        var sectionLabelTMP = AddTMP(canvasGO.transform, "SectionLabel", "GAME LOBBY",
                                     font, 16f, ColGold, FontStyles.Bold, TextAlignmentOptions.Left);
        sectionLabelTMP.rectTransform.anchorMin = new Vector2(0f, 1f);
        sectionLabelTMP.rectTransform.anchorMax = new Vector2(0f, 1f);
        sectionLabelTMP.rectTransform.pivot = new Vector2(0f, 1f);
        sectionLabelTMP.rectTransform.anchoredPosition = new Vector2(48f, -118f);
        sectionLabelTMP.rectTransform.sizeDelta = new Vector2(300f, 28f);

        // Gold separator line
        var sepRT = MakeRT(canvasGO.transform, "GoldSep");
        sepRT.gameObject.AddComponent<Image>().color = ColSep;
        sepRT.anchorMin = new Vector2(0f, 1f); sepRT.anchorMax = new Vector2(1f, 1f);
        sepRT.pivot = new Vector2(0.5f, 1f);
        sepRT.anchoredPosition = new Vector2(0f, -152f); sepRT.sizeDelta = new Vector2(-80f, 1f);

        // ── Game card scroll area ─────────────────────────────────────────
        var scrollRT = MakeRT(canvasGO.transform, "GameScrollArea");
        scrollRT.anchorMin = new Vector2(0f, 0f); scrollRT.anchorMax = new Vector2(1f, 1f);
        scrollRT.offsetMin = new Vector2(40f, 60f); scrollRT.offsetMax = new Vector2(-40f, -168f);

        var scrollRect = scrollRT.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false; scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;

        var viewportRT = MakeRT(scrollRT, "Viewport");
        FullStretch(viewportRT);
        viewportRT.gameObject.AddComponent<RectMask2D>();
        scrollRect.viewport = viewportRT;

        var contentRT = MakeRT(viewportRT, "GameCardContainer");
        contentRT.anchorMin = new Vector2(0f, 1f); contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero; contentRT.sizeDelta = new Vector2(0f, 0f);

        var grid = contentRT.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(380f, 240f);
        grid.spacing = new Vector2(28f, 28f);
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperLeft;

        var csf = contentRT.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // ── Footer status bar ─────────────────────────────────────────────
        var footer = MakeRT(canvasGO.transform, "Footer");
        footer.gameObject.AddComponent<Image>().color = ColHeader;
        footer.anchorMin = new Vector2(0f, 0f); footer.anchorMax = new Vector2(1f, 0f);
        footer.pivot = new Vector2(0.5f, 0f);
        footer.anchoredPosition = Vector2.zero; footer.sizeDelta = new Vector2(0f, 52f);

        // Gold top border on footer
        var footerBorder = MakeRT(footer, "GoldBorder");
        footerBorder.gameObject.AddComponent<Image>().color = ColGold;
        footerBorder.anchorMin = new Vector2(0f, 1f); footerBorder.anchorMax = new Vector2(1f, 1f);
        footerBorder.pivot = new Vector2(0.5f, 1f);
        footerBorder.anchoredPosition = Vector2.zero; footerBorder.sizeDelta = new Vector2(0f, 1f);

        var statusTMP = AddTMP(footer, "StatusText", "LOADING GAME CATALOG...",
                               font, 12f, ColDim, FontStyles.Normal, TextAlignmentOptions.Left);
        statusTMP.rectTransform.anchorMin = Vector2.zero; statusTMP.rectTransform.anchorMax = Vector2.one;
        statusTMP.rectTransform.offsetMin = new Vector2(28f, 0f); statusTMP.rectTransform.offsetMax = new Vector2(-28f, 0f);

        // ── Wire LobbyController ──────────────────────────────────────────
        var lc = canvasGO.AddComponent<LobbyController>();
        lc.welcomeText   = welcomeTMP;
        lc.gcBalanceText = gcBadge.Find("BalanceValue").GetComponent<TextMeshProUGUI>();
        lc.scBalanceText = scBadge.Find("BalanceValue").GetComponent<TextMeshProUGUI>();
        lc.statusText    = statusTMP;
        lc.logoutButton  = logoutBtnRT.GetComponent<Button>();
        lc.gameCardContainer = contentRT;

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, SceneSavePath);
        AssetDatabase.Refresh();
        Debug.Log($"[ZeroDay] LobbyScene saved to {SceneSavePath}");
    }

    static void BuildGameCardPrefab(TMP_FontAsset font)
    {
        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        var card = new GameObject("GameCard");
        card.AddComponent<Image>().color = ColCard;
        card.AddComponent<GameCardController>();

        var cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(ColGold.r, ColGold.g, ColGold.b, 0.45f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        // Gold top stripe
        var stripe = new GameObject("GoldStripe"); stripe.transform.SetParent(card.transform, false);
        stripe.AddComponent<Image>().color = ColGold;
        var sRT = stripe.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 1f); sRT.anchorMax = new Vector2(1f, 1f);
        sRT.pivot = new Vector2(0.5f, 1f); sRT.anchoredPosition = Vector2.zero; sRT.sizeDelta = new Vector2(0f, 5f);

        var nameTMP = AddTMP(card.GetComponent<RectTransform>(), "GameName",
                             "GAME TITLE", font, 20f, ColGold, FontStyles.Bold, TextAlignmentOptions.Left);
        nameTMP.rectTransform.anchorMin = new Vector2(0f, 1f); nameTMP.rectTransform.anchorMax = new Vector2(1f, 1f);
        nameTMP.rectTransform.pivot = new Vector2(0.5f, 1f);
        nameTMP.rectTransform.anchoredPosition = new Vector2(0f, -22f); nameTMP.rectTransform.sizeDelta = new Vector2(-28f, 32f);

        var descTMP = AddTMP(card.GetComponent<RectTransform>(), "Description",
                             "Game description.", font, 12f, ColDim, FontStyles.Normal, TextAlignmentOptions.Left);
        descTMP.enableWordWrapping = true;
        descTMP.rectTransform.anchorMin = Vector2.zero; descTMP.rectTransform.anchorMax = Vector2.one;
        descTMP.rectTransform.offsetMin = new Vector2(16f, 62f); descTMP.rectTransform.offsetMax = new Vector2(-16f, -58f);

        var playBtnRT = MakeButton(card.GetComponent<RectTransform>(), "PlayButton", "PLAY NOW",
                                   font, ColGold, new Color(0.08f, 0.03f, 0.01f), false);
        playBtnRT.anchorMin = new Vector2(0f, 0f); playBtnRT.anchorMax = new Vector2(1f, 0f);
        playBtnRT.pivot = new Vector2(0.5f, 0f);
        playBtnRT.anchoredPosition = new Vector2(0f, 14f); playBtnRT.sizeDelta = new Vector2(-28f, 46f);

        var gcc = card.GetComponent<GameCardController>();
        gcc.gameNameText = nameTMP; gcc.descriptionText = descTMP; gcc.playButton = playBtnRT.GetComponent<Button>();

        PrefabUtility.SaveAsPrefabAsset(card, PrefabPath);
        Object.DestroyImmediate(card);
    }

    static RectTransform BuildBadge(RectTransform parent, string name,
                                    string currencyLabel, string value,
                                    TMP_FontAsset font, Color accentCol)
    {
        var badge = MakeRT(parent, name);
        badge.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.02f, 0.03f, 1f);
        var outl = badge.gameObject.AddComponent<Outline>();
        outl.effectColor = new Color(accentCol.r, accentCol.g, accentCol.b, 0.55f);
        outl.effectDistance = new Vector2(1f, -1f);

        var labelTMP = AddTMP(badge, "CurrencyLabel", currencyLabel,
                              font, 11f, accentCol, FontStyles.Bold, TextAlignmentOptions.Left);
        labelTMP.rectTransform.anchorMin = new Vector2(0f, 0.5f); labelTMP.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        labelTMP.rectTransform.pivot = new Vector2(0f, 0.5f);
        labelTMP.rectTransform.anchoredPosition = new Vector2(10f, 0f); labelTMP.rectTransform.sizeDelta = new Vector2(32f, 20f);

        var valueTMP = AddTMP(badge, "BalanceValue", value,
                              font, 19f, ColCream, FontStyles.Bold, TextAlignmentOptions.Right);
        valueTMP.rectTransform.anchorMin = Vector2.zero; valueTMP.rectTransform.anchorMax = Vector2.one;
        valueTMP.rectTransform.offsetMin = new Vector2(40f, 0f); valueTMP.rectTransform.offsetMax = new Vector2(-10f, 0f);

        return badge;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static RectTransform MakeRT(Transform parent, string name)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static TextMeshProUGUI AddTMP(RectTransform parent, string name, string text,
                                  TMP_FontAsset font, float size, Color color,
                                  FontStyles style, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.font = font; tmp.fontSize = size;
        tmp.color = color; tmp.fontStyle = style; tmp.alignment = alignment;
        return tmp;
    }

    static TextMeshProUGUI AddTMP(Transform parent, string name, string text,
                                  TMP_FontAsset font, float size, Color color,
                                  FontStyles style, TextAlignmentOptions alignment)
        => AddTMP(parent.GetComponent<RectTransform>() ??
                  parent.gameObject.AddComponent<RectTransform>(),
                  name, text, font, size, color, style, alignment);

    static RectTransform MakeButton(RectTransform parent, string name, string label,
                                    TMP_FontAsset font, Color bgColor, Color textColor, bool addBorder)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgColor;
        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.18f);
        cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.28f);
        btn.colors = cb;

        var lgo = new GameObject("Label"); lgo.transform.SetParent(go.transform, false);
        var lTMP = lgo.AddComponent<TextMeshProUGUI>();
        lTMP.text = label; lTMP.font = font; lTMP.fontSize = 14f;
        lTMP.color = textColor; lTMP.fontStyle = FontStyles.Bold;
        lTMP.alignment = TextAlignmentOptions.Center;
        FullStretch(lgo.GetComponent<RectTransform>());

        if (addBorder)
        {
            var outl = go.AddComponent<Outline>();
            outl.effectColor = new Color(ColGold.r, ColGold.g, ColGold.b, 0.60f);
            outl.effectDistance = new Vector2(1f, -1f);
        }
        return go.GetComponent<RectTransform>();
    }

    static void FullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static TMP_FontAsset LoadFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font != null) return font;
        var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }
}
