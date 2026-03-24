using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SweepstakesNetworkManager : MonoBehaviour
{
    public ZeroDaySlotController slotController;
    [Tooltip("Base URL of the backend server (no trailing slash)")]
    public string serverBaseUrl = "http://localhost:3000";
    [Tooltip("Must match the API_KEY environment variable set on the server")]
    public string apiKey = "CHANGE_ME";

    private string InitUrl => serverBaseUrl + "/api/init";
    private string SpinUrl => serverBaseUrl + "/api/spin";

    [Serializable]
    public class ProvablyFairData
    {
        public string hash;
        public string serverSeed;
        public string clientSeed;
        public int nonce;
    }

    [Serializable]
    public class SpinRequestData
    {
        public string currencyType;
        public int betAmount;
        public string clientSeed;
    }

    [Serializable]
    public class SpinResponseData
    {
        public bool success;
        public string error;
        public int winAmount;
        public string currencyType;
        public int newBalance;
        public string gridData;
        public ProvablyFairData provablyFair;
    }

    [Serializable]
    public class BalanceData
    {
        public int GC;
        public int SC;
    }

    [Serializable]
    public class InitResponseData
    {
        public bool success;
        public string gridData;
        public BalanceData balances;
    }

    public void RequestInit()
    {
        StartCoroutine(GetInitRequest());
    }

    public void RequestSpin(string currency, int bet)
    {
        SpinRequestData reqData = new SpinRequestData
        {
            currencyType = currency,
            betAmount = bet,
            clientSeed = System.Guid.NewGuid().ToString("N")
        };

        string jsonPayload = JsonUtility.ToJson(reqData);
        StartCoroutine(PostSpinRequest(jsonPayload));
    }

    private IEnumerator GetInitRequest()
    {
        Debug.Log($"[NETWORK] Requesting initial board state from: {InitUrl}");
        using (UnityWebRequest request = UnityWebRequest.Get(InitUrl))
        {
            request.SetRequestHeader("x-api-key", apiKey);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[NETWORK] Boot payload received: {request.downloadHandler.text}");
                InitResponseData responseData = JsonUtility.FromJson<InitResponseData>(request.downloadHandler.text);
                if (responseData.success && slotController != null)
                {
                    slotController.ResolveInit(responseData.gridData, responseData.balances.GC, responseData.balances.SC);
                }
            }
            else
            {
                Debug.LogError($"[NETWORK ERROR] {request.error}");
            }
        }
    }

    private IEnumerator PostSpinRequest(string jsonPayload)
    {
        using (UnityWebRequest request = new UnityWebRequest(SpinUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-api-key", apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                SpinResponseData responseData = JsonUtility.FromJson<SpinResponseData>(request.downloadHandler.text);
                if (responseData.success)
                {
                    Debug.Log($"[PAYLOAD] Won: {responseData.winAmount} {responseData.currencyType} | New Balance: {responseData.newBalance}");

                    if (slotController != null)
                    {
                        slotController.ResolveSpin(responseData.gridData, responseData.winAmount, responseData.newBalance, new int[0]);
                    }
                }
                else
                {
                    // UPGRADE: Catch server rejections and unlock the UI
                    Debug.LogWarning($"[SERVER REJECTION] {responseData.error}");
                    if (slotController != null)
                    {
                        slotController.RejectSpin(responseData.error);
                    }
                }
            }
            else
            {
                // UPGRADE: Catch physical connection drops
                Debug.LogError($"[NETWORK ERROR] {request.error}");
                if (slotController != null)
                {
                    slotController.RejectSpin("NETWORK_DISCONNECTED");
                }
            }
        }
    }
}