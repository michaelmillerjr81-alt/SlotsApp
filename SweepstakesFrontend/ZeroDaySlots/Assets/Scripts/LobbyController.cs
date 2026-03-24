using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/**
 * LobbyController
 * Attach to a GameObject in LobbyScene.
 *
 * Required scene setup:
 *   - TextMeshProUGUI: welcomeText       (e.g. "WELCOME BACK, ALICE")
 *   - TextMeshProUGUI: gcBalanceText
 *   - TextMeshProUGUI: scBalanceText
 *   - TextMeshProUGUI: statusText
 *   - Button: logoutButton
 *   - Transform: gameCardContainer       (parent object for game cards, e.g. a Grid Layout Group)
 *   - GameObject: gameCardPrefab         (prefab with GameCardController attached)
 *   - Set loginSceneName in Inspector    (e.g. "LoginScene")
 */
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
    public GameObject gameCardPrefab;

    [Header("Scene Settings")]
    public string loginSceneName = "LoginScene";

    private PlatformNetworkManager netManager;

    private void Start()
    {
        // Redirect to login if session expired
        if (PlatformManager.Instance == null || !PlatformManager.Instance.IsLoggedIn)
        {
            SceneManager.LoadScene(loginSceneName);
            return;
        }

        netManager = FindObjectOfType<PlatformNetworkManager>();

        if (logoutButton != null) logoutButton.onClick.AddListener(OnLogoutClicked);

        RefreshUI();
        LoadGames();
    }

    private void RefreshUI()
    {
        if (welcomeText != null)
            welcomeText.text = $"WELCOME BACK, {PlatformManager.Instance.Username.ToUpper()}";

        UpdateBalanceDisplay();
    }

    private void UpdateBalanceDisplay()
    {
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

            // Clear existing cards
            foreach (Transform child in gameCardContainer)
                Destroy(child.gameObject);

            // Spawn a card per game
            foreach (var game in response.games)
            {
                GameObject cardObj = Instantiate(gameCardPrefab, gameCardContainer);
                GameCardController card = cardObj.GetComponent<GameCardController>();
                if (card != null)
                    card.Setup(game.name, game.description, game.sceneName);
            }

            SetStatus($"{response.games.Length} GAME(S) AVAILABLE. SELECT YOUR TABLE.", Color.green);
        });
    }

    private void OnLogoutClicked()
    {
        PlatformManager.Instance.Logout();
        SceneManager.LoadScene(loginSceneName);
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text  = message;
            statusText.color = color;
        }
        Debug.Log($"[LOBBY] {message}");
    }
}
