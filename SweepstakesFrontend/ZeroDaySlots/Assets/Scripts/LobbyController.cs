using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class LobbyController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI welcomeText;
    public TextMeshProUGUI gcBalanceText;
    public TextMeshProUGUI scBalanceText;
    public TextMeshProUGUI statusText;
    public Button logoutButton;

    [Header("Game Grid")]
    public Transform gameCardContainer;

    [Header("Scene Settings")]
    public string loginSceneName = "LoginScene";

    // Colours matching the cyberpunk palette
    static readonly Color ColCard    = new Color(0.07f, 0.07f, 0.14f, 1f);
    static readonly Color ColAccent  = new Color(0.00f, 1.00f, 0.90f, 1f);
    static readonly Color ColDim     = new Color(0.45f, 0.45f, 0.55f, 1f);
    static readonly Color ColBtn     = new Color(0.00f, 0.75f, 0.70f, 1f);

    private PlatformNetworkManager netManager;

    private void Start()
    {
        if (PlatformManager.Instance == null || !PlatformManager.Instance.IsLoggedIn)
        {
            SceneManager.LoadScene(loginSceneName);
            return;
        }

        netManager = PlatformManager.Instance.GetComponent<PlatformNetworkManager>();

        if (netManager == null)
        {
            Debug.LogError("[LOBBY] PlatformNetworkManager not found on PlatformManager GameObject!");
            SetStatus("ERROR: NETWORK MANAGER MISSING.", Color.red);
            return;
        }

        if (logoutButton != null) logoutButton.onClick.AddListener(OnLogoutClicked);

        RefreshUI();

        // Always resolve container at runtime — don't rely on serialized ref
        if (gameCardContainer == null)
        {
            var found = GameObject.Find("GameCardContainer");
            if (found != null)
                gameCardContainer = found.transform;
        }

        // Last resort: attach directly to this Canvas
        if (gameCardContainer == null)
        {
            var canvas = FindAnyObjectByType<Canvas>();
            gameCardContainer = canvas != null ? canvas.transform : transform;
            Debug.LogWarning("[LOBBY] GameCardContainer not found — falling back to Canvas root.");
        }

        Debug.Log($"[LOBBY] Using container: {gameCardContainer.name}");

        // Disable any Mask that may be clipping the scroll content
        var viewport = GameObject.Find("Viewport");
        if (viewport != null)
        {
            var mask = viewport.GetComponent<UnityEngine.UI.Mask>();
            if (mask != null) mask.enabled = false;
        }

        LoadGames();
    }

    private void RefreshUI()
    {
        if (welcomeText != null)
            welcomeText.text = $"WELCOME BACK, {PlatformManager.Instance.Username.ToUpper()}";

        if (gcBalanceText != null)
            gcBalanceText.text = $"GC: {PlatformManager.Instance.GCBalance:N0}";
        if (scBalanceText != null)
            scBalanceText.text = $"SC: {PlatformManager.Instance.SCBalance:N0}";
    }

    private void LoadGames()
    {
        SetStatus("LOADING GAME CATALOG...", Color.cyan);

        netManager.GetGames(response =>
        {
            if (!response.success)
            {
                SetStatus($"ERROR LOADING GAMES: {response.error}", Color.red);
                return;
            }

            // Clear old cards
            foreach (Transform child in gameCardContainer)
                Destroy(child.gameObject);

            // Build each card in code — no prefab dependency
            foreach (var game in response.games)
                SpawnCard(game.name, game.description, game.sceneName);

            StartCoroutine(RebuildLayout());
            SetStatus($"{response.games.Length} GAME(S) AVAILABLE. SELECT YOUR TABLE.", Color.green);
        });
    }

    private void SpawnCard(string gameName, string description, string sceneName)
    {
        // Root
        var card = new GameObject("GameCard_" + gameName);
        card.transform.SetParent(gameCardContainer, false);
        var img = card.AddComponent<Image>();
        img.color = ColCard;
        // Explicit size — GridLayoutGroup will override this if present,
        // but this ensures the card is visible even without a layout group
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(340, 220);

        // Accent stripe at top
        var stripe = new GameObject("Stripe");
        stripe.transform.SetParent(card.transform, false);
        var stripeImg = stripe.AddComponent<Image>();
        stripeImg.color = ColAccent;
        var stripeRT = stripe.GetComponent<RectTransform>();
        stripeRT.anchorMin = new Vector2(0, 1); stripeRT.anchorMax = new Vector2(1, 1);
        stripeRT.pivot = new Vector2(0.5f, 1f);
        stripeRT.anchoredPosition = Vector2.zero;
        stripeRT.sizeDelta = new Vector2(0, 4);

        // Game name
        var nameGO = new GameObject("GameName");
        nameGO.transform.SetParent(card.transform, false);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = gameName.ToUpper();
        nameTMP.fontSize = 18; nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = Color.white;
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 1); nameRT.anchorMax = new Vector2(1, 1);
        nameRT.pivot = new Vector2(0.5f, 1f);
        nameRT.anchoredPosition = new Vector2(0, -18);
        nameRT.sizeDelta = new Vector2(-24, 28);

        // Description
        var descGO = new GameObject("Desc");
        descGO.transform.SetParent(card.transform, false);
        var descTMP = descGO.AddComponent<TextMeshProUGUI>();
        descTMP.text = description;
        descTMP.fontSize = 11;
        descTMP.color = ColDim;
        descTMP.enableWordWrapping = true;
        var descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = Vector2.zero; descRT.anchorMax = Vector2.one;
        descRT.offsetMin = new Vector2(16, 56); descRT.offsetMax = new Vector2(-16, -52);

        // Play button
        var btnGO = new GameObject("PlayButton");
        btnGO.transform.SetParent(card.transform, false);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = ColBtn;
        var btn = btnGO.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = ColBtn;
        cb.highlightedColor = Color.Lerp(ColBtn, Color.white, 0.2f);
        cb.pressedColor = Color.Lerp(ColBtn, Color.black, 0.25f);
        btn.colors = cb;
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0, 0); btnRT.anchorMax = new Vector2(1, 0);
        btnRT.pivot = new Vector2(0.5f, 0f);
        btnRT.anchoredPosition = new Vector2(0, 12);
        btnRT.sizeDelta = new Vector2(-24, 40);

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(btnGO.transform, false);
        var lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text = "▶  PLAY";
        lblTMP.fontSize = 14; lblTMP.fontStyle = FontStyles.Bold;
        lblTMP.color = Color.black;
        lblTMP.alignment = TextAlignmentOptions.Center;
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;

        string targetScene = sceneName;
        btn.onClick.AddListener(() => SceneManager.LoadScene(targetScene));

        Debug.Log($"[LOBBY] Built card for '{gameName}' → scene '{sceneName}'");
    }

    private IEnumerator RebuildLayout()
    {
        yield return null;
        if (gameCardContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(gameCardContainer.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
        }
    }

    private void OnLogoutClicked()
    {
        PlatformManager.Instance.Logout();
        SceneManager.LoadScene(loginSceneName);
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText != null) { statusText.text = message; statusText.color = color; }
        Debug.Log($"[LOBBY] {message}");
    }
}
