using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Builds the ZeroDaySlots scene from scratch, auto-wiring every component reference.
/// Run via: ZeroDay Platform > Create Slots Scene
/// </summary>
public static class CreateSlotsScene
{
    // ── Palette ───────────────────────────────────────────────────────────
    static readonly Color ColBg       = new Color(0.04f, 0.04f, 0.09f, 1f);
    static readonly Color ColPanel    = new Color(0.07f, 0.07f, 0.14f, 0.97f);
    static readonly Color ColGridBg   = new Color(0.05f, 0.05f, 0.10f, 1f);
    static readonly Color ColCell     = new Color(0.08f, 0.08f, 0.16f, 1f);
    static readonly Color ColAccent   = new Color(0.00f, 1.00f, 0.90f, 1f);
    static readonly Color ColGold     = new Color(1.00f, 0.85f, 0.00f, 1f);
    static readonly Color ColDim      = new Color(0.45f, 0.45f, 0.55f, 1f);
    static readonly Color ColBtnPrime = new Color(0.00f, 0.75f, 0.70f, 1f);
    static readonly Color ColBtnDark  = new Color(0.06f, 0.06f, 0.12f, 1f);
    static readonly Color ColBtnDanger= new Color(0.65f, 0.10f, 0.10f, 1f);

    const string FontPath  = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string SavePath  = "Assets/Scenes/ZeroDaySlots.unity";

    // ── Symbol → sprite mapping (edit these paths if you reskin symbols) ──
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

    // ── Entry point ───────────────────────────────────────────────────────
    [MenuItem("ZeroDay Platform/Create Slots Scene")]
    public static void Build()
    {
        var font  = LoadFont();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Game Controller (holds all logic components) ──────────────────
        var gcGO    = new GameObject("GameController");
        var slot    = gcGO.AddComponent<ZeroDaySlotController>();
        var net     = gcGO.AddComponent<SweepstakesNetworkManager>();
        var grid    = gcGO.AddComponent<ZeroDayGridManager>();
        net.lobbySceneName = "LobbyScene";

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

        // ── Background (stays still — gets colour-glitched by controller) ─
        var bgRT  = MakeRT(canvasGO.transform, "Background");
        var bgImg = bgRT.gameObject.AddComponent<Image>();
        bgImg.color = ColBg;
        FullStretch(bgRT);

        // ── Win flash overlay (full screen, invisible by default) ─────────
        var flashRT  = MakeRT(canvasGO.transform, "WinFlash");
        var flashImg = flashRT.gameObject.AddComponent<Image>();
        flashImg.color         = new Color(1, 1, 1, 0);
        flashImg.raycastTarget = false;
        FullStretch(flashRT);

        // ── Shake container (everything inside moves on wins) ─────────────
        var shakeRT = MakeRT(canvasGO.transform, "ShakeContainer");
        FullStretch(shakeRT);

        // ── Top bar ───────────────────────────────────────────────────────
        var topBar = MakePanel(shakeRT, "TopBar", ColPanel, 0, 1, 1, 1, 0.5f, 1f, 0, 0, 0, 70);
        var topOutline = topBar.gameObject.AddComponent<Outline>();
        topOutline.effectColor    = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.20f);
        topOutline.effectDistance = new Vector2(0f, -2f);

        // Back-to-lobby button (top-left)
        var backBtnRT = MakeButton(topBar, "BackButton", "◄  LOBBY",
                                   font, ColBtnDark, ColAccent, true, 13f);
        AnchorLeft(backBtnRT, 20f, 130f, 46f);
        UnityEventTools.AddPersistentListener(backBtnRT.GetComponent<Button>().onClick,
                                              net.ReturnToLobby);

        // Title (centred)
        var titleTMP = AddTMP(topBar, "TitleText", "ZERO  DAY  SLOTS",
                              font, 22f, ColAccent, FontStyles.Bold, TextAlignmentOptions.Center);
        titleTMP.rectTransform.anchorMin        = new Vector2(0.5f, 0f);
        titleTMP.rectTransform.anchorMax        = new Vector2(0.5f, 1f);
        titleTMP.rectTransform.pivot            = new Vector2(0.5f, 0.5f);
        titleTMP.rectTransform.anchoredPosition = Vector2.zero;
        titleTMP.rectTransform.sizeDelta        = new Vector2(500f, 0f);

        // Balance text (top-right)
        var balanceTMP = AddTMP(topBar, "BalanceText", "BALANCE:\n10,000 GC",
                                font, 15f, ColGold, FontStyles.Bold, TextAlignmentOptions.Right);
        AnchorRight(balanceTMP.rectTransform, 20f, 280f, 0f);

