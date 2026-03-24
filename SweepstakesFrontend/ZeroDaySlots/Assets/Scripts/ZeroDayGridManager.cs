using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/**
 * ZeroDayGridManager
 * UPGRADE: Converted from a flat 25-cell shuffle to a 5-Reel sequential column engine.
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

    private bool[] isReelSpinning = new bool[5];
    private Coroutine[] reelCoroutines = new Coroutine[5];

    public void BeginDigitalSpin()
    {
        // Start all 5 columns spinning simultaneously
        for (int i = 0; i < 5; i++)
        {
            isReelSpinning[i] = true;
            if (reelCoroutines[i] != null) StopCoroutine(reelCoroutines[i]);
            reelCoroutines[i] = StartCoroutine(SpinReelRoutine(i));
        }
    }

    private IEnumerator SpinReelRoutine(int reelIndex)
    {
        while (isReelSpinning[reelIndex])
        {
            // Rapidly cycle random images for the 5 cells in this specific column
            for (int row = 0; row < 5; row++)
            {
                int cellIndex = (row * 5) + reelIndex;
                if (cellIndex < gridCells.Length && gridCells[cellIndex] != null)
                {
                    if (symbolDirectory.Length > 0)
                    {
                        int randomIcon = Random.Range(0, symbolDirectory.Length);
                        gridCells[cellIndex].sprite = symbolDirectory[randomIcon].symbolImage;
                    }
                }
            }
            yield return new WaitForSeconds(0.05f); // High-speed blur effect
        }
    }

    // NEW: Stops a specific column and locks in the server's authoritative payload
    public void StopReel(int reelIndex, string[] finalSymbols)
    {
        isReelSpinning[reelIndex] = false;
        if (reelCoroutines[reelIndex] != null)
        {
            StopCoroutine(reelCoroutines[reelIndex]);
        }

        for (int row = 0; row < 5; row++)
        {
            int cellIndex = (row * 5) + reelIndex;
            if (cellIndex < gridCells.Length && cellIndex < finalSymbols.Length)
            {
                string targetName = finalSymbols[cellIndex].Trim();
                Sprite targetSprite = GetSpriteForSymbol(targetName);
                if (targetSprite != null && gridCells[cellIndex] != null)
                {
                    gridCells[cellIndex].sprite = targetSprite;
                }
            }
        }
    }

    // Instantly snaps all reels (used for Boot Sequence)
    public void UpdateGridVisuals(string gridData)
    {
        if (string.IsNullOrEmpty(gridData)) return;
        string[] symbols = gridData.Split(',');
        for (int i = 0; i < 5; i++)
        {
            StopReel(i, symbols);
        }
    }

    private Sprite GetSpriteForSymbol(string name)
    {
        foreach (var sym in symbolDirectory)
        {
            if (sym.symbolName == name) return sym.symbolImage;
        }
        return null;
    }
}