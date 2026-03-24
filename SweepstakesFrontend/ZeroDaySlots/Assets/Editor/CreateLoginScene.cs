using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Builds the LoginScene from scratch.
/// Run via: ZeroDay Platform > Create Login Scene
/// </summary>
public static class CreateLoginScene
{
    // ── Cyberpunk colour palette ──────────────────────────────────────────
    static readonly Color ColBg         = new Color(0.04f, 0.04f, 0.09f, 1f);
    static readonly Color ColPanel      = new Color(0.07f, 0.07f, 0.14f, 0.97f);
    static readonly Color ColAccent     = new Color(0.00f, 1.00f, 0.90f, 1f);   // cyan
    static readonly Color ColDim        = new Color(0.45f, 0.45f, 0.55f, 1f);
    static readonly Color ColInputBg    = new Color(0.03f, 0.03f, 0.07f, 1f);
    static readonly Color ColBtnPrimary = new Color(0.00f, 0.75f, 0.70f, 1f);   // teal fill
    static readonly Color ColBtnSecBg   = new Color(0.06f, 0.06f, 0.12f, 1f);   // dark fill
    static readonly Color ColSepLine    = new Color(0.00f, 1.00f, 0.90f, 0.20f);

    const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string SavePath = "Assets/Scenes/LoginScene.unity";

