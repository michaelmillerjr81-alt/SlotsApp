using UnityEngine;

/**
 * PlatformManager
 * Singleton that persists across all scenes. Stores the logged-in player's
 * session data (JWT token, username, balances).
 *
 * Setup: Attach to a GameObject in LoginScene. It will survive scene loads.
 */
public class PlatformManager : MonoBehaviour
{
    private static PlatformManager _instance;
    public static PlatformManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("PlatformServices");
                _instance = go.AddComponent<PlatformManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Set these in the Inspector or via code before making API calls
    [Tooltip("Base URL of the backend server (no trailing slash)")]
    public string serverBaseUrl = "http://localhost:3000";
    [Tooltip("Must match the API_KEY environment variable on the server")]
    public string apiKey = "CHANGE_ME";

    // Session data (populated after login/register)
    public string JwtToken   { get; set; }
    public string Username   { get; set; }
    public int    GCBalance  { get; set; }
    public int    SCBalance  { get; set; }

    public bool IsLoggedIn => !string.IsNullOrEmpty(JwtToken);

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSession(string token, string username, int gcBalance, int scBalance)
    {
        JwtToken  = token;
        Username  = username;
        GCBalance = gcBalance;
        SCBalance = scBalance;
    }

    public void Logout()
    {
        JwtToken  = null;
        Username  = null;
        GCBalance = 0;
        SCBalance = 0;
    }
}
