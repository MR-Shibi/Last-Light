using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastLight
{
    public enum GothicSymbol
    {
        Skull = 0,
        Moon = 1,
        Key = 2,
        Crown = 3
    }

    /// <summary>
    /// Floor 3 Final Escape Puzzle: Ancient Stone Lock Panel.
    /// Player inputs the randomized 4-step Gothic Symbol Sequence (Skull, Moon, Key, Crown)
    /// uncovered from the 4 hidden clue plaques/notes on Floor 3 to unlock the final exit gate.
    /// </summary>
    public class SymbolSequencePuzzleUI : MonoBehaviour
    {
        [Header("UI References (Auto-Wired)")]
        public GameObject panelRoot;
        public Text titleText;
        public Text subtitleText;
        public Text feedbackText;

        [Header("4 Sequence Slot Displays (Top Row)")]
        public Image[] slotIconImages;   // 4 slots
        public Text[] slotLabels;        // 4 slots
        public Image[] slotFrames;       // 4 slots

        [Header("4 Carved Symbol Buttons (Skull, Moon, Key, Crown)")]
        public Button[] symbolButtons;   // 0=Skull, 1=Moon, 2=Key, 3=Crown
        public Sprite[] symbolSprites;   // 0=Skull, 1=Moon, 2=Key, 3=Crown

        public Button resetButton;
        public Button closeButton;

        private GothicSymbol[] targetSequence = { GothicSymbol.Skull, GothicSymbol.Moon, GothicSymbol.Key, GothicSymbol.Crown };
        private readonly GothicSymbol[] entered = new GothicSymbol[4];
        private int enteredCount;
        private Action onSolvedCallback;
        private Action onClosedCallback;
        private bool isLockedOut;

        private static readonly string[] SymbolNames = { "SKULL", "MOON", "KEY", "CROWN" };

        private void Awake()
        {
            if (symbolButtons != null)
            {
                for (int i = 0; i < symbolButtons.Length; i++)
                {
                    int symIdx = i;
                    if (symbolButtons[i] != null)
                    {
                        symbolButtons[i].onClick.AddListener(() => OnSymbolPressed((GothicSymbol)symIdx));
                    }
                }
            }

            if (resetButton != null) resetButton.onClick.AddListener(ResetSequence);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Open(string title, string subtitle, GothicSymbol[] expectedSequence, Action onSolved, Action onClosed)
        {
            if (expectedSequence != null && expectedSequence.Length == 4)
            {
                targetSequence = expectedSequence;
            }
            onSolvedCallback = onSolved;
            onClosedCallback = onClosed;
            enteredCount = 0;
            isLockedOut = false;

            if (titleText != null) titleText.text = title;
            if (subtitleText != null) subtitleText.text = subtitle;
            if (feedbackText != null)
            {
                feedbackText.text = "Select the 4 ancient symbols in exact order";
                feedbackText.color = new Color(0.84f, 0.78f, 0.68f, 1f);
            }

            RefreshSlots();
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

        private void ResetSequence()
        {
            if (isLockedOut) return;
            AudioManager.Instance?.PlayPuzzleClick();
            enteredCount = 0;
            if (feedbackText != null)
            {
                feedbackText.text = "Sequence cleared — select 4 symbols";
                feedbackText.color = new Color(0.84f, 0.78f, 0.68f, 1f);
            }
            RefreshSlots();
        }

        private void OnSymbolPressed(GothicSymbol symbol)
        {
            if (isLockedOut || enteredCount >= 4) return;

            AudioManager.Instance?.PlayPuzzleClick();
            entered[enteredCount] = symbol;
            enteredCount++;
            RefreshSlots();

            if (enteredCount == 4)
            {
                ValidateSequence();
            }
        }

        private void ValidateSequence()
        {
            bool match = true;
            for (int i = 0; i < 4; i++)
            {
                if (entered[i] != targetSequence[i])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                isLockedOut = true;
                if (feedbackText != null)
                {
                    feedbackText.text = "THE ANCIENT SEAL BREAKS — GATE UNLOCKED!";
                    feedbackText.color = new Color(0.42f, 0.98f, 0.62f, 1f);
                }
                StartCoroutine(UnlockSuccessRoutine());
            }
            else
            {
                AudioManager.Instance?.PlayPuzzleError();
                if (feedbackText != null)
                {
                    feedbackText.text = "WRONG SEQUENCE — CHECK YOUR SANCTUM PLAQUE NOTES";
                    feedbackText.color = new Color(1f, 0.34f, 0.28f, 1f);
                }
                StartCoroutine(ResetAfterErrorDelay());
            }
        }

        private IEnumerator ResetAfterErrorDelay()
        {
            isLockedOut = true;
            yield return new WaitForSeconds(0.65f);
            enteredCount = 0;
            isLockedOut = false;
            RefreshSlots();
        }

        private IEnumerator UnlockSuccessRoutine()
        {
            AudioManager.Instance?.PlayPuzzleUnlock();
            yield return new WaitForSeconds(0.60f);

            Action solved = onSolvedCallback;
            Action closed = onClosedCallback;
            onSolvedCallback = null;
            onClosedCallback = null;

            if (panelRoot != null) panelRoot.SetActive(false);
            closed?.Invoke();
            solved?.Invoke();
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < 4; i++)
            {
                bool filled = i < enteredCount;
                int symIdx = filled ? (int)entered[i] : -1;

                if (slotIconImages != null && i < slotIconImages.Length && slotIconImages[i] != null)
                {
                    slotIconImages[i].enabled = filled && symbolSprites != null && symIdx >= 0 && symIdx < symbolSprites.Length;
                    if (slotIconImages[i].enabled)
                    {
                        slotIconImages[i].sprite = symbolSprites[symIdx];
                    }
                }

                if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
                {
                    slotLabels[i].text = filled ? SymbolNames[symIdx] : $"STEP {i + 1}";
                    slotLabels[i].color = filled ? new Color(1f, 0.86f, 0.42f, 1f) : new Color(0.55f, 0.52f, 0.48f, 1f);
                }

                if (slotFrames != null && i < slotFrames.Length && slotFrames[i] != null)
                {
                    slotFrames[i].color = filled
                        ? new Color(0.24f, 0.20f, 0.12f, 1f)
                        : new Color(0.10f, 0.10f, 0.12f, 1f);
                }
            }
        }

        public static string GetSymbolDisplayName(GothicSymbol sym)
        {
            switch (sym)
            {
                case GothicSymbol.Skull: return "SKULL";
                case GothicSymbol.Moon: return "CRESCENT MOON";
                case GothicSymbol.Key: return "IRON KEY";
                default: return "ROYAL CROWN";
            }
        }
    }
}
