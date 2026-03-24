using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class CreateSlotsScene
{
    // ── Casino palette ────────────────────────────────────────────────────
    static readonly Color ColBg        = new Color(0.06f, 0.02f, 0.03f, 1f);
    static readonly Color ColPanel     = new Color(0.10f, 0.04f, 0.05f, 1f);
    static readonly Color ColGridBg    = new Color(0.08f, 0.03f, 0.04f, 1f);
    static readonly Color ColCell      = new Color(0.11f, 0.04f, 0.05f, 1f);
    static readonly Color ColGold      = new Color(0.95f, 0.78f, 0.20f, 1f);
    static readonly Color ColCream     = new Color(1.00f, 0.95f, 0.85f, 1f);
    static readonly Color ColDim       = new Color(0.65f, 0.55f, 0.42f, 1f);
    static readonly Color ColRed       = new Color(0.78f, 0.10f, 0.14f, 1f);
    static readonly Color ColSpinBtn   = new Color(0.75f, 0.10f, 0.13f, 1f);  // rich casino red

    const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string SavePath = "Assets/Scenes/ZeroDaySlots.unity";

    static readonly Dictionary<string, string> SymbolPaths = new Dictionary<string, string>
    {
        { "Wire",      "Assets/Art_Symbols/KeyLogger.png" },
        { "Cable",     "Assets/Art_Symbols/LowTier_CyberneticKey.png" },
        { "Fan",       "Assets/Art_Symbols/lense.png" },
        { "Drive",     "Assets/Art_Symbols/RetinaScanner.png" },
        { "Battery",   "Assets/Art_Symbols/EmpGranade.png" },
        { "RAM",       "Assets/Art_Symbols/Core.png" },
        { "Microchip", "Assets/Art_Symbols/LowTier_Microchip.png" },
        { "Skull",     "Assets/Art_Symbols/Icon_CyberSkull.png" },
        { "Neon",      "Assets/Art_Symbols/WildSymbol.png" },
        { "Seven",     "Assets/Art_Symbols/ZeroDayExploit.png" },
    };

    [MenuItem("ZeroDay Platform/Create Slots Scene")]
    public static void Build()
    {
        var font  = LoadFont();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var gcGO = new GameObject("GameController");
        var slot = gcGO.AddComponent<ZeroDaySlotController>();
        var net  = gcGO.AddComponent<SweepstakesNetworkManager>();
        var grid = gcGO.AddComponent<ZeroDayGridManager>();
        net.lobbySceneName = "LobbyScene";

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
        var bgRT  = MakeRT(canvasGO.transform, "Background");
        var bgImg = bgRT.gameObject.AddComponent<Image>();
        bgImg.color = ColBg; FullStretch(bgRT);

        // Win flash
        var flashRT  = MakeRT(canvasGO.transform, "WinFlash");
        var flashImg = flashRT.gameObject.AddComponent<Image>();
        flashImg.color = new Color(1, 1, 1, 0); flashImg.raycastTarget = false;
        FullStretch(flashRT);

        // Shake container
        var shakeRT = MakeRT(canvasGO.transform, "ShakeContainer");
        FullStretch(shakeRT);

        // ── Top bar ───────────────────────────────────────────────────────
        var topBar = MakePanel(shakeRT, "TopBar", ColPanel, 0, 1, 1, 1, 0.5f, 1f, 0, 0, 0, 80f);

        // Gold bottom border
        var topBorder = MakeRT(topBar, "GoldBorder");
        topBorder.gameObject.AddComponent<Image>().color = ColGold;
        topBorder.anchorMin = new Vector2(0f, 0f); topBorder.anchorMax = new Vector2(1f, 0f);
        topBorder.pivot = new Vector2(0.5f, 0f);
        topBorder.anchoredPosition = Vector2.zero; topBorder.sizeDelta = new Vector2(0f, 2f);

        // Back button
        var backBtnRT = MakeButton(topBar, "BackButton", "◄  LOBBY",
                                   font, new Color(0.08f, 0.03f, 0.04f), ColGold, true, 13f);
        AnchorLeft(backBtnRT, 20f, 140f, 50f);
        UnityEventTools.AddPersistentListener(backBtnRT.GetComponent<Button>().onClick, net.ReturnToLobby);

        // Title
        var titleTMP = AddTMP(topBar, "TitleText", "SCARLET SANDS  SLOTS",
                              font, 24f, ColGold, FontStyles.Bold, TextAlignmentOptions.Center);
        titleTMP.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        titleTMP.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        titleTMP.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        titleTMP.rectTransform.anchoredPosition = Vector2.zero;
        titleTMP.rectTransform.sizeDelta = new Vector2(600f, 0f);

        // Balance (right)
        var balanceTMP = AddTMP(topBar, "BalanceText", "BALANCE:\n10,000 GC",
                                font, 15f, ColGold, FontStyles.Bold, TextAlignmentOptions.Right);
        AnchorRight(balanceTMP.rectTransform, 24f, 300f, 0f);

        // ── Slot machine frame ────────────────────────────────────────────
        // Outer decorative frame (slightly larger than grid)
        var frameOuter = MakeRT(shakeRT, "FrameOuter");
        frameOuter.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.06f, 0.07f, 1f);
        frameOuter.anchorMin = new Vector2(0.5f, 0.5f); frameOuter.anchorMax = new Vector2(0.5f, 0.5f);
        frameOuter.pivot = new Vector2(0.5f, 0.5f);
        frameOuter.anchoredPosition = new Vector2(0f, 28f); frameOuter.sizeDelta = new Vector2(524f, 524f);
        var frameOuterOutline = frameOuter.gameObject.AddComponent<Outline>();
        frameOuterOutline.effectColor = new Color(ColGold.r, ColGold.g, ColGold.b, 0.80f);
        frameOuterOutline.effectDistance = new Vector2(3f, -3f);

        // Inner grid background
        var gridFrameRT = MakeRT(shakeRT, "GridFrame");
        gridFrameRT.gameObject.AddComponent<Image>().color = ColGridBg;
        gridFrameRT.anchorMin = new Vector2(0.5f, 0.5f); gridFrameRT.anchorMax = new Vector2(0.5f, 0.5f);
        gridFrameRT.pivot = new Vector2(0.5f, 0.5f);
        gridFrameRT.anchoredPosition = new Vector2(0f, 28f); gridFrameRT.sizeDelta = new Vector2(500f, 500f);
        var frameInnerOutline = gridFrameRT.gameObject.AddComponent<Outline>();
        frameInnerOutline.effectColor = new Color(ColGold.r, ColGold.g, ColGold.b, 0.35f);
        frameInnerOutline.effectDistance = new Vector2(1f, -1f);

        // Reel dividers (gold tint)
        float[] divX = { -150f, -55f, 41f, 136f };
        foreach (float dx in divX)
        {
            var div = MakeRT(gridFrameRT, "ReelDivider");
            div.gameObject.AddComponent<Image>().color = new Color(ColGold.r, ColGold.g, ColGold.b, 0.15f);
            div.anchorMin = new Vector2(0.5f, 0.5f); div.anchorMax = new Vector2(0.5f, 0.5f);
            div.pivot = new Vector2(0.5f, 0.5f);
            div.anchoredPosition = new Vector2(dx, 0f); div.sizeDelta = new Vector2(2f, 488f);
        }

        // Grid container
        var gridContainerRT = MakeRT(gridFrameRT, "GridContainer");
        FullStretch(gridContainerRT);
        var glg = gridContainerRT.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(90f, 90f); glg.spacing = new Vector2(5f, 5f);
        glg.padding = new RectOffset(6, 6, 6, 6);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 5; glg.childAlignment = TextAnchor.UpperLeft;

        var gridCells = new Image[25];
        for (int i = 0; i < 25; i++)
        {
            var cellRT  = MakeRT(gridContainerRT, $"Cell_{i:D2}");
            var cellImg = cellRT.gameObject.AddComponent<Image>();
            cellImg.color = ColCell;
            var cellOutline = cellRT.gameObject.AddComponent<Outline>();
            cellOutline.effectColor = new Color(ColGold.r, ColGold.g, ColGold.b, 0.10f);
            cellOutline.effectDistance = new Vector2(1f, -1f);
            gridCells[i] = cellImg;
        }

        // ── Control bar ───────────────────────────────────────────────────
        var controlBar = MakePanel(shakeRT, "ControlBar", ColPanel,
                                   0, 0, 1, 0, 0.5f, 0f, 0, 48f, 0, 110f);

        // Gold top border on control bar
        var ctrlBorder = MakeRT(controlBar, "GoldBorder");
        ctrlBorder.gameObject.AddComponent<Image>().color = ColGold;
        ctrlBorder.anchorMin = new Vector2(0f, 1f); ctrlBorder.anchorMax = new Vector2(1f, 1f);
        ctrlBorder.pivot = new Vector2(0.5f, 1f);
        ctrlBorder.anchoredPosition = Vector2.zero; ctrlBorder.sizeDelta = new Vector2(0f, 2f);

        // Decrease bet
        var decBtnRT = MakeButton(controlBar, "DecreaseBetButton", "−",
                                  font, new Color(0.08f, 0.03f, 0.04f), ColGold, true, 30f);
        AnchorLeft(decBtnRT, 110f, 72f, 68f);

        // Currency / bet text
        var currencyModeTMP = AddTMP(controlBar, "CurrencyModeText", "GC  |  BET: 100",
                                     font, 15f, ColGold, FontStyles.Bold, TextAlignmentOptions.Center);
        AnchorLeft(currencyModeTMP.rectTransform, 220f, 180f, 68f);

        // Increase bet
        var incBtnRT = MakeButton(controlBar, "IncreaseBetButton", "+",
                                  font, new Color(0.08f, 0.03f, 0.04f), ColGold, true, 30f);
        AnchorLeft(incBtnRT, 340f, 72f, 68f);

        // SPIN button — large red casino button centred
        var spinBtnRT = MakeButton(controlBar, "SpinButton", "S P I N",
                                   font, ColSpinBtn, ColCream, false, 28f);
        spinBtnRT.anchorMin = new Vector2(0.5f, 0.5f); spinBtnRT.anchorMax = new Vector2(0.5f, 0.5f);
        spinBtnRT.pivot = new Vector2(0.5f, 0.5f);
        spinBtnRT.anchoredPosition = Vector2.zero; spinBtnRT.sizeDelta = new Vector2(240f, 82f);
        var spinOutline = spinBtnRT.gameObject.AddComponent<Outline>();
        spinOutline.effectColor = new Color(ColGold.r, ColGold.g, ColGold.b, 0.60f);
        spinOutline.effectDistance = new Vector2(2f, -2f);

        // Toggle currency
        var toggleBtnRT = MakeButton(controlBar, "ToggleCurrencyButton", "GC / SC",
                                     font, new Color(0.08f, 0.03f, 0.04f), ColGold, true, 14f);
        AnchorRight(toggleBtnRT, 220f, 170f, 60f);

        // ── Status bar ────────────────────────────────────────────────────
        var statusBar = MakePanel(shakeRT, "StatusBar", new Color(0.05f, 0.02f, 0.02f),
                                  0, 0, 1, 0, 0.5f, 0f, 0, 0, 0, 44f);
        var statusTMP = AddTMP(statusBar, "StatusText",
                               "CONNECTING TO SERVER...",
                               font, 13f, ColDim, FontStyles.Normal, TextAlignmentOptions.Left);
        statusTMP.rectTransform.anchorMin = Vector2.zero; statusTMP.rectTransform.anchorMax = Vector2.one;
        statusTMP.rectTransform.offsetMin = new Vector2(20f, 0f); statusTMP.rectTransform.offsetMax = new Vector2(-20f, 0f);

        // ── Bonus panel ───────────────────────────────────────────────────
        var bonusGO = new GameObject("BonusGamePanel");
        bonusGO.transform.SetParent(canvasGO.transform, false);
        bonusGO.AddComponent<Image>().color = new Color(0.04f, 0.01f, 0.01f, 0.96f);
        FullStretch(bonusGO.GetComponent<RectTransform>());
        bonusGO.SetActive(false);

        // Gold border on bonus panel
        var bonusOutline = bonusGO.AddComponent<Outline>();
        bonusOutline.effectColor = new Color(ColGold.r, ColGold.g, ColGold.b, 0.60f);
        bonusOutline.effectDistance = new Vector2(3f, -3f);

        var bonusRT = bonusGO.GetComponent<RectTransform>();
        var bonusTitleTMP = AddTMP(bonusRT, "BonusTitle", "BONUS ROUND",
                                   font, 52f, ColGold, FontStyles.Bold, TextAlignmentOptions.Center);
        bonusTitleTMP.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        bonusTitleTMP.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        bonusTitleTMP.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        bonusTitleTMP.rectTransform.anchoredPosition = new Vector2(0f, 130f);
        bonusTitleTMP.rectTransform.sizeDelta = new Vector2(900f, 72f);

        var bonusDescTMP = AddTMP(bonusRT, "BonusDescription",
                                  "SCATTER BONUS TRIGGERED\n10x MULTIPLIER APPLIED TO YOUR WAGER",
                                  font, 24f, ColCream, FontStyles.Normal, TextAlignmentOptions.Center);
        bonusDescTMP.enableWordWrapping = true;
        bonusDescTMP.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        bonusDescTMP.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        bonusDescTMP.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        bonusDescTMP.rectTransform.anchoredPosition = new Vector2(0f, 20f);
        bonusDescTMP.rectTransform.sizeDelta = new Vector2(800f, 100f);

        var claimBtnRT = MakeButton(bonusRT, "ClaimButton", "COLLECT WINNINGS",
                                    font, ColGold, new Color(0.08f, 0.03f, 0.01f), false, 22f);
        claimBtnRT.anchorMin = new Vector2(0.5f, 0.5f); claimBtnRT.anchorMax = new Vector2(0.5f, 0.5f);
        claimBtnRT.pivot = new Vector2(0.5f, 0.5f);
        claimBtnRT.anchoredPosition = new Vector2(0f, -120f); claimBtnRT.sizeDelta = new Vector2(340f, 70f);
        UnityEventTools.AddPersistentListener(claimBtnRT.GetComponent<Button>().onClick, slot.EndBonusMiniGame);

        // ── Wire everything ───────────────────────────────────────────────
        grid.gridCells       = gridCells;
        grid.symbolDirectory = BuildSymbolDirectory();
        net.slotController   = slot;

        slot.networkManager       = net;
        slot.spinButton           = spinBtnRT.GetComponent<Button>();
        slot.toggleCurrencyButton = toggleBtnRT.GetComponent<Button>();
        slot.increaseBetButton    = incBtnRT.GetComponent<Button>();
        slot.decreaseBetButton    = decBtnRT.GetComponent<Button>();
        slot.statusText           = statusTMP;
        slot.currencyModeText     = currencyModeTMP;
        slot.balanceText          = balanceTMP;
        slot.bonusGamePanel       = bonusGO;
        slot.gridManager          = grid;
        slot.screenShakeContainer = shakeRT;
        slot.backgroundImage      = bgImg;
        slot.winFlashImage        = flashImg;

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, SavePath);
        AssetDatabase.Refresh();
        Debug.Log($"[ZeroDay] ZeroDaySlots scene saved to {SavePath}");
    }

    static ZeroDayGridManager.SymbolData[] BuildSymbolDirectory()
    {
        var dir = new ZeroDayGridManager.SymbolData[SymbolPaths.Count];
        int i = 0;
        foreach (var kv in SymbolPaths)
        {
            dir[i] = new ZeroDayGridManager.SymbolData
            {
                symbolName  = kv.Key,
                symbolImage = LoadSprite(kv.Value)
            };
            i++;
        }
        return dir;
    }

    static Sprite LoadSprite(string path)
    {
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;
        string filename = System.IO.Path.GetFileNameWithoutExtension(path);
        var guids = AssetDatabase.FindAssets($"t:Sprite {filename}");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }

    static RectTransform MakePanel(RectTransform parent, string name, Color color,
                                   float aMinX, float aMinY, float aMaxX, float aMaxY,
                                   float pivotX, float pivotY,
                                   float posX, float posY, float sizeX, float sizeY)
    {
        var rt = MakeRT(parent, name);
        rt.gameObject.AddComponent<Image>().color = color;
        rt.anchorMin = new Vector2(aMinX, aMinY); rt.anchorMax = new Vector2(aMaxX, aMaxY);
        rt.pivot = new Vector2(pivotX, pivotY);
        rt.anchoredPosition = new Vector2(posX, posY); rt.sizeDelta = new Vector2(sizeX, sizeY);
        return rt;
    }

    static void AnchorLeft(RectTransform rt, float x, float w, float h)
    {
        rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f); rt.sizeDelta = new Vector2(w, h);
    }

    static void AnchorRight(RectTransform rt, float xFromRight, float w, float h)
    {
        rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-xFromRight, 0f); rt.sizeDelta = new Vector2(w, h);
    }

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

    static RectTransform MakeButton(RectTransform parent, string name, string label,
                                    TMP_FontAsset font, Color bgColor, Color textColor,
                                    bool addBorder, float fontSize = 15f)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgColor;
        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.20f);
        cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.30f);
        btn.colors = cb;

        var lgo = new GameObject("Label"); lgo.transform.SetParent(go.transform, false);
        var lTMP = lgo.AddComponent<TextMeshProUGUI>();
        lTMP.text = label; lTMP.font = font; lTMP.fontSize = fontSize;
        lTMP.color = textColor; lTMP.fontStyle = FontStyles.Bold;
        lTMP.alignment = TextAlignmentOptions.Center;
        FullStretch(lgo.GetComponent<RectTransform>());

        if (addBorder)
        {
            var outl = go.AddComponent<Outline>();
            outl.effectColor = new Color(ColGold.r, ColGold.g, ColGold.b, 0.55f);
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
