using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/**
 * GameCardController
 * Attached to each game card prefab in the lobby.
 * Populated by LobbyController at runtime.
 */
public class GameCardController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI gameNameText;
    public TextMeshProUGUI descriptionText;
    public Button playButton;

    private string targetScene;

    public void Setup(string gameName, string description, string sceneName)
    {
        targetScene = sceneName;

        if (gameNameText != null)    gameNameText.text    = gameName.ToUpper();
        if (descriptionText != null) descriptionText.text = description;
        if (playButton != null)      playButton.onClick.AddListener(OnPlayClicked);
    }

    private void OnPlayClicked()
    {
        if (!string.IsNullOrEmpty(targetScene))
            SceneManager.LoadScene(targetScene);
        else
            Debug.LogError("[GAME CARD] No scene name set.");
    }
}
