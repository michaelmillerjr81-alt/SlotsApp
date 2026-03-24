using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/**
 * ZeroDaySlotController
 * Industry-standard UI and Game State manager.
 * UPGRADE: Contains the Sequential Reel Engine, Scatter Anticipation, and Balance UI sync.
 */
public class ZeroDaySlotController : MonoBehaviour
{
    [Header("Network Connection")]
    public SweepstakesNetworkManager networkManager;

    [Header("UI References")]
    public Button spinButton;
    public Button toggleCurrencyButton;
    public Button increaseBetButton;
    public Button decreaseBetButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI currencyModeText;
    public TextMeshProUGUI balanceText;
    public GameObject bonusGamePanel;

    [Header("Grid Connection")]
    public ZeroDayGridManager gridManager;

    [Header("VFX & Polish")]
    public Transform screenShakeContainer;
    public Image backgroundImage;
    public ParticleSystem winParticles;

    [Header("Shake & Glitch Settings")]
    public float shakeDuration = 0.4f;
    public float shakePositionalMagnitude = 15f;
    public float shakeRotationalMagnitude = 2f;
    public float buttonPulseSpeed = 0.1f;

    [Header("Sweepstakes Settings")]
    public string currentCurrency = "GC";
    public int currentBetAmount = 100;

    private int currentGCBalance = 0;
    private int currentSCBalance = 0;

    private int betStepGC = 100;
    private int maxBetGC = 10000;
    private int minBetGC = 100;

    private int betStepSC = 5;
    private int maxBetSC = 500;
    private int minBetSC = 5;

    public event Action onSpinComplete;

    private void Start()
    {
        if (spinButton != null) spinButton.onClick.AddListener(OnLocalSpinClicked);
        if (toggleCurrencyButton != null) toggleCurrencyButton.onClick.AddListener(OnToggleCurrencyClicked);
        if (increaseBetButton != null) increaseBetButton.onClick.AddListener(OnIncreaseBetClicked);
        if (decreaseBetButton != null) decreaseBetButton.onClick.AddListener(OnDecreaseBetClicked);

        if (bonusGamePanel != null) bonusGamePanel.SetActive(false);

        UpdateCurrencyUI();
        UpdateStatusText("SYSTEM BOOTING. CONNECTING TO SERVER...");

        if (networkManager != null)
        {
            networkManager.RequestInit();
        }
    }

    private void OnIncreaseBetClicked()
    {
        if (increaseBetButton != null) StartCoroutine(PulseButtonRoutine(increaseBetButton.transform));

        if (currentCurrency == "GC")
        {
            currentBetAmount += betStepGC;
            if (currentBetAmount > maxBetGC) currentBetAmount = maxBetGC;
        }
        else
        {
            currentBetAmount += betStepSC;
            if (currentBetAmount > maxBetSC) currentBetAmount = maxBetSC;
        }
        UpdateCurrencyUI();
    }

    private void OnDecreaseBetClicked()
    {
        if (decreaseBetButton != null) StartCoroutine(PulseButtonRoutine(decreaseBetButton.transform));

        if (currentCurrency == "GC")
        {
            currentBetAmount -= betStepGC;
            if (currentBetAmount < minBetGC) currentBetAmount = minBetGC;
        }
        else
        {
            currentBetAmount -= betStepSC;
            if (currentBetAmount < minBetSC) currentBetAmount = minBetSC;
        }
        UpdateCurrencyUI();
    }

    private void OnToggleCurrencyClicked()
    {
        if (toggleCurrencyButton != null) StartCoroutine(PulseButtonRoutine(toggleCurrencyButton.transform));

        currentCurrency = (currentCurrency == "GC") ? "SC" : "GC";
        currentBetAmount = (currentCurrency == "GC") ? minBetGC : minBetSC;

        UpdateCurrencyUI();
        UpdateStatusText($"CURRENCY SWITCHED. NOW WAGERING {currentCurrency}.");
    }

    private void UpdateCurrencyUI()
    {
        if (currencyModeText != null)
        {
            currencyModeText.text = $"MODE: {currentCurrency}\nBET: {currentBetAmount}";
            currencyModeText.color = (currentCurrency == "GC") ? Color.yellow : Color.cyan;
        }

        if (balanceText != null)
        {
            int displayBalance = (currentCurrency == "GC") ? currentGCBalance : currentSCBalance;
            balanceText.text = $"BALANCE:\n{displayBalance} {currentCurrency}";
        }
    }

