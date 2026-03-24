using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/**
 * LoginController
 * Attach to a GameObject in LoginScene.
 *
 * Required scene setup:
 *   - A GameObject with PlatformManager + PlatformNetworkManager attached (auto-persists)
 *   - TMP_InputField: usernameInput
 *   - TMP_InputField: passwordInput  (Content Type = Password)
 *   - Button: loginButton
 *   - Button: registerButton
 *   - TextMeshProUGUI: statusText
 *   - Set LobbySceneName in Inspector to the name of your lobby scene (e.g. "LobbyScene")
 */
public class LoginController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button registerButton;
    public TextMeshProUGUI statusText;

    [Header("Scene Settings")]
    public string lobbySceneName = "LobbyScene";

    private PlatformNetworkManager netManager;

    private void Start()
    {
        netManager = FindObjectOfType<PlatformNetworkManager>();

        if (loginButton != null)    loginButton.onClick.AddListener(OnLoginClicked);
        if (registerButton != null) registerButton.onClick.AddListener(OnRegisterClicked);

        SetStatus("ENTER CREDENTIALS TO ACCESS THE NETWORK.", Color.cyan);
    }

    private void OnLoginClicked()
    {
        string username = usernameInput != null ? usernameInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text : "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            SetStatus("ERROR: USERNAME AND PASSWORD ARE REQUIRED.", Color.red);
            return;
        }

        SetInteractable(false);
        SetStatus("AUTHENTICATING...", Color.yellow);

        netManager.Login(username, password, response =>
        {
            if (response.success)
            {
                PlatformManager.Instance.SetSession(response.token, response.username, response.gcBalance, response.scBalance);
                SetStatus($"ACCESS GRANTED. WELCOME, {response.username.ToUpper()}.", Color.green);
                SceneManager.LoadScene(lobbySceneName);
            }
            else
            {
                SetStatus($"ACCESS DENIED: {response.error}", Color.red);
                SetInteractable(true);
            }
        });
    }

    private void OnRegisterClicked()
    {
        string username = usernameInput != null ? usernameInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text : "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            SetStatus("ERROR: USERNAME AND PASSWORD ARE REQUIRED.", Color.red);
            return;
        }

        if (password.Length < 8)
        {
            SetStatus("ERROR: PASSWORD MUST BE AT LEAST 8 CHARACTERS.", Color.red);
            return;
        }

        SetInteractable(false);
        SetStatus("CREATING ACCOUNT...", Color.yellow);

        netManager.Register(username, password, response =>
        {
            if (response.success)
            {
                PlatformManager.Instance.SetSession(response.token, response.username, response.gcBalance, response.scBalance);
                SetStatus($"ACCOUNT CREATED. WELCOME, {response.username.ToUpper()}.", Color.green);
                SceneManager.LoadScene(lobbySceneName);
            }
            else
            {
                string msg = response.error switch
                {
                    "USERNAME_TAKEN"       => "ERROR: THAT USERNAME IS ALREADY TAKEN.",
                    "INVALID_USERNAME"     => "ERROR: USERNAME MUST BE 3-20 ALPHANUMERIC CHARACTERS.",
                    "WEAK_PASSWORD"        => "ERROR: PASSWORD MUST BE AT LEAST 8 CHARACTERS.",
                    _                      => $"ERROR: {response.error}"
                };
                SetStatus(msg, Color.red);
                SetInteractable(true);
            }
        });
    }

    private void SetInteractable(bool state)
    {
        if (loginButton != null)    loginButton.interactable    = state;
        if (registerButton != null) registerButton.interactable = state;
        if (usernameInput != null)  usernameInput.interactable  = state;
        if (passwordInput != null)  passwordInput.interactable  = state;
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text  = message;
            statusText.color = color;
        }
        Debug.Log($"[LOGIN] {message}");
    }
}