    // ── Entry point ───────────────────────────────────────────────────────
    [MenuItem("ZeroDay Platform/Create Login Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            // Fallback: search project
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        if (font == null)
            Debug.LogWarning("[CreateLoginScene] Could not find a TMP_FontAsset. Text will use TMP default.");

        // ── Platform Services (persists across scenes) ────────────────────
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
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Full-screen background ────────────────────────────────────────
        var bg = MakeImage(canvasGO.transform, "Background", ColBg);
        FullStretch(bg);

        // ── Centred login panel (480 × 600) ───────────────────────────────
        var panel = MakeImage(canvasGO.transform, "LoginPanel", ColPanel);
        SetRect(panel, 0f, 0f, 480f, 600f);

        var panelOutline = panel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor    = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.55f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        // ── Panel contents (laid out top → bottom) ────────────────────────
        float y = 248f;

        // Title
        var titleTMP = AddTMP(panel, "TitleText", "ZERO DAY PLATFORM",
                              font, 30f, ColAccent, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(titleTMP.rectTransform, 0f, y, 440f, 46f);  y -= 40f;

        // Subtitle
        var subTMP = AddTMP(panel, "SubtitleText", "ACCESS TERMINAL  v2.0",
                            font, 13f, ColDim, FontStyles.Normal, TextAlignmentOptions.Center);
        SetRect(subTMP.rectTransform, 0f, y, 440f, 24f);  y -= 34f;

        // Separator
        var sepTMP = AddTMP(panel, "Separator",
                            "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
                            font, 10f, ColSepLine, FontStyles.Normal, TextAlignmentOptions.Center);
        SetRect(sepTMP.rectTransform, 0f, y, 440f, 16f);  y -= 42f;

        // Username label + input
        var uLabelTMP = AddTMP(panel, "UsernameLabel", "USERNAME",
                               font, 11f, ColAccent, FontStyles.Bold, TextAlignmentOptions.Left);
        SetRect(uLabelTMP.rectTransform, -8f, y, 400f, 18f);  y -= 24f;

        var uInput = MakeInputField(panel, "UsernameInput", "your_handle", font, false);
        SetRect(uInput, 0f, y, 400f, 46f);  y -= 68f;

        // Password label + input
        var pLabelTMP = AddTMP(panel, "PasswordLabel", "PASSWORD",
                               font, 11f, ColAccent, FontStyles.Bold, TextAlignmentOptions.Left);
        SetRect(pLabelTMP.rectTransform, -8f, y, 400f, 18f);  y -= 24f;

        var pInput = MakeInputField(panel, "PasswordInput", "••••••••", font, true);
        SetRect(pInput, 0f, y, 400f, 46f);  y -= 72f;

        // Buttons  (Login left, Register right)
        var loginBtnGO = MakeButton(panel, "LoginButton", "LOGIN",
                                    font, ColBtnPrimary, Color.black, false);
        SetRect(loginBtnGO, -106f, y, 184f, 48f);

        var regBtnGO = MakeButton(panel, "RegisterButton", "REGISTER",
                                  font, ColBtnSecBg, ColAccent, true);
        SetRect(regBtnGO, 106f, y, 184f, 48f);
        y -= 68f;

        // Status text
        var statusTMP = AddTMP(panel, "StatusText",
                               "ENTER CREDENTIALS TO ACCESS THE NETWORK.",
                               font, 12f, ColAccent, FontStyles.Normal, TextAlignmentOptions.Center);
        SetRect(statusTMP.rectTransform, 0f, y, 420f, 48f);
        statusTMP.enableWordWrapping = true;

        // ── Wire LoginController ──────────────────────────────────────────
        var lc            = canvasGO.AddComponent<LoginController>();
        lc.usernameInput  = uInput.GetComponent<TMP_InputField>();
        lc.passwordInput  = pInput.GetComponent<TMP_InputField>();
        lc.loginButton    = loginBtnGO.GetComponent<Button>();
        lc.registerButton = regBtnGO.GetComponent<Button>();
        lc.statusText     = statusTMP;

        // ── Save ──────────────────────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(scene, SavePath);
        AssetDatabase.Refresh();
        Debug.Log($"[ZeroDay] LoginScene saved to {SavePath}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// Creates a child GameObject with an Image component.
    static RectTransform MakeImage(Transform parent, string name, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go.GetComponent<RectTransform>();
    }

    /// Creates a TextMeshProUGUI child and returns it.
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

    /// Creates a TMP_InputField with a fully wired viewport → text area → placeholder/text hierarchy.
    static RectTransform MakeInputField(RectTransform parent, string name,
                                        string placeholder, TMP_FontAsset font, bool isPassword)
    {
        // Root
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        var rootImg = root.AddComponent<Image>();
        rootImg.color = ColInputBg;

        // Left-side accent bar
        var bar = new GameObject("AccentBar");
        bar.transform.SetParent(root.transform, false);
        var barImg  = bar.AddComponent<Image>();
        barImg.color = ColAccent;
        var barRT   = bar.GetComponent<RectTransform>();
        barRT.anchorMin        = new Vector2(0f, 0f);
        barRT.anchorMax        = new Vector2(0f, 1f);
        barRT.pivot            = new Vector2(0f, 0.5f);
        barRT.anchoredPosition = Vector2.zero;
        barRT.sizeDelta        = new Vector2(3f, 0f);

        // Text Area
        var ta   = new GameObject("Text Area");
        ta.transform.SetParent(root.transform, false);
        ta.AddComponent<RectMask2D>();
        var taRT = ta.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(14f, 5f);
        taRT.offsetMax = new Vector2(-8f, -5f);

        // Placeholder
        var ph    = new GameObject("Placeholder");
        ph.transform.SetParent(ta.transform, false);
        var phTMP = ph.AddComponent<TextMeshProUGUI>();
        phTMP.text      = placeholder;
        phTMP.font      = font;
        phTMP.fontSize  = 16f;
        phTMP.color     = new Color(0.35f, 0.35f, 0.40f, 1f);
        phTMP.fontStyle = FontStyles.Italic;
        FullStretch(ph.GetComponent<RectTransform>());

        // Input text
        var txt    = new GameObject("Text");
        txt.transform.SetParent(ta.transform, false);
        var txtTMP = txt.AddComponent<TextMeshProUGUI>();
        txtTMP.text     = "";
        txtTMP.font     = font;
        txtTMP.fontSize = 16f;
        txtTMP.color    = ColAccent;
        FullStretch(txt.GetComponent<RectTransform>());

        // TMP_InputField
        var field = root.AddComponent<TMP_InputField>();
        field.textViewport   = taRT;
        field.textComponent  = txtTMP;
        field.placeholder    = phTMP;
        field.caretColor     = ColAccent;
        field.selectionColor = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.3f);
        if (isPassword)
            field.contentType = TMP_InputField.ContentType.Password;

        // Outline on root
        var outl = root.AddComponent<Outline>();
        outl.effectColor    = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.30f);
        outl.effectDistance = new Vector2(1f, -1f);

        return root.GetComponent<RectTransform>();
    }

    /// Creates a Button with a TextMeshPro label.
    static RectTransform MakeButton(RectTransform parent, string name, string label,
                                    TMP_FontAsset font, Color bgColor, Color textColor,
                                    bool addBorder)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor      = bgColor;
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.15f);
        cb.pressedColor     = Color.Lerp(bgColor, Color.black, 0.25f);
        cb.selectedColor    = cb.highlightedColor;
        btn.colors = cb;

        // Label
        var labelGO  = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text      = label;
        labelTMP.font      = font;
        labelTMP.fontSize  = 15f;
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

    // ── RectTransform utilities ───────────────────────────────────────────

    /// Anchors centre, sets position and size.
    static void SetRect(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    /// Stretches to fill parent.
    static void FullStretch(RectTransform rt)
    {
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }
}
