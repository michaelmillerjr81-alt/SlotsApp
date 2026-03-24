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
    public static PlatformManager Instance { get; private set; }

    // Set these in the Inspector or via code before making API calls
    [Tooltip("Base URL of the backend server (no trailing slash)")]
    public string serverBaseUrl = "http://localhost:3000";
    [Tooltip("Must match the API_KEY environment variable on the server")]
    public string apiKey = "zeroday123";

    // Session data (populated after login/register)
    public string JwtToken   { get; set; }
    public string Username   { get; set; }
    public int    GCBalance  { get; set; }
    public int    SCBalance  { get; set; }

    public bool IsLoggedIn => !string.IsNullOrEmpty(JwtToken);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
