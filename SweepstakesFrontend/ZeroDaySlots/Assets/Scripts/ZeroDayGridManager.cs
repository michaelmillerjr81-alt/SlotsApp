using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/**
 * ZeroDayGridManager
 * 5-reel sequential column engine with deceleration, bounce-on-land, and symbol pop.
 */
public class ZeroDayGridManager : MonoBehaviour
{
    [System.Serializable]
    public class SymbolData
    {
        public string symbolName;
        public Sprite symbolImage;
    }

    [Header("Visual Dictionary")]
    public SymbolData[] symbolDirectory;

    [Header("Grid Layout (0-24)")]
    [Tooltip("Ensure all 25 Image slots are linked here, from top-left to bottom-right.")]
    public Image[] gridCells;

    [Header("Reel Animation Settings")]
    public float reelBounceDistance = 22f;
    public float reelBounceDuration = 0.22f;
    public float symbolPopScale = 1.28f;
    public float symbolPopDuration = 0.11f;

    private bool[] isReelSpinning = new bool[5];
    private float[] reelSpinDelay = new float[5];
    private Coroutine[] reelCoroutines = new Coroutine[5];
    private Coroutine[] bounceCoroutines = new Coroutine[5];
    private Coroutine[] decelerateCoroutines = new Coroutine[5];

    // ---- Public API ----

    public void BeginDigitalSpin()
    {
        for (int i = 0; i < 5; i++)
        {
            reelSpinDelay[i] = 0.05f;
            isReelSpinning[i] = true;
            if (reelCoroutines[i] != null) StopCoroutine(reelCoroutines[i]);
            reelCoroutines[i] = StartCoroutine(SpinReelRoutine(i));
        }
    }

    // Begin slowing this reel — call before StopReel for a deceleration feel
    public void DecelerateReel(int reelIndex)
    {
        if (decelerateCoroutines[reelIndex] != null) StopCoroutine(decelerateCoroutines[reelIndex]);
        decelerateCoroutines[reelIndex] = StartCoroutine(DecelerateReelRoutine(reelIndex));
    }

    public void StopReel(int reelIndex, string[] finalSymbols)
    {
        isReelSpinning[reelIndex] = false;
        if (reelCoroutines[reelIndex] != null) StopCoroutine(reelCoroutines[reelIndex]);
        if (decelerateCoroutines[reelIndex] != null) StopCoroutine(decelerateCoroutines[reelIndex]);

        // Snap to final symbols
        for (int row = 0; row < 5; row++)
        {
            int cellIndex = (row * 5) + reelIndex;
            if (cellIndex < gridCells.Length && cellIndex < finalSymbols.Length)
            {
                string targetName = finalSymbols[cellIndex].Trim();
                Sprite targetSprite = GetSpriteForSymbol(targetName);
                if (targetSprite != null && gridCells[cellIndex] != null)
                    gridCells[cellIndex].sprite = targetSprite;
            }
        }

        // Bounce + pop
        if (bounceCoroutines[reelIndex] != null) StopCoroutine(bounceCoroutines[reelIndex]);
        bounceCoroutines[reelIndex] = StartCoroutine(BounceReelRoutine(reelIndex));
    }

    // Instantly loads grid with no animation (used on boot)
    public void UpdateGridVisuals(string gridData)
    {
        if (string.IsNullOrEmpty(gridData)) return;
        string[] symbols = gridData.Split(',');
        for (int i = 0; i < 5; i++)
        {
            isReelSpinning[i] = false;
            if (reelCoroutines[i] != null) StopCoroutine(reelCoroutines[i]);
            for (int row = 0; row < 5; row++)
            {
                int cellIndex = (row * 5) + i;
                if (cellIndex < gridCells.Length && cellIndex < symbols.Length)
                {
                    Sprite s = GetSpriteForSymbol(symbols[cellIndex].Trim());
                    if (s != null && gridCells[cellIndex] != null)
                        gridCells[cellIndex].sprite = s;
                }
            }
        }
    }

    // ---- Spin Coroutines ----