        // ── Grid frame ────────────────────────────────────────────────────
        //  Centred, shifted up slightly so it clears the control bar.
        //  Frame: 496 × 496.  Inner GridLayoutGroup: 5×5 cells (90×90, gap 5)
        //  with 6px padding → content width = 6+450+20+6 = 482px → fits in 496.
        var gridFrameRT = MakeRT(shakeRT, "GridFrame");
        gridFrameRT.gameObject.AddComponent<Image>().color = ColGridBg;
        gridFrameRT.anchorMin        = new Vector2(0.5f, 0.5f);
        gridFrameRT.anchorMax        = new Vector2(0.5f, 0.5f);
        gridFrameRT.pivot            = new Vector2(0.5f, 0.5f);
        gridFrameRT.anchoredPosition = new Vector2(0f, 30f);
        gridFrameRT.sizeDelta        = new Vector2(496f, 496f);
        var frameOutline = gridFrameRT.gameObject.AddComponent<Outline>();
        frameOutline.effectColor    = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.55f);
        frameOutline.effectDistance = new Vector2(2f, -2f);

        // ── Reel divider lines (4 vertical cyan lines between columns) ────
        //  Column centres from frame centre: -197, -102, -7, 88, 183
        //  Divider mid-gaps: -149.5, -54.5, 40.5, 135.5 → rounded
        float[] divX   = { -150f, -55f, 41f, 136f };
        float   divH   = 484f;  // frame height minus top+bottom padding
        foreach (float dx in divX)
        {
            var div = MakeRT(gridFrameRT, "ReelDivider");
            div.gameObject.AddComponent<Image>().color = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.18f);
            div.anchorMin        = new Vector2(0.5f, 0.5f);
            div.anchorMax        = new Vector2(0.5f, 0.5f);
            div.pivot            = new Vector2(0.5f, 0.5f);
            div.anchoredPosition = new Vector2(dx, 0f);
            div.sizeDelta        = new Vector2(2f, divH);
        }

        // ── Grid container (GridLayoutGroup → 25 cells) ───────────────────
        var gridContainerRT = MakeRT(gridFrameRT, "GridContainer");
        FullStretch(gridContainerRT);
        var glg = gridContainerRT.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize        = new Vector2(90f, 90f);
        glg.spacing         = new Vector2(5f, 5f);
        glg.padding         = new RectOffset(6, 6, 6, 6);
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 5;
        glg.childAlignment  = TextAnchor.UpperLeft;

        var gridCells = new Image[25];
        for (int i = 0; i < 25; i++)
        {
            var cellRT  = MakeRT(gridContainerRT, $"Cell_{i:D2}");
            var cellImg = cellRT.gameObject.AddComponent<Image>();
            cellImg.color = ColCell;
            var cellOutline = cellRT.gameObject.AddComponent<Outline>();
            cellOutline.effectColor    = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.12f);
            cellOutline.effectDistance = new Vector2(1f, -1f);
            gridCells[i] = cellImg;
        }

        // ── Control bar (100px, sits 40px above bottom — above StatusBar) ─
        var controlBar = MakePanel(shakeRT, "ControlBar", ColPanel,
                                   0, 0, 1, 0, 0.5f, 0f, 0, 40f, 0, 100f);

        // Decrease bet (left zone, x=100)
        var decBtnRT = MakeButton(controlBar, "DecreaseBetButton", "−",
                                  font, ColBtnDark, ColAccent, true, 28f);
        AnchorLeft(decBtnRT, 100f, 70f, 64f);

        // Currency/bet mode text (left zone, x=200)
        var currencyModeTMP = AddTMP(controlBar, "CurrencyModeText", "MODE: GC\nBET: 100",
                                     font, 14f, ColGold, FontStyles.Bold, TextAlignmentOptions.Center);
        AnchorLeft(currencyModeTMP.rectTransform, 200f, 160f, 70f);

        // Increase bet (left zone, x=310)
        var incBtnRT = MakeButton(controlBar, "IncreaseBetButton", "+",
                                  font, ColBtnDark, ColAccent, true, 28f);
        AnchorLeft(incBtnRT, 310f, 70f, 64f);

        // SPIN button (centred)
        var spinBtnRT = MakeButton(controlBar, "SpinButton", "S P I N",
                                   font, ColBtnPrime, Color.black, false, 26f);
        spinBtnRT.anchorMin        = new Vector2(0.5f, 0.5f);
        spinBtnRT.anchorMax        = new Vector2(0.5f, 0.5f);
        spinBtnRT.pivot            = new Vector2(0.5f, 0.5f);
        spinBtnRT.anchoredPosition = Vector2.zero;
        spinBtnRT.sizeDelta        = new Vector2(220f, 76f);

        // Toggle currency (right zone, x=-200 from right)
        var toggleBtnRT = MakeButton(controlBar, "ToggleCurrencyButton", "GC / SC",
                                     font, ColBtnDark, ColAccent, true, 14f);
        AnchorRight(toggleBtnRT, 200f, 160f, 60f);

        // ── Status bar (40px, bottom) ──────────────────────────────────────
        var statusBar = MakePanel(shakeRT, "StatusBar", ColPanel,
                                  0, 0, 1, 0, 0.5f, 0f, 0, 0, 0, 40f);
        var statusTMP = AddTMP(statusBar, "StatusText",
                               "SYSTEM BOOTING. CONNECTING TO SERVER...",
                               font, 13f, ColAccent, FontStyles.Normal, TextAlignmentOptions.Left);
        statusTMP.rectTransform.anchorMin = Vector2.zero;
        statusTMP.rectTransform.anchorMax = Vector2.one;
        statusTMP.rectTransform.offsetMin = new Vector2(20f, 0f);
        statusTMP.rectTransform.offsetMax = new Vector2(-20f, 0f);

        // ── Bonus game panel (full screen, hidden by default) ─────────────
        var bonusGO  = new GameObject("BonusGamePanel");
        bonusGO.transform.SetParent(canvasGO.transform, false);
        bonusGO.AddComponent<Image>().color = new Color(0f, 0f, 0.05f, 0.96f);
        var bonusRT  = bonusGO.GetComponent<RectTransform>();
        FullStretch(bonusRT);
        bonusGO.SetActive(false);

        var bonusTitleTMP = AddTMP(bonusRT, "BonusTitle",
                                   "ZERO-DAY EXPLOIT INITIATED",
                                   font, 42f, ColAccent, FontStyles.Bold, TextAlignmentOptions.Center);
        bonusTitleTMP.rectTransform.anchorMin        = new Vector2(0.5f, 0.5f);
        bonusTitleTMP.rectTransform.anchorMax        = new Vector2(0.5f, 0.5f);
        bonusTitleTMP.rectTransform.pivot            = new Vector2(0.5f, 0.5f);
        bonusTitleTMP.rectTransform.anchoredPosition = new Vector2(0f, 120f);
        bonusTitleTMP.rectTransform.sizeDelta        = new Vector2(900f, 64f);

        var bonusDescTMP = AddTMP(bonusRT, "BonusDescription",
                                  "CRITICAL SCATTER ANOMALY DETECTED\n10× MULTIPLIER PAYOUT APPLIED TO YOUR WAGER",
                                  font, 22f, Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
        bonusDescTMP.enableWordWrapping = true;
        bonusDescTMP.rectTransform.anchorMin        = new Vector2(0.5f, 0.5f);
        bonusDescTMP.rectTransform.anchorMax        = new Vector2(0.5f, 0.5f);
        bonusDescTMP.rectTransform.pivot            = new Vector2(0.5f, 0.5f);
        bonusDescTMP.rectTransform.anchoredPosition = new Vector2(0f, 20f);
        bonusDescTMP.rectTransform.sizeDelta        = new Vector2(800f, 100f);

        var claimBtnRT = MakeButton(bonusRT, "ClaimButton", "CLAIM PAYOUT",
                                    font, ColBtnPrime, Color.black, false, 22f);
        claimBtnRT.anchorMin        = new Vector2(0.5f, 0.5f);
        claimBtnRT.anchorMax        = new Vector2(0.5f, 0.5f);
        claimBtnRT.pivot            = new Vector2(0.5f, 0.5f);
        claimBtnRT.anchoredPosition = new Vector2(0f, -110f);
        claimBtnRT.sizeDelta        = new Vector2(300f, 66f);
        UnityEventTools.AddPersistentListener(claimBtnRT.GetComponent<Button>().onClick,
                                              slot.EndBonusMiniGame);

        // ── Wire ZeroDayGridManager ───────────────────────────────────────
        grid.gridCells       = gridCells;
        grid.symbolDirectory = BuildSymbolDirectory();

        // ── Wire SweepstakesNetworkManager ────────────────────────────────
        net.slotController = slot;

        // ── Wire ZeroDaySlotController ────────────────────────────────────
        slot.networkManager          = net;
        slot.spinButton              = spinBtnRT.GetComponent<Button>();
        slot.toggleCurrencyButton    = toggleBtnRT.GetComponent<Button>();
        slot.increaseBetButton       = incBtnRT.GetComponent<Button>();
        slot.decreaseBetButton       = decBtnRT.GetComponent<Button>();
        slot.statusText              = statusTMP;
        slot.currencyModeText        = currencyModeTMP;
        slot.balanceText             = balanceTMP;
        slot.bonusGamePanel          = bonusGO;
        slot.gridManager             = grid;
        slot.screenShakeContainer    = shakeRT;
        slot.backgroundImage         = bgImg;
        slot.winFlashImage           = flashImg;
        // slot.winParticles left null — Screen Space Overlay canvases render
        // above world-space particle systems. Add a Particle System on a
        // separate World Space canvas (depth > 0) and assign it manually.

        // ── Save ──────────────────────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(scene, SavePath);
        AssetDatabase.Refresh();

        Debug.Log($"[ZeroDay] ZeroDaySlots scene saved to {SavePath}");
        Debug.Log("[ZeroDay] Symbol sprites auto-mapped from Art_Symbols. " +
                  "Check ZeroDayGridManager.symbolDirectory in the Inspector " +
                  "and reassign any sprites that don't look right.");
    }

    // ── Symbol directory builder ──────────────────────────────────────────

    static ZeroDayGridManager.SymbolData[] BuildSymbolDirectory()
    {
        var dir = new ZeroDayGridManager.SymbolData[SymbolPaths.Count];
        int i   = 0;
        foreach (var kv in SymbolPaths)
        {
            dir[i] = new ZeroDayGridManager.SymbolData
            {
                symbolName  = kv.Key,
                symbolImage = LoadSprite(kv.Value)
            };
            if (dir[i].symbolImage == null)
                Debug.LogWarning($"[ZeroDay] Sprite not found for '{kv.Key}' at {kv.Value}. Assign manually in the Inspector.");
            i++;
        }
        return dir;
    }

    static Sprite LoadSprite(string path)
    {
        // Try the direct path first
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;
        // Fall back: find by filename anywhere in the project
        string filename = System.IO.Path.GetFileNameWithoutExtension(path);
        var guids = AssetDatabase.FindAssets($"t:Sprite {filename}");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }

    // ── Layout helpers ────────────────────────────────────────────────────

    /// Creates a full-width panel anchored to a horizontal edge.
    /// anchorY = 0 for bottom, 1 for top.
    static RectTransform MakePanel(RectTransform parent, string name, Color color,
                                   float aMinX, float aMinY, float aMaxX, float aMaxY,
                                   float pivotX, float pivotY,
                                   float posX, float posY, float sizeX, float sizeY)
    {
        var rt = MakeRT(parent, name);
        rt.gameObject.AddComponent<Image>().color = color;
        rt.anchorMin        = new Vector2(aMinX, aMinY);
        rt.anchorMax        = new Vector2(aMaxX, aMaxY);
        rt.pivot            = new Vector2(pivotX, pivotY);
        rt.anchoredPosition = new Vector2(posX, posY);
        rt.sizeDelta        = new Vector2(sizeX, sizeY);
        return rt;
    }

    /// Anchors element to the LEFT of its parent (pivot centre-left).
    static void AnchorLeft(RectTransform rt, float x, float w, float h)
    {
        rt.anchorMin        = new Vector2(0f, 0.5f);
        rt.anchorMax        = new Vector2(0f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta        = new Vector2(w, h);
    }

    /// Anchors element to the RIGHT of its parent (pivot centre-right).
    static void AnchorRight(RectTransform rt, float xFromRight, float w, float h)
    {
        rt.anchorMin        = new Vector2(1f, 0.5f);
        rt.anchorMax        = new Vector2(1f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-xFromRight, 0f);
        rt.sizeDelta        = new Vector2(w, h);
    }

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

    static RectTransform MakeButton(RectTransform parent, string name, string label,
                                    TMP_FontAsset font, Color bgColor, Color textColor,
                                    bool addBorder, float fontSize = 15f)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgColor;

        var btn = go.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = bgColor;
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.18f);
        cb.pressedColor     = Color.Lerp(bgColor, Color.black, 0.28f);
        btn.colors = cb;

        var labelGO  = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text      = label;
        labelTMP.font      = font;
        labelTMP.fontSize  = fontSize;
        labelTMP.color     = textColor;
        labelTMP.fontStyle = FontStyles.Bold;
        labelTMP.alignment = TextAlignmentOptions.Center;
        FullStretch(labelGO.GetComponent<RectTransform>());

        if (addBorder)
        {
            var outl = go.AddComponent<Outline>();
            outl.effectColor    = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.50f);
            outl.effectDistance = new Vector2(1f, -1f);
        }

        return go.GetComponent<RectTransform>();
    }

    static void FullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TMP_FontAsset LoadFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font != null) return font;
        var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        Debug.LogWarning("[CreateSlotsScene] Could not find a TMP_FontAsset.");
        return null;
    }
}
