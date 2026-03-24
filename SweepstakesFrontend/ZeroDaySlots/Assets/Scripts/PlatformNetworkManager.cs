using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/**
 * PlatformNetworkManager
 * Handles auth and platform-level API calls (register, login, games list, player info).
 * Attach to the same GameObject as PlatformManager in LoginScene.
 */
public class PlatformNetworkManager : MonoBehaviour
{
    // ---- Response Data Classes ----

    [Serializable]
    public class AuthResponseData
    {
        public bool   success;
        public string error;
        public string token;
        public string username;
        public int    gcBalance;
        public int    scBalance;
    }

    [Serializable]
    public class GameData
    {
        public string id;
        public string name;
        public string sceneName;
        public string description;
    }

    [Serializable]
    public class GamesResponseData
    {
        public bool       success;
        public string     error;
        public GameData[] games;
    }

    [Serializable]
    public class PlayerResponseData
    {
        public bool   success;
        public string error;
        public string username;
        public int    gcBalance;
        public int    scBalance;
    }

    // ---- Request Data Classes ----

    [Serializable]
    private class AuthRequestData
    {
        public string username;
        public string password;
    }

    // ---- Public API ----

    public void Register(string username, string password, Action<AuthResponseData> onComplete)
    {
        var data = new AuthRequestData { username = username, password = password };
        StartCoroutine(PostRequest("/api/auth/register", JsonUtility.ToJson(data), false, text =>
        {
            onComplete(JsonUtility.FromJson<AuthResponseData>(text));
        }));
    }

    public void Login(string username, string password, Action<AuthResponseData> onComplete)
    {
        var data = new AuthRequestData { username = username, password = password };
        StartCoroutine(PostRequest("/api/auth/login", JsonUtility.ToJson(data), false, text =>
        {
            onComplete(JsonUtility.FromJson<AuthResponseData>(text));
        }));
    }

    public void GetGames(Action<GamesResponseData> onComplete)
    {
        StartCoroutine(GetRequest("/api/games", true, text =>
        {
            onComplete(JsonUtility.FromJson<GamesResponseData>(text));
        }));
    }

    public void GetPlayer(Action<PlayerResponseData> onComplete)
    {
        StartCoroutine(GetRequest("/api/player", true, text =>
        {
            onComplete(JsonUtility.FromJson<PlayerResponseData>(text));
        }));
    }

    // ---- Internal Helpers ----

    private IEnumerator GetRequest(string endpoint, bool requiresAuth, Action<string> onSuccess)
    {
        string url = PlatformManager.Instance.serverBaseUrl + endpoint;
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            AddHeaders(req, requiresAuth);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onSuccess(req.downloadHandler.text);
            else
                Debug.LogError($"[PLATFORM NET] GET {endpoint} failed: {req.error}");
        }
    }

    private IEnumerator PostRequest(string endpoint, string json, bool requiresAuth, Action<string> onSuccess)
    {
        string url = PlatformManager.Instance.serverBaseUrl + endpoint;
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler   = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            AddHeaders(req, requiresAuth);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onSuccess(req.downloadHandler.text);
            else
                Debug.LogError($"[PLATFORM NET] POST {endpoint} failed: {req.error}");
        }
    }

    private void AddHeaders(UnityWebRequest req, bool requiresAuth)
    {
        req.SetRequestHeader("x-api-key", PlatformManager.Instance.apiKey);
        if (requiresAuth && PlatformManager.Instance.IsLoggedIn)
            req.SetRequestHeader("Authorization", "Bearer " + PlatformManager.Instance.JwtToken);
    }
}