    private void OnLocalSpinClicked()
    {
        int currentBalance = (currentCurrency == "GC") ? currentGCBalance : currentSCBalance;
        if (currentBetAmount > currentBalance)
        {
            UpdateStatusText("ERROR: INSUFFICIENT FUNDS. PLEASE LOWER WAGER.");
            return;
        }

        if (spinButton != null) StartCoroutine(PulseButtonRoutine(spinButton.transform));

        SetButtonsInteractable(false);

        if (networkManager != null)
        {
            BeginInfiniteSpin();
            networkManager.RequestSpin(currentCurrency, currentBetAmount);
        }
        else
        {
            Debug.LogError("[NETWORK ERROR] SweepstakesNetworkManager is missing!");
            UpdateStatusText("ERROR: NETWORK DISCONNECTED.");
            SetButtonsInteractable(true);
        }
    }

    private void SetButtonsInteractable(bool state)
    {
        if (spinButton != null) spinButton.interactable = state;
        if (toggleCurrencyButton != null) toggleCurrencyButton.interactable = state;
        if (increaseBetButton != null) increaseBetButton.interactable = state;
        if (decreaseBetButton != null) decreaseBetButton.interactable = state;
    }

    public void BeginInfiniteSpin()
    {
        UpdateStatusText($"TRANSMITTING {currentBetAmount} {currentCurrency}... REELS SPINNING.");
        if (gridManager != null) gridManager.BeginDigitalSpin();
    }

    public void RejectSpin(string errorMessage)
    {
        UpdateStatusText($"TRANSMISSION FAILED: {errorMessage}");
        SetButtonsInteractable(true);
        if (gridManager != null) gridManager.StopAllCoroutines();
    }

    public void ResolveInit(string gridData, int initialGC, int initialSC)
    {
        currentGCBalance = initialGC;
        currentSCBalance = initialSC;
        UpdateCurrencyUI();

        if (gridManager != null) gridManager.UpdateGridVisuals(gridData);
        UpdateStatusText("SYSTEM READY. AWAITING WAGER.");
    }

    public void ResolveSpin(string gridData, int winAmount, int newBalance, int[] winningLines)
    {
        if (currentCurrency == "GC") currentGCBalance = newBalance;
        else currentSCBalance = newBalance;

        UpdateCurrencyUI();

        StartCoroutine(AnimateReelSequence(gridData, winAmount));
    }

    // PATCH: Sequential Reel Engine with Scatter Suspense Delay
    private IEnumerator AnimateReelSequence(string gridData, int winAmount)
    {
        string[] finalSymbols = gridData.Split(',');
        int scatterCountSoFar = 0;
        int totalScattersNeeded = 5;

        // Sequence through the 5 columns (Reel 0 through 4)
        for (int reel = 0; reel < 5; reel++)
        {
            // SUSPENSE CHECK: If we already have 3+ Scatters, and we are spinning reels 4 or 5, slow down!
            bool isSuspenseReel = (scatterCountSoFar >= 3 && reel >= 3);

            if (isSuspenseReel)
            {
                UpdateStatusText("CRITICAL: SCATTER ANOMALY DETECTED... BRACE FOR EXPLOIT!");
                if (backgroundImage != null) StartCoroutine(GlitchBackgroundRoutine());

                // Massive suspense delay (2.5 seconds) for the final reels
                yield return new WaitForSeconds(2.5f);
            }
            else
            {
                // Normal "Thud" delay (0.3 seconds)
                yield return new WaitForSeconds(0.3f);
            }

            // Stop this specific vertical column
            if (gridManager != null) gridManager.StopReel(reel, finalSymbols);

            // Scan the newly stopped column to see if a Scatter landed
            for (int row = 0; row < 5; row++)
            {
                int cellIndex = (row * 5) + reel;
                if (cellIndex < finalSymbols.Length && finalSymbols[cellIndex].Trim() == "Seven")
                {
                    scatterCountSoFar++;
                }
            }
        }

        // Slight pause for effect after the final reel lands
        yield return new WaitForSeconds(0.5f);

        // VFX TRIGGER: Did we hit a Payline?
        if (winAmount > 0)
        {
            UpdateStatusText($"WINNER! PAYOUT: {winAmount} {currentCurrency}");

            if (winParticles != null) winParticles.Play();
            if (screenShakeContainer != null) StartCoroutine(ShakeUIRoutine());
            if (backgroundImage != null) StartCoroutine(GlitchBackgroundRoutine());
        }
        else
        {
            UpdateStatusText($"NO WIN.");
        }

        // BONUS TRIGGER: Did we hit 5 Scatters?
        if (scatterCountSoFar >= totalScattersNeeded)
        {
            TriggerBonusMiniGame();
        }
        else
        {
            SetButtonsInteractable(true);
            onSpinComplete?.Invoke();
        }
    }

    // --- BONUS GAME LOGIC ---

    private void TriggerBonusMiniGame()
    {
        UpdateStatusText("CRITICAL: 5 SCATTERS DETECTED. INITIATING ZERO-DAY EXPLOIT.");
        Debug.Log("[BONUS ENGINE] 5+ Scatters hit! Transitioning to Mini-Game...");

        if (backgroundImage != null) StartCoroutine(GlitchBackgroundRoutine());
        if (bonusGamePanel != null) bonusGamePanel.SetActive(true);
    }

    public void EndBonusMiniGame()
    {
        Debug.Log("[BONUS ENGINE] Exploit Complete. Returning to main terminal.");
        UpdateStatusText("SYSTEM NORMALIZED. AWAITING WAGER.");

        if (bonusGamePanel != null) bonusGamePanel.SetActive(false);

        SetButtonsInteractable(true);
        onSpinComplete?.Invoke();
    }

    // --- VFX COROUTINES ---

    private IEnumerator PulseButtonRoutine(Transform btnTransform)
    {
        Vector3 originalScale = btnTransform.localScale;
        Vector3 punchedScale = originalScale * 0.85f;

        float elapsed = 0f;
        while (elapsed < buttonPulseSpeed)
        {
            btnTransform.localScale = Vector3.Lerp(originalScale, punchedScale, elapsed / buttonPulseSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < buttonPulseSpeed)
        {
            btnTransform.localScale = Vector3.Lerp(punchedScale, originalScale, elapsed / buttonPulseSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        btnTransform.localScale = originalScale;
    }

    private IEnumerator GlitchBackgroundRoutine()
    {
        Color originalColor = backgroundImage.color;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            int randomChoice = UnityEngine.Random.Range(0, 4);
            Color glitchColor = originalColor;

            switch (randomChoice)
            {
                case 0: glitchColor = Color.cyan; break;
                case 1: glitchColor = Color.magenta; break;
                case 2: glitchColor = Color.white; break;
                case 3: glitchColor = new Color(0, 0, 0, 0.5f); break;
            }

            backgroundImage.color = glitchColor;
            yield return new WaitForSeconds(0.05f);

            backgroundImage.color = originalColor;
            yield return new WaitForSeconds(0.05f);

            elapsed += 0.1f;
        }

        backgroundImage.color = originalColor;
    }

    private IEnumerator ShakeUIRoutine()
    {
        Vector3 originalPos = screenShakeContainer.localPosition;
        Quaternion originalRot = screenShakeContainer.localRotation;

        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = originalPos.x + UnityEngine.Random.Range(-1f, 1f) * shakePositionalMagnitude;
            float y = originalPos.y + UnityEngine.Random.Range(-1f, 1f) * shakePositionalMagnitude;
            float zTilt = UnityEngine.Random.Range(-1f, 1f) * shakeRotationalMagnitude;

            screenShakeContainer.localPosition = new Vector3(x, y, originalPos.z);
            screenShakeContainer.localRotation = originalRot * Quaternion.Euler(0, 0, zTilt);

            elapsed += Time.deltaTime;
            yield return null;
        }

        screenShakeContainer.localPosition = originalPos;
        screenShakeContainer.localRotation = originalRot;
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log($"[SLOT CONTROLLER] {message}");
    }

    private void OnDestroy()
    {
        if (spinButton != null) spinButton.onClick.RemoveListener(OnLocalSpinClicked);
        if (toggleCurrencyButton != null) toggleCurrencyButton.onClick.RemoveListener(OnToggleCurrencyClicked);
        if (increaseBetButton != null) increaseBetButton.onClick.RemoveListener(OnIncreaseBetClicked);
        if (decreaseBetButton != null) decreaseBetButton.onClick.RemoveListener(OnDecreaseBetClicked);
    }
}