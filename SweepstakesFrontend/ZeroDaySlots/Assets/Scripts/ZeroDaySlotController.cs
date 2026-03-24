using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/**
 * ZeroDaySlotController
 * UI and game-state manager with tiered win effects, deceleration, dampened shake,
 * screen flash, and balance roll-up animation.
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
    [Tooltip("Optional: a full-screen white Image (alpha 0) used for win flash. Set raycast target OFF.")]
    public Image winFlashImage;

    [Header("Shake Settings")]
    public float shakeDuration = 0.45f;
    public float shakePositionalMagnitude = 14f;
    public float shakeRotationalMagnitude = 2f;

    [Header("Button Settings")]
    public float buttonPulseSpeed = 0.1f;

    [Header("Win Tier Thresholds (multiplier of bet)")]
    public float bigWinMultiplier = 3f;
    public float jackpotMultiplier = 10f;

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

    private Coroutine balanceRollCoroutine;
    private Coroutine statusFlashCoroutine;

    public event Action onSpinComplete;

    private void Start()
    {
        if (PlatformManager.Instance == null || !PlatformManager.Instance.IsLoggedIn)
        {
            SceneManager.LoadScene("LoginScene");
            return;
        }

        if (spinButton != null) spinButton.onClick.AddListener(OnLocalSpinClicked);
        if (toggleCurrencyButton != null) toggleCurrencyButton.onClick.AddListener(OnToggleCurrencyClicked);
        if (increaseBetButton != null) increaseBetButton.onClick.AddListener(OnIncreaseBetClicked);
        if (decreaseBetButton != null) decreaseBetButton.onClick.AddListener(OnDecreaseBetClicked);

        if (bonusGamePanel != null) bonusGamePanel.SetActive(false);
        if (winFlashImage != null) winFlashImage.color = new Color(1, 1, 1, 0);

        UpdateCurrencyUI();
        UpdateStatusText("SYSTEM BOOTING. CONNECTING TO SERVER...", Color.cyan);

        if (networkManager != null)
            networkManager.RequestInit();
    }

    // ---- Bet Controls ----

    private void OnIncreaseBetClicked()
    {
        if (increaseBetButton != null) StartCoroutine(PulseButtonRoutine(increaseBetButton.transform));
        if (currentCurrency == "GC")
        {
            currentBetAmount = Mathf.Min(currentBetAmount + betStepGC, maxBetGC);
        }
        else
        {
            currentBetAmount = Mathf.Min(currentBetAmount + betStepSC, maxBetSC);
        }
        UpdateCurrencyUI();
    }

    private void OnDecreaseBetClicked()
    {
        if (decreaseBetButton != null) StartCoroutine(PulseButtonRoutine(decreaseBetButton.transform));
        if (currentCurrency == "GC")
        {
            currentBetAmount = Mathf.Max(currentBetAmount - betStepGC, minBetGC);
        }
        else
        {
            currentBetAmount = Mathf.Max(currentBetAmount - betStepSC, minBetSC);
        }
        UpdateCurrencyUI();
    }

    private void OnToggleCurrencyClicked()
    {
        if (toggleCurrencyButton != null) StartCoroutine(PulseButtonRoutine(toggleCurrencyButton.transform));
        currentCurrency = (currentCurrency == "GC") ? "SC" : "GC";
        currentBetAmount = (currentCurrency == "GC") ? minBetGC : minBetSC;
        UpdateCurrencyUI();
        UpdateStatusText($"CURRENCY SWITCHED. NOW WAGERING {currentCurrency}.", Color.cyan);
    }

    // ---- UI Updates ----

    private void UpdateCurrencyUI()
    {
        if (currencyModeText != null)
        {
            currencyModeText.text = $"MODE: {currentCurrency}\nBET: {currentBetAmount}";
            currencyModeText.color = (currentCurrency == "GC") ? Color.yellow : Color.cyan;
        }
        RefreshBalanceDisplay();
    }

    private void RefreshBalanceDisplay()
    {
        if (balanceText != null)
        {
            int displayBalance = (currentCurrency == "GC") ? currentGCBalance : currentSCBalance;
            balanceText.text = $"BALANCE:\n{displayBalance} {currentCurrency}";
        }
    }

    private void UpdateStatusText(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        Debug.Log($"[SLOT CONTROLLER] {message}");
    }

    private void UpdateStatusText(string message) => UpdateStatusText(message, Color.white);

    // ---- Spin Flow ----

    private void OnLocalSpinClicked()
    {
        int currentBalance = (currentCurrency == "GC") ? currentGCBalance : currentSCBalance;
        if (currentBetAmount > currentBalance)
        {
            UpdateStatusText("ERROR: INSUFFICIENT FUNDS. PLEASE LOWER WAGER.", Color.red);
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
            UpdateStatusText("ERROR: NETWORK DISCONNECTED.", Color.red);
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
        UpdateStatusText($"TRANSMITTING {currentBetAmount} {currentCurrency}... REELS SPINNING.", Color.white);
        if (gridManager != null) gridManager.BeginDigitalSpin();
    }

    public void RejectSpin(string errorMessage)
    {
        UpdateStatusText($"TRANSMISSION FAILED: {errorMessage}", Color.red);
        SetButtonsInteractable(true);
        if (gridManager != null) gridManager.StopAllCoroutines();
    }

    public void ResolveInit(string gridData, int initialGC, int initialSC)
    {
        currentGCBalance = initialGC;
        currentSCBalance = initialSC;
        UpdateCurrencyUI();

        if (gridManager != null) gridManager.UpdateGridVisuals(gridData);
        UpdateStatusText("SYSTEM READY. AWAITING WAGER.", Color.green);
    }

    public void ResolveSpin(string gridData, int winAmount, int newBalance, int[] winningLines)
    {
        int oldBalance = (currentCurrency == "GC") ? currentGCBalance : currentSCBalance;

        if (currentCurrency == "GC") currentGCBalance = newBalance;
        else currentSCBalance = newBalance;

        // Roll up balance during reel sequence
        if (balanceRollCoroutine != null) StopCoroutine(balanceRollCoroutine);
        balanceRollCoroutine = StartCoroutine(RollUpBalanceRoutine(oldBalance, newBalance, 1.5f));

        StartCoroutine(AnimateReelSequence(gridData, winAmount));
    }

    // ---- Reel Sequence ----

    private IEnumerator AnimateReelSequence(string gridData, int winAmount)
    {
        string[] finalSymbols = gridData.Split(',');
        int scatterCountSoFar = 0;
        int totalScattersNeeded = 5;

        for (int reel = 0; reel < 5; reel++)
        {
            bool isSuspenseReel = (scatterCountSoFar >= 3 && reel >= 3);

            if (isSuspenseReel)
            {
                UpdateStatusText("CRITICAL: SCATTER ANOMALY DETECTED... BRACE FOR EXPLOIT!", Color.magenta);
                if (backgroundImage != null) StartCoroutine(GlitchBackgroundRoutine(shakeDuration, 1.0f));
                if (gridManager != null) gridManager.DecelerateReel(reel);
                yield return new WaitForSeconds(2.5f);
            }
            else
            {
                // Decelerate as we approach the stop
                if (gridManager != null) gridManager.DecelerateReel(reel);
                yield return new WaitForSeconds(0.3f);
            }

            if (gridManager != null) gridManager.StopReel(reel, finalSymbols);

            // Count scatters in this column
            for (int row = 0; row < 5; row++)
            {
                int cellIndex = (row * 5) + reel;
                if (cellIndex < finalSymbols.Length && finalSymbols[cellIndex].Trim() == "Seven")
                    scatterCountSoFar++;
            }
        }

        yield return new WaitForSeconds(0.5f);

        // Resolve win effects
        if (winAmount > 0)
        {
            float multiplier = (float)winAmount / currentBetAmount;
            PlayWinEffects(winAmount, multiplier);
        }
        else
        {
            UpdateStatusText("NO WIN.", new Color(0.6f, 0.6f, 0.6f));
        }

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

    private void PlayWinEffects(int winAmount, float multiplier)
    {
        if (multiplier >= jackpotMultiplier)
        {
            // Jackpot
            if (statusFlashCoroutine != null) StopCoroutine(statusFlashCoroutine);
            statusFlashCoroutine = StartCoroutine(JackpotStatusRoutine(winAmount));

            if (winParticles != null) winParticles.Play();
            if (screenShakeContainer != null) StartCoroutine(ShakeUIRoutine(shakeDuration * 1.8f, shakePositionalMagnitude * 2f, shakeRotationalMagnitude * 2f));
            if (backgroundImage != null) StartCoroutine(GlitchBackgroundRoutine(shakeDuration * 1.5f, 1.0f));
            if (winFlashImage != null) StartCoroutine(FlashScreenRoutine(0.85f, 0.5f));
        }
        else if (multiplier >= bigWinMultiplier)
        {
            // Big win
            UpdateStatusText($"BIG WIN!  +{winAmount} {currentCurrency}", Color.yellow);

            if (winParticles != null) winParticles.Play();
            if (screenShakeContainer != null) StartCoroutine(ShakeUIRoutine(shakeDuration * 1.3f, shakePositionalMagnitude * 1.5f, shakeRotationalMagnitude * 1.5f));
            if (backgroundImage != null) StartCoroutine(GlitchBackgroundRoutine(shakeDuration * 1.1f, 0.7f));
            if (winFlashImage != null) StartCoroutine(FlashScreenRoutine(0.55f, 0.35f));
        }
        else
        {
            // Small win
            UpdateStatusText($"WIN!  +{winAmount} {currentCurrency}", Color.green);

            if (winParticles != null) winParticles.Play();
            if (screenShakeContainer != null) StartCoroutine(ShakeUIRoutine(shakeDuration, shakePositionalMagnitude, shakeRotationalMagnitude));
            if (backgroundImage != null) StartCoroutine(GlitchBackgroundRoutine(shakeDuration * 0.7f, 0.4f));
            if (winFlashImage != null) StartCoroutine(FlashScreenRoutine(0.3f, 0.25f));
        }
    }

    // ---- Bonus Game ----

    private void TriggerBonusMiniGame()
    {
        UpdateStatusText("CRITICAL: 5 SCATTERS DETECTED. INITIATING ZERO-DAY EXPLOIT.", Color.magenta);
        Debug.Log("[BONUS ENGINE] 5+ Scatters hit! Transitioning to Mini-Game...");

        if (backgroundImage != null) StartCoroutine(GlitchBackgroundRoutine(shakeDuration, 1.0f));
        if (bonusGamePanel != null) bonusGamePanel.SetActive(true);
    }

    public void EndBonusMiniGame()
    {
        Debug.Log("[BONUS ENGINE] Exploit Complete. Returning to main terminal.");
        UpdateStatusText("SYSTEM NORMALIZED. AWAITING WAGER.", Color.green);

        if (bonusGamePanel != null) bonusGamePanel.SetActive(false);

        SetButtonsInteractable(true);
        onSpinComplete?.Invoke();
    }

    // ---- VFX Coroutines ----

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

    // Dampened shake — magnitude decays over time for a natural feel
    private IEnumerator ShakeUIRoutine(float duration, float posMagnitude, float rotMagnitude)
    {
        Vector3 originalPos = screenShakeContainer.localPosition;
        Quaternion originalRot = screenShakeContainer.localRotation;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float decay = 1f - (elapsed / duration);  // 1 → 0 linear decay
            float currentMag = posMagnitude * decay;
            float currentRot = rotMagnitude * decay;

            float x = originalPos.x + UnityEngine.Random.Range(-1f, 1f) * currentMag;
            float y = originalPos.y + UnityEngine.Random.Range(-1f, 1f) * currentMag;
            float zTilt = UnityEngine.Random.Range(-1f, 1f) * currentRot;

            screenShakeContainer.localPosition = new Vector3(x, y, originalPos.z);
            screenShakeContainer.localRotation = originalRot * Quaternion.Euler(0, 0, zTilt);

            elapsed += Time.deltaTime;
            yield return null;
        }

        screenShakeContainer.localPosition = originalPos;
        screenShakeContainer.localRotation = originalRot;
    }

    // Glitch background with adjustable intensity (0..1)
    private IEnumerator GlitchBackgroundRoutine(float duration, float intensity)
    {
        Color originalColor = backgroundImage.color;
        float elapsed = 0f;

        Color[] glitchColors = {
            Color.cyan, Color.magenta, Color.white,
            new Color(0f, 1f, 0.5f), new Color(1f, 0.3f, 0f),
            new Color(0f, 0f, 0f, 0.6f)
        };

        while (elapsed < duration)
        {
            // Higher intensity = more frequent, more saturated hits
            if (UnityEngine.Random.value < intensity)
            {
                Color glitch = glitchColors[UnityEngine.Random.Range(0, glitchColors.Length)];
                backgroundImage.color = Color.Lerp(originalColor, glitch, intensity * 0.85f);
                yield return new WaitForSeconds(0.04f);
                backgroundImage.color = originalColor;
                yield return new WaitForSeconds(0.03f);
            }
            else
            {
                yield return new WaitForSeconds(0.04f);
            }
            elapsed += 0.07f;
        }

        backgroundImage.color = originalColor;
    }

    // Brief full-screen flash (requires winFlashImage assigned in Inspector)
    private IEnumerator FlashScreenRoutine(float peakAlpha, float duration)
    {
        if (winFlashImage == null) yield break;

        float halfDur = duration * 0.4f;
        float elapsed = 0f;

        // Fade in
        while (elapsed < halfDur)
        {
            float a = Mathf.Lerp(0f, peakAlpha, elapsed / halfDur);
            winFlashImage.color = new Color(1, 1, 1, a);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Fade out
        elapsed = 0f;
        float fadeOutDur = duration * 0.6f;
        while (elapsed < fadeOutDur)
        {
            float a = Mathf.Lerp(peakAlpha, 0f, elapsed / fadeOutDur);
            winFlashImage.color = new Color(1, 1, 1, a);
            elapsed += Time.deltaTime;
            yield return null;
        }

        winFlashImage.color = new Color(1, 1, 1, 0);
    }

    // Jackpot: cycles status text through neon colors
    private IEnumerator JackpotStatusRoutine(int winAmount)
    {
        Color[] jackpotColors = { Color.yellow, Color.cyan, Color.magenta, Color.green, Color.white };
        float elapsed = 0f;
        float totalDuration = 2.5f;
        int colorIndex = 0;

        while (elapsed < totalDuration)
        {
            UpdateStatusText($"J A C K P O T !!   +{winAmount} {currentCurrency}", jackpotColors[colorIndex % jackpotColors.Length]);
            colorIndex++;
            yield return new WaitForSeconds(0.12f);
            elapsed += 0.12f;
        }

        UpdateStatusText($"JACKPOT!  +{winAmount} {currentCurrency}", Color.yellow);
    }

    // Counts balance display from old to new value
    private IEnumerator RollUpBalanceRoutine(int fromValue, int toValue, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = 1f - (1f - t) * (1f - t); // ease out
            int displayed = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, t));
            if (balanceText != null)
                balanceText.text = $"BALANCE:\n{displayed} {currentCurrency}";
            elapsed += Time.deltaTime;
            yield return null;
        }
        RefreshBalanceDisplay();
    }

    // ---- Cleanup ----

    private void OnDestroy()
    {
        if (spinButton != null) spinButton.onClick.RemoveListener(OnLocalSpinClicked);
        if (toggleCurrencyButton != null) toggleCurrencyButton.onClick.RemoveListener(OnToggleCurrencyClicked);
        if (increaseBetButton != null) increaseBetButton.onClick.RemoveListener(OnIncreaseBetClicked);
        if (decreaseBetButton != null) decreaseBetButton.onClick.RemoveListener(OnDecreaseBetClicked);
    }
}
