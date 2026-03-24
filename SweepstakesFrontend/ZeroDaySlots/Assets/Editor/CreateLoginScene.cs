using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class CreateLoginScene
{
    // ── Casino palette ────────────────────────────────────────────────────
    static readonly Color ColBg         = new Color(0.06f, 0.02f, 0.03f, 1f);   // deep burgundy-black
    static readonly Color ColPanel      = new Color(0.13f, 0.05f, 0.06f, 0.98f);
    static readonly Color ColGold       = new Color(0.95f, 0.78f, 0.20f, 1f);   // warm gold
    static readonly Color ColGoldDim    = new Color(0.70f, 0.55f, 0.12f, 1f);
    static readonly Color ColCream      = new Color(1.00f, 0.95f, 0.85f, 1f);
    static readonly Color ColDim        = new Color(0.65f, 0.55f, 0.42f, 1f);
    static readonly Color ColInputBg    = new Color(0.08f, 0.03f, 0.04f, 1f);
    static readonly Color ColRed        = new Color(0.78f, 0.10f, 0.14f, 1f);
    static readonly Color ColSep        = new Color(0.95f, 0.78f, 0.20f, 0.22f);

    const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string SavePath = "Assets/Scenes/LoginScene.unity";

    [MenuItem("ZeroDay Platform/Create Login Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        var services = new GameObject("PlatformServices");
        services.AddComponent<PlatformManager>();
        services.AddComponent<PlatformNetworkManager>();

        // Camera
        var camGO = new GameObject("Main Camera"); camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColBg;
        cam.orthographic = true; cam.depth = -1;
        camGO.AddComponent<AudioListener>();

        // EventSystem
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background
        var bg = MakeImage(canvasGO.transform, "Background", ColBg);
        FullStretch(bg);

        // Vignette overlay — dark edges, slightly transparent
        var vignette = MakeImage(canvasGO.transform, "Vignette", new Color(0f, 0f, 0f, 0.45f));
        FullStretch(vignette);

        // ── Login panel (520 × 640) ───────────────────────────────────────
        var panel = MakeImage(canvasGO.transform, "LoginPanel", ColPanel);
        SetRect(panel, 0f, 0f, 520f, 640f);

        // Gold border outline
        var panelOutline = panel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor    = new Color(ColGold.r, ColGold.g, ColGold.b, 0.70f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        // Gold top stripe
        var topStripe = MakeImage(panel, "TopStripe", ColGold);
        var tsRT = topStripe;
        tsRT.anchorMin = new Vector2(0f, 1f); tsRT.anchorMax = new Vector2(1f, 1f);
        tsRT.pivot = new Vector2(0.5f, 1f);
        tsRT.anchoredPosition = Vector2.zero; tsRT.sizeDelta = new Vector2(0f, 4f);

        float y = 278f;

        // Casino logo / title
        var logoTMP = AddTMP(panel, "LogoText", "VELVET JACKPOT",
                             font, 36f, ColGold, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(logoTMP.rectTransform, 0f, y, 480f, 52f); y -= 42f;

        // Subtitle
        var subTMP = AddTMP(panel, "SubtitleText", "SOCIAL CASINO  |  PLAY FOR FUN",
                            font, 12f, ColDim, FontStyles.Normal, TextAlignmentOptions.Center);
        SetRect(subTMP.rectTransform, 0f, y, 460f, 22f); y -= 36f;

        // Gold separator
        var sepTMP = AddTMP(panel, "Separator", "- - - - - - - - - - - - - - - - - - - -",
                            font, 10f, ColSep, FontStyles.Normal, TextAlignmentOptions.Center);
        SetRect(sepTMP.rectTransform, 0f, y, 460f, 16f); y -= 46f;

        // Username
        var uLabelTMP = AddTMP(panel, "UsernameLabel", "USERNAME",
                               font, 11f, ColGold, FontStyles.Bold, TextAlignmentOptions.Left);
        SetRect(uLabelTMP.rectTransform, -10f, y, 420f, 18f); y -= 26f;
        var uInput = MakeInputField(panel, "UsernameInput", "Enter username...", font, false);
        SetRect(uInput, 0f, y, 420f, 50f); y -= 74f;

        // Password
        var pLabelTMP = AddTMP(panel, "PasswordLabel", "PASSWORD",
                               font, 11f, ColGold, FontStyles.Bold, TextAlignmentOptions.Left);
        SetRect(pLabelTMP.rectTransform, -10f, y, 420f, 18f); y -= 26f;
        var pInput = MakeInputField(panel, "PasswordInput", "Enter password...", font, true);
        SetRect(pInput, 0f, y, 420f, 50f); y -= 76f;

        // Buttons
        var loginBtnGO = MakeButton(panel, "LoginButton", "SIGN IN",
                                    font, ColGold, new Color(0.1f, 0.04f, 0.01f), false);
        SetRect(loginBtnGO, -112f, y, 190f, 52f);

        var regBtnGO = MakeButton(panel, "RegisterButton", "REGISTER",
                                  font, new Color(0.10f, 0.04f, 0.05f, 1f), ColGold, true);
        SetRect(regBtnGO, 112f, y, 190f, 52f);
        y -= 74f;

        // Status
        var statusTMP = AddTMP(panel, "StatusText", "Sign in to start playing.",
                               font, 12f, ColDim, FontStyles.Normal, TextAlignmentOptions.Center);
        SetRect(statusTMP.rectTransform, 0f, y, 440f, 48f);
        statusTMP.enableWordWrapping = true;

        // Wire LoginController
        var lc            = canvasGO.AddComponent<LoginController>();
        lc.usernameInput  = uInput.GetComponent<TMP_InputField>();
        lc.passwordInput  = pInput.GetComponent<TMP_InputField>();
        lc.loginButton    = loginBtnGO.GetComponent<Button>();
        lc.registerButton = regBtnGO.GetComponent<Button>();
        lc.statusText     = statusTMP;

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, SavePath);
        AssetDatabase.Refresh();
        Debug.Log($"[ZeroDay] LoginScene saved to {SavePath}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static RectTransform MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    static TextMeshProUGUI AddTMP(RectTransform parent, string name, string text,
                                  TMP_FontAsset font, float size, Color color,
                                  FontStyles style, TextAlignmentOptions alignment)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.font = font; tmp.fontSize = size;
        tmp.color = color; tmp.fontStyle = style; tmp.alignment = alignment;
        return tmp;
    }

    static RectTransform MakeInputField(RectTransform parent, string name,
                                        string placeholder, TMP_FontAsset font, bool isPassword)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.AddComponent<Image>().color = ColInputBg;

        // Gold bottom bar
        var bar = new GameObject("BottomBar"); bar.transform.SetParent(root.transform, false);
        bar.AddComponent<Image>().color = ColGold;
        var barRT = bar.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0f, 0f); barRT.anchorMax = new Vector2(1f, 0f);
        barRT.pivot = new Vector2(0.5f, 0f);
        barRT.anchoredPosition = Vector2.zero; barRT.sizeDelta = new Vector2(0f, 2f);

        var ta = new GameObject("Text Area"); ta.transform.SetParent(root.transform, false);
        ta.AddComponent<RectMask2D>();
        var taRT = ta.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(14f, 5f); taRT.offsetMax = new Vector2(-8f, -5f);

        var ph = new GameObject("Placeholder"); ph.transform.SetParent(ta.transform, false);
        var phTMP = ph.AddComponent<TextMeshProUGUI>();
        phTMP.text = placeholder; phTMP.font = font; phTMP.fontSize = 16f;
        phTMP.color = new Color(0.45f, 0.35f, 0.25f, 1f);
        phTMP.fontStyle = FontStyles.Italic;
        FullStretch(ph.GetComponent<RectTransform>());

        var txt = new GameObject("Text"); txt.transform.SetParent(ta.transform, false);
        var txtTMP = txt.AddComponent<TextMeshProUGUI>();
        txtTMP.text = ""; txtTMP.font = font; txtTMP.fontSize = 16f;
        txtTMP.color = ColCream;
        FullStretch(txt.GetComponent<RectTransform>());

        var field = root.AddComponent<TMP_InputField>();
        field.textViewport = taRT; field.textComponent = txtTMP; field.placeholder = phTMP;
        field.caretColor = ColGold;
        field.selectionColor = new Color(ColGold.r, ColGold.g, ColGold.b, 0.3f);
        if (isPassword) field.contentType = TMP_InputField.ContentType.Password;

        var outl = root.AddComponent<Outline>();
        outl.effectColor = new Color(ColGold.r, ColGold.g, ColGold.b, 0.25f);
        outl.effectDistance = new Vector2(1f, -1f);

        return root.GetComponent<RectTransform>();
    }

    static RectTransform MakeButton(RectTransform parent, string name, string label,
                                    TMP_FontAsset font, Color bgColor, Color textColor, bool addBorder)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgColor;
        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor      = bgColor;
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.18f);
        cb.pressedColor     = Color.Lerp(bgColor, Color.black, 0.28f);
        btn.colors = cb;

        var lgo = new GameObject("Label"); lgo.transform.SetParent(go.transform, false);
        var lTMP = lgo.AddComponent<TextMeshProUGUI>();
        lTMP.text = label; lTMP.font = font; lTMP.fontSize = 16f;
        lTMP.color = textColor; lTMP.fontStyle = FontStyles.Bold;
        lTMP.alignment = TextAlignmentOptions.Center;
        FullStretch(lgo.GetComponent<RectTransform>());

        if (addBorder)
        {
            var outl = go.AddComponent<Outline>();
            outl.effectColor = new Color(ColGold.r, ColGold.g, ColGold.b, 0.65f);
            outl.effectDistance = new Vector2(1f, -1f);
        }
        return go.GetComponent<RectTransform>();
    }

    static void SetRect(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
    }

    static void FullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
