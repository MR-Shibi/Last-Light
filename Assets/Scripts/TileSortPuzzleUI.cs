using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastLight
{
    /// <summary>
    /// Mobile 4-Piece Tile-Sort Escape-Room Minigame.
    /// Tap two tiles to swap them until they match the ascending sequence 1 - 2 - 3 - 4.
    /// </summary>
    public class TileSortPuzzleUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panelRoot;
        public Text titleText;
        public Text statusText;
        public Button[] tileButtons;     // 4 slots
        public Image[] tileBackgrounds;  // 4 slots
        public Text[] tileLabels;        // 4 slots
        public Button closeButton;

        private readonly int[] currentOrder = new int[4];
        private int selectedIndex = -1;
        private Action onSolvedCallback;
        private Action onClosedCallback;
        private bool solved;

        private readonly string[] tileSymbols = { "I\n[1]\nEMBER", "II\n[2]\nFLAME", "III\n[3]\nTORCH", "IV\n[4]\nSUN" };
        private readonly Color[] tileColors =
        {
            new Color(0.32f, 0.20f, 0.14f, 1f),
            new Color(0.45f, 0.28f, 0.14f, 1f),
            new Color(0.58f, 0.39f, 0.15f, 1f),
            new Color(0.72f, 0.54f, 0.18f, 1f)
        };
        private readonly Color selectedTint = new Color(1f, 0.86f, 0.35f, 1f);
        private readonly Color solvedTint = new Color(0.28f, 0.78f, 0.45f, 1f);

        private void Awake()
        {
            if (tileButtons != null)
            {
                for (int i = 0; i < tileButtons.Length; i++)
                {
                    int slotIdx = i;
                    if (tileButtons[i] != null)
                    {
                        tileButtons[i].onClick.AddListener(() => OnTileTapped(slotIdx));
                    }
                }
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        public void Open(string title, Action onSolved, Action onClosed)
        {
            onSolvedCallback = onSolved;
            onClosedCallback = onClosed;
            selectedIndex = -1;
            solved = false;

            if (titleText != null) titleText.text = title;
            if (statusText != null) statusText.text = "Tap two tiles to swap them into order:  1  →  2  →  3  →  4";

            ScrambleTiles();
            RefreshVisuals();

            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            Action closed = onClosedCallback;
            onClosedCallback = null;
            onSolvedCallback = null;
            closed?.Invoke();
        }

        private void ScrambleTiles()
        {
            // Guaranteed non-solved starting permutation
            int[][] scrambles =
            {
                new[] { 3, 1, 4, 2 },
                new[] { 2, 4, 1, 3 },
                new[] { 4, 2, 3, 1 },
                new[] { 3, 4, 2, 1 }
            };
            int[] pick = scrambles[UnityEngine.Random.Range(0, scrambles.Length)];
            for (int i = 0; i < 4; i++)
            {
                currentOrder[i] = pick[i];
            }
        }

        private void OnTileTapped(int index)
        {
            if (solved || index < 0 || index >= 4) return;
            AudioManager.Instance?.PlayPuzzleClick();

            if (selectedIndex < 0)
            {
                selectedIndex = index;
                RefreshVisuals();
                return;
            }

            if (selectedIndex == index)
            {
                selectedIndex = -1;
                RefreshVisuals();
                return;
            }

            // Swap the two tapped tiles
            int tmp = currentOrder[selectedIndex];
            currentOrder[selectedIndex] = currentOrder[index];
            currentOrder[index] = tmp;
            selectedIndex = -1;

            RefreshVisuals();

            if (IsSolved())
            {
                solved = true;
                if (statusText != null) statusText.text = "UNLOCKED!";
                RefreshVisuals();
                StartCoroutine(CompleteAfterBriefDelay());
            }
        }

        private bool IsSolved()
        {
            return currentOrder[0] == 1 && currentOrder[1] == 2 && currentOrder[2] == 3 && currentOrder[3] == 4;
        }

        private IEnumerator CompleteAfterBriefDelay()
        {
            AudioManager.Instance?.PlayPuzzleUnlock();
            yield return new WaitForSeconds(0.55f);

            Action solvedCb = onSolvedCallback;
            Action closedCb = onClosedCallback;
            onSolvedCallback = null;
            onClosedCallback = null;

            if (panelRoot != null) panelRoot.SetActive(false);
            closedCb?.Invoke();
            solvedCb?.Invoke();
        }

        private void RefreshVisuals()
        {
            for (int i = 0; i < 4; i++)
            {
                int val = Mathf.Clamp(currentOrder[i], 1, 4);
                if (tileLabels != null && i < tileLabels.Length && tileLabels[i] != null)
                {
                    tileLabels[i].text = tileSymbols[val - 1];
                    tileLabels[i].color = (selectedIndex == i) ? new Color(0.08f, 0.05f, 0.02f, 1f) : Color.white;
                }
                if (tileBackgrounds != null && i < tileBackgrounds.Length && tileBackgrounds[i] != null)
                {
                    if (solved)
                    {
                        tileBackgrounds[i].color = solvedTint;
                    }
                    else if (selectedIndex == i)
                    {
                        tileBackgrounds[i].color = selectedTint;
                    }
                    else
                    {
                        tileBackgrounds[i].color = tileColors[val - 1];
                    }
                }
            }
        }
    }
}
