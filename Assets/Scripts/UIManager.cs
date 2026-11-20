using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastLight
{
    /// <summary>
    /// Graphical Dark-Gothic Victorian UI Manager (COMPASS COMPLETELY REMOVED):
    /// - Start Screen (START + conditional CONTINUE)
    /// - Dark Character Select with live rotating 3D previews
    /// - Live Cutscene Overlay + Skip Button
    /// - HUD (Ornate Oil Vial Gauge capped at 100%, Objective, Floor #, 1P/3P Toggle, Inventory, Options Gear, Virtual Joystick, Contextual OPEN/CLOSE)
    /// - Story Note Parchment Modal, Scrollable Notes Inventory, Tile-Sort Minigame, 3-Digit Keypad,
    ///   Ancient Stone Symbol Sequence Lock, Options Modal, Level Banner Fade, Game Over & Victory.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Top-Level Panels")]
        public GameObject startScreenPanel;
        public GameObject characterSelectPanel;
        public GameObject introRootPanel;
        public GameObject hudPanel;
        public GameObject storyNotePanel;
        public GameObject optionsModalPanel;
        public GameObject gameOverPanel;
        public GameObject victoryPanel;

        [Header("3D Live Preview Stage")]
        public CharacterPreviewStage characterPreviewStage;

        [Header("Start & Dark Character Select")]
        public Button startButton;
        public Button continueButton;
        public Button selectMaleButton;
        public Button selectFemaleButton;

        [Header("Cutscene Overlay")]
        public CanvasGroup introBackdropGroup;
        public Text introStoryText;
        public Button skipIntroButton;

        [Header("HUD Elements (No Compass, No Boost)")]
        public Image oilBarFill;
        public Text oilValueText;
        public Text floorHeaderText;
        public Text objectiveText;
        public VirtualJoystick virtualJoystick;
        public Button interactButton;
        public Text interactButtonLabel;
        public Button flameToggleButton;
        public Text flameToggleLabel;
        public Button lampPowerButton;
        public Text lampPowerLabel;
        public Button cameraToggleButton;
        public Text cameraToggleLabel;
        public Button inventoryButton;
        public Button optionsButton;
        public Text notificationText;
        public CanvasGroup notificationGroup;
        public CanvasGroup banditWarningVignetteGroup;
        public Text banditWarningText;

        [Header("Options Modal (Resume / Restart Level / Save & Exit)")]
        public Button optionsResumeButton;
        public Button optionsRestartButton;
        public Button optionsSaveExitButton;

        [Header("Story Note Modal")]
        public Text noteTitleText;
        public Text noteBodyText;
        public Button noteCloseButton;

        [Header("Escape-Room Puzzle & Inventory UIs")]
        public InventoryManager inventoryManager;
        public TileSortPuzzleUI tileSortPuzzleUI;
        public KeypadPuzzleUI keypadPuzzleUI;
        public SymbolSequencePuzzleUI symbolSequencePuzzleUI;

        [Header("Transition Fade & Level Banner")]
        public CanvasGroup screenFadeGroup;
        public Text levelBannerTitleText;
        public Text levelBannerSubText;

        [Header("Game Over Panel")]
        public Text gameOverTitleText;
        public Text gameOverSubText;
        public Button retryFloorButton;

        [Header("Victory Panel")]
        public Text victoryTimeText;
        public Text victoryOilText;
        public Button playAgainButton;

        private Coroutine notificationRoutine;
        private Action onStoryNoteClosed;

        private readonly Color oilHighColor = new Color(0.98f, 0.75f, 0.22f, 1f);
        private readonly Color oilMidColor = new Color(0.92f, 0.45f, 0.14f, 1f);
        private readonly Color oilLowColor = new Color(0.88f, 0.16f, 0.14f, 1f);

        private void Awake()
        {
            if (notificationGroup != null) notificationGroup.alpha = 0f;
            if (banditWarningVignetteGroup != null)
            {
                banditWarningVignetteGroup.alpha = 0f;
                banditWarningVignetteGroup.blocksRaycasts = false;
            }
            if (banditWarningText != null) banditWarningText.text = string.Empty;
            if (screenFadeGroup != null)
            {
                screenFadeGroup.alpha = 0f;
                screenFadeGroup.blocksRaycasts = false;
            }
            SetLevelBannerText(string.Empty, string.Empty);

            if (noteCloseButton != null)
            {
                noteCloseButton.onClick.RemoveListener(CloseStoryNote);
                noteCloseButton.onClick.AddListener(CloseStoryNote);
            }
        }

        public void ShowStartScreen(bool hasSaveData)
        {
            HideAllPanels();
            SetPanelActive(startScreenPanel, true);
            if (continueButton != null) continueButton.gameObject.SetActive(hasSaveData);
            ClearNotification();
        }

        public void ShowCharacterSelect()
        {
            HideAllPanels();
            SetPanelActive(characterSelectPanel, true);
            characterPreviewStage?.SetPreviewActive(true);
            ClearNotification();
        }

        public void ShowIntroOverlay()
        {
            HideAllPanels();
            SetPanelActive(introRootPanel, true);
            ClearNotification();
        }

        public void ShowHUD()
        {
            HideAllPanels();
            SetPanelActive(hudPanel, true);
        }

        public void SetOptionsModalVisible(bool visible)
        {
            SetPanelActive(optionsModalPanel, visible);
        }

        public void ShowGameOver(int floorNumber, string floorSubtitle)
        {
            HideAllPanels();
            SetPanelActive(gameOverPanel, true);
            ClearNotification();

            if (gameOverTitleText != null) gameOverTitleText.text = "You are lost.";
            if (gameOverSubText != null)
            {
                gameOverSubText.text = $"Your lantern died on Floor {floorNumber} — {floorSubtitle}";
            }
        }

        public void ShowVictory(float totalSeconds, float oilRemaining, float maxOil)
        {
            HideAllPanels();
            SetPanelActive(victoryPanel, true);
            ClearNotification();

            int mins = Mathf.FloorToInt(totalSeconds / 60f);
            float secs = totalSeconds - mins * 60f;
            int clampedOil = Mathf.Clamp(Mathf.RoundToInt(oilRemaining), 0, 100);

            if (victoryTimeText != null) victoryTimeText.text = $"Time Taken:  {mins:00}:{secs:00.0}";
            if (victoryOilText != null) victoryOilText.text = $"Oil Remaining:  {clampedOil}%";
        }

        public void ShowStoryNote(string title, string body, Action onClosed)
        {
            onStoryNoteClosed = onClosed;
            if (noteTitleText != null) noteTitleText.text = title;
            if (noteBodyText != null) noteBodyText.text = body;
            SetPanelActive(storyNotePanel, true);
        }

        public void CloseStoryNote()
        {
            SetPanelActive(storyNotePanel, false);
            AudioManager.Instance?.PlayPuzzleClick();
            Action cb = onStoryNoteClosed;
            onStoryNoteClosed = null;
            cb?.Invoke();
        }

        public void UpdateCameraModeButtonLabel(CameraViewMode mode)
        {
            if (cameraToggleLabel != null)
            {
                cameraToggleLabel.text = mode == CameraViewMode.FirstPerson ? "1P / 3P  [1P]" : "1P / 3P  [3P]";
            }
        }

        public void UpdateFlameModeButton(bool isSpirit)
        {
            if (flameToggleLabel != null)
            {
                flameToggleLabel.text = isSpirit ? "FLAME: SPIRIT" : "FLAME: WARM";
                flameToggleLabel.color = isSpirit
                    ? new Color(0.36f, 0.94f, 1.0f, 1f)
                    : new Color(1.0f, 0.85f, 0.44f, 1f);
            }
            if (flameToggleButton != null)
            {
                Image bg = flameToggleButton.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = isSpirit
                        ? new Color(0.08f, 0.24f, 0.34f, 0.94f)
                        : new Color(0.24f, 0.18f, 0.10f, 0.94f);
                }
            }
        }

        public void UpdateLampPowerButton(bool isLampOn)
        {
            if (lampPowerLabel != null)
            {
                lampPowerLabel.text = isLampOn ? "LAMP: ON" : "LAMP: OFF";
                lampPowerLabel.color = isLampOn
                    ? new Color(1.0f, 0.88f, 0.48f, 1f)
                    : new Color(0.76f, 0.76f, 0.80f, 1f);
            }
            if (lampPowerButton != null)
            {
                Image bg = lampPowerButton.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = isLampOn
                        ? new Color(0.42f, 0.28f, 0.10f, 0.94f)
                        : new Color(0.12f, 0.12f, 0.14f, 0.94f);
                }
            }
        }

        public void SetInteractPrompt(bool visible, string label)
        {
            if (interactButton != null && interactButton.gameObject.activeSelf != visible)
            {
                interactButton.gameObject.SetActive(visible);
            }
            if (visible && interactButtonLabel != null && !string.IsNullOrEmpty(label))
            {
                interactButtonLabel.text = label;
            }
        }

        public void UpdateOilUI(float currentOil, float maxOil)
        {
            float clamped = Mathf.Clamp(currentOil, 0f, 100f);
            float ratio = clamped / 100f;

            if (oilBarFill != null)
            {
                oilBarFill.fillAmount = ratio;
                if (ratio > 0.5f)
                {
                    oilBarFill.color = Color.Lerp(oilMidColor, oilHighColor, (ratio - 0.5f) * 2f);
                }
                else
                {
                    Color lowCol = Color.Lerp(oilLowColor, oilMidColor, ratio * 2f);
                    if (ratio < 0.25f)
                    {
                        float pulse = 0.68f + 0.32f * Mathf.Sin(Time.time * 11f);
                        lowCol *= pulse;
                        lowCol.a = 1f;
                    }
                    oilBarFill.color = lowCol;
                }
            }

            if (oilValueText != null)
            {
                int pct = Mathf.Clamp(Mathf.RoundToInt(clamped), 0, 100);
                oilValueText.text = $"OIL  {pct}%";
            }
        }

        public void UpdateFloorInfo(int floorNumber, string floorSubtitle, string objective)
        {
            if (floorHeaderText != null)
            {
                floorHeaderText.text = $"FLOOR {floorNumber}  •  {floorSubtitle.ToUpperInvariant()}";
            }
            if (objectiveText != null) objectiveText.text = objective;
        }

        public void UpdateObjectiveOnly(string objective)
        {
            if (objectiveText != null) objectiveText.text = objective;
        }

        public void ShowNotification(string message, Color color, float duration = 2.4f)
        {
            if (notificationText == null || notificationGroup == null) return;
            if (notificationRoutine != null) StopCoroutine(notificationRoutine);
            notificationRoutine = StartCoroutine(NotificationCoroutine(message, color, duration));
        }

        public void ClearNotification()
        {
            if (notificationRoutine != null)
            {
                StopCoroutine(notificationRoutine);
                notificationRoutine = null;
            }
            if (notificationGroup != null) notificationGroup.alpha = 0f;
        }

        public void SetLevelBannerText(string title, string subtitle)
        {
            if (levelBannerTitleText != null) levelBannerTitleText.text = title;
            if (levelBannerSubText != null) levelBannerSubText.text = subtitle;
        }

        public IEnumerator FadeScreen(float targetAlpha, float duration)
        {
            if (screenFadeGroup == null) yield break;

            screenFadeGroup.blocksRaycasts = targetAlpha > 0.05f;
            float startAlpha = screenFadeGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration));
                screenFadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            screenFadeGroup.alpha = targetAlpha;
            screenFadeGroup.blocksRaycasts = targetAlpha > 0.05f;
        }

        private void HideAllPanels()
        {
            SetPanelActive(startScreenPanel, false);
            SetPanelActive(characterSelectPanel, false);
            SetPanelActive(introRootPanel, false);
            SetPanelActive(hudPanel, false);
            SetPanelActive(storyNotePanel, false);
            SetPanelActive(optionsModalPanel, false);
            SetPanelActive(gameOverPanel, false);
            SetPanelActive(victoryPanel, false);

            characterPreviewStage?.SetPreviewActive(false);

            if (inventoryManager != null && inventoryManager.inventoryPanelRoot != null)
                inventoryManager.inventoryPanelRoot.SetActive(false);
            if (tileSortPuzzleUI != null && tileSortPuzzleUI.panelRoot != null)
                tileSortPuzzleUI.panelRoot.SetActive(false);
            if (keypadPuzzleUI != null && keypadPuzzleUI.panelRoot != null)
                keypadPuzzleUI.panelRoot.SetActive(false);
            if (symbolSequencePuzzleUI != null && symbolSequencePuzzleUI.panelRoot != null)
                symbolSequencePuzzleUI.panelRoot.SetActive(false);
        }

        private IEnumerator NotificationCoroutine(string message, Color color, float duration)
        {
            notificationText.text = message;
            notificationText.color = color;

            float fadeIn = 0.18f;
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                notificationGroup.alpha = Mathf.Clamp01(elapsed / fadeIn);
                yield return null;
            }
            notificationGroup.alpha = 1f;

            yield return new WaitForSeconds(Mathf.Max(0.25f, duration - 0.58f));

            float fadeOut = 0.4f;
            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                notificationGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut);
                yield return null;
            }

            notificationGroup.alpha = 0f;
            notificationRoutine = null;
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }
    }
}
