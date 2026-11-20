using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastLight
{
    /// <summary>
    /// Mobile 3-Digit Escape-Room Keypad UI for math riddle & final vault code locks.
    /// </summary>
    public class KeypadPuzzleUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panelRoot;
        public Text titleText;
        public Text subtitleText;
        public Text codeDisplayText;
        public Text feedbackText;
        public Button[] digitButtons; // 0..9
        public Button clearButton;
        public Button enterButton;
        public Button closeButton;

        private string targetCode = "942";
        private string entered = "";
        private Action onSolvedCallback;
        private Action onClosedCallback;
        private bool isLockedOut;

        private void Awake()
        {
            if (digitButtons != null)
            {
                for (int i = 0; i < digitButtons.Length; i++)
                {
                    int d = i;
                    if (digitButtons[i] != null)
                    {
                        digitButtons[i].onClick.AddListener(() => AppendDigit(d));
                    }
                }
            }

            if (clearButton != null) clearButton.onClick.AddListener(ClearCode);
            if (enterButton != null) enterButton.onClick.AddListener(SubmitCode);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        public void Open(string title, string subtitle, string expectedCode, Action onSolved, Action onClosed)
        {
            targetCode = string.IsNullOrEmpty(expectedCode) ? "942" : expectedCode;
            entered = "";
            isLockedOut = false;
            onSolvedCallback = onSolved;
            onClosedCallback = onClosed;

            if (titleText != null) titleText.text = title;
            if (subtitleText != null) subtitleText.text = subtitle;
            if (feedbackText != null)
            {
                feedbackText.text = "Enter the 3-digit code";
                feedbackText.color = new Color(0.82f, 0.78f, 0.70f, 1f);
            }

            UpdateDisplay();
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

        private void AppendDigit(int digit)
        {
            if (isLockedOut) return;
            if (entered.Length >= targetCode.Length) return;

            AudioManager.Instance?.PlayPuzzleClick();
            entered += digit.ToString();
            UpdateDisplay();
        }

        private void ClearCode()
        {
            if (isLockedOut) return;
            AudioManager.Instance?.PlayPuzzleClick();
            entered = "";
            if (feedbackText != null)
            {
                feedbackText.text = "Code cleared";
                feedbackText.color = new Color(0.82f, 0.78f, 0.70f, 1f);
            }
            UpdateDisplay();
        }

        private void SubmitCode()
        {
            if (isLockedOut) return;

            if (entered == targetCode)
            {
                isLockedOut = true;
                if (feedbackText != null)
                {
                    feedbackText.text = "ACCESS GRANTED — DOOR UNLOCKED!";
                    feedbackText.color = new Color(0.35f, 1f, 0.62f, 1f);
                }
                StartCoroutine(SuccessRoutine());
            }
            else
            {
                AudioManager.Instance?.PlayPuzzleError();
                if (feedbackText != null)
                {
                    feedbackText.text = "WRONG CODE — CHECK THE STORY NOTES";
                    feedbackText.color = new Color(1f, 0.35f, 0.30f, 1f);
                }
                entered = "";
                UpdateDisplay();
            }
        }

        private IEnumerator SuccessRoutine()
        {
            AudioManager.Instance?.PlayPuzzleUnlock();
            yield return new WaitForSeconds(0.55f);

            Action solved = onSolvedCallback;
            Action closed = onClosedCallback;
            onSolvedCallback = null;
            onClosedCallback = null;

            if (panelRoot != null) panelRoot.SetActive(false);
            closed?.Invoke();
            solved?.Invoke();
        }

        private void UpdateDisplay()
        {
            if (codeDisplayText == null) return;
            int len = Mathf.Max(3, targetCode.Length);
            char[] chars = new char[len];
            for (int i = 0; i < len; i++)
            {
                chars[i] = i < entered.Length ? entered[i] : '_';
            }
            codeDisplayText.text = string.Join("   ", chars);
        }
    }
}