    private IEnumerator SpinReelRoutine(int reelIndex)
    {
        while (isReelSpinning[reelIndex])
        {
            for (int row = 0; row < 5; row++)
            {
                int cellIndex = (row * 5) + reelIndex;
                if (cellIndex < gridCells.Length && gridCells[cellIndex] != null && symbolDirectory.Length > 0)
                {
                    int randomIcon = Random.Range(0, symbolDirectory.Length);
                    gridCells[cellIndex].sprite = symbolDirectory[randomIcon].symbolImage;
                }
            }
            yield return new WaitForSeconds(reelSpinDelay[reelIndex]);
        }
    }

    private IEnumerator DecelerateReelRoutine(int reelIndex)
    {
        while (isReelSpinning[reelIndex] && reelSpinDelay[reelIndex] < 0.2f)
        {
            reelSpinDelay[reelIndex] = Mathf.Min(reelSpinDelay[reelIndex] + 0.015f, 0.2f);
            yield return new WaitForSeconds(0.04f);
        }
    }

    // ---- Impact Animation ----

    private IEnumerator BounceReelRoutine(int reelIndex)
    {
        RectTransform[] cells = new RectTransform[5];
        Vector2[] originalPos = new Vector2[5];

        for (int row = 0; row < 5; row++)
        {
            int cellIndex = (row * 5) + reelIndex;
            if (cellIndex < gridCells.Length && gridCells[cellIndex] != null)
            {
                cells[row] = gridCells[cellIndex].rectTransform;
                originalPos[row] = cells[row].anchoredPosition;
            }
        }

        // Phase 1: slam down (impact)
        float elapsed = 0f;
        float impactDuration = reelBounceDuration * 0.35f;
        while (elapsed < impactDuration)
        {
            float t = EaseOut(elapsed / impactDuration);
            float offset = Mathf.Lerp(0f, reelBounceDistance, t);
            for (int row = 0; row < 5; row++)
                if (cells[row] != null)
                    cells[row].anchoredPosition = originalPos[row] + Vector2.down * offset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Phase 2: spring back with overshoot
        elapsed = 0f;
        float springDuration = reelBounceDuration * 0.65f;
        while (elapsed < springDuration)
        {
            float t = elapsed / springDuration;
            float spring = SpringEase(t);
            float offset = reelBounceDistance * (1f - spring);
            for (int row = 0; row < 5; row++)
                if (cells[row] != null)
                    cells[row].anchoredPosition = originalPos[row] + Vector2.down * Mathf.Max(-5f, offset);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap exact
        for (int row = 0; row < 5; row++)
            if (cells[row] != null)
                cells[row].anchoredPosition = originalPos[row];

        // Pop each symbol top-to-bottom, staggered
        for (int row = 0; row < 5; row++)
        {
            int cellIndex = (row * 5) + reelIndex;
            if (cellIndex < gridCells.Length && gridCells[cellIndex] != null)
                StartCoroutine(PopSymbolRoutine(gridCells[cellIndex].rectTransform));
            yield return new WaitForSeconds(0.03f);
        }
    }

    private IEnumerator PopSymbolRoutine(RectTransform cell)
    {
        Vector3 originalScale = cell.localScale;
        Vector3 popScale = originalScale * symbolPopScale;
        float halfDur = symbolPopDuration * 0.5f;

        float elapsed = 0f;
        while (elapsed < halfDur)
        {
            cell.localScale = Vector3.Lerp(originalScale, popScale, elapsed / halfDur);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < halfDur)
        {
            cell.localScale = Vector3.Lerp(popScale, originalScale, elapsed / halfDur);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cell.localScale = originalScale;
    }

    // ---- Easing Helpers ----

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

    // Exponential spring that overshoots 1.0 briefly then settles
    private static float SpringEase(float t) => 1f - Mathf.Exp(-9f * t) * Mathf.Cos(13f * t);

    // ---- Utility ----

    private Sprite GetSpriteForSymbol(string name)
    {
        foreach (var sym in symbolDirectory)
            if (sym.symbolName == name) return sym.symbolImage;
        return null;
    }
}
