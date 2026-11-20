using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastLight
{
    public enum GameState
    {
        StartScreen,
        CharacterSelect,
        Intro,
        Playing,
        ModalPaused,
        FloorTransition,
        GameOver,
        Victory
    }

    /// <summary>
    /// Top-level game loop manager for "Last Light":
    /// - Start Screen (with Save & Exit -> CONTINUE support via PlayerPrefs)
    /// - Dark Character Select with live rotating 3D Male/Female previews
    /// - 2-Act Live Movie Cutscene (Jungle Campfire Ambush + Old Haveli Bed Wake-Up)
    /// - First-Person Default + Over-the-Shoulder 3P Toggle Camera
    /// - Strict 100%-Capped High-Challenge Oil Economy (75% start, 4.2%/sec drain, NO Boost)
    /// - 3 Distinct Randomized Puzzles (Floor 1 Tile-Sort, Floor 2 Random Math Code, Floor 3 Random 4-Symbol Stone Lock)
    /// - Notes Inventory, Options Gear Menu, Staircase Transitions, Game Over & Victory.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private const string SaveKeyHasSave = "LastLight_HasSave";
        private const string SaveKeyFloor = "LastLight_FloorIndex";
        private const string SaveKeyOil = "LastLight_Oil";
        private const string SaveKeyTime = "LastLight_PlayTime";
        private const string SaveKeyGender = "LastLight_Gender";
        private const string SaveKeyNotes = "LastLight_NotesJson";

        [Header("Core References")]
        public PlayerController playerController;
        public LampController lampController;
        public IntroManager introManager;
        public UIManager uiManager;
        public CameraController cameraController;
        public InventoryManager inventoryManager;
        public MansionThreatManager threatManager;

        [Header("Floors (1, 2, 3)")]
        public FloorController[] floors;

        private GameState currentState = GameState.StartScreen;
        private CharacterGender selectedGender = CharacterGender.Male;
        private int currentFloorIndex;
        private float totalPlayTime;
        private float oilAtFloorStart = 75f;

        public GameState CurrentState => currentState;
        public int CurrentFloorIndex => currentFloorIndex;
        public FloorController ActiveFloor => (floors != null && currentFloorIndex >= 0 && currentFloorIndex < floors.Length)
            ? floors[currentFloorIndex]
            : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnforcePureBlackDarkness();
        }

        private void Start()
        {
            Time.timeScale = 1f;
            WireEvents();
            AudioManager.Instance?.StartAmbientDrone();
            EnterStartScreen();
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                totalPlayTime += Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            if (lampController != null)
            {
                lampController.OnOilChanged -= HandleOilChanged;
                lampController.OnOilDepleted -= HandleOilDepleted;
            }
        }

        private void EnforcePureBlackDarkness()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.ambientIntensity = 0f;
            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.fog = false;
        }

        private void WireEvents()
        {
            if (lampController != null)
            {
                lampController.OnOilChanged -= HandleOilChanged;
                lampController.OnOilChanged += HandleOilChanged;
                lampController.OnOilDepleted -= HandleOilDepleted;
                lampController.OnOilDepleted += HandleOilDepleted;
            }

            if (uiManager == null) return;

            if (uiManager.startButton != null)
            {
                uiManager.startButton.onClick.RemoveListener(OnStartButtonClicked);
                uiManager.startButton.onClick.AddListener(OnStartButtonClicked);
            }
            if (uiManager.continueButton != null)
            {
                uiManager.continueButton.onClick.RemoveListener(OnContinueButtonClicked);
                uiManager.continueButton.onClick.AddListener(OnContinueButtonClicked);
            }
            if (uiManager.selectMaleButton != null)
            {
                uiManager.selectMaleButton.onClick.RemoveAllListeners();
                uiManager.selectMaleButton.onClick.AddListener(() => OnCharacterSelected(CharacterGender.Male));
            }
            if (uiManager.selectFemaleButton != null)
            {
                uiManager.selectFemaleButton.onClick.RemoveAllListeners();
                uiManager.selectFemaleButton.onClick.AddListener(() => OnCharacterSelected(CharacterGender.Female));
            }
            if (uiManager.interactButton != null)
            {
                uiManager.interactButton.onClick.RemoveAllListeners();
                uiManager.interactButton.onClick.AddListener(() => playerController?.InteractWithNearest());
            }
            if (uiManager.cameraToggleButton != null)
            {
                uiManager.cameraToggleButton.onClick.RemoveAllListeners();
                uiManager.cameraToggleButton.onClick.AddListener(ToggleCameraMode);
            }
            if (uiManager.flameToggleButton != null)
            {
                uiManager.flameToggleButton.onClick.RemoveAllListeners();
                uiManager.flameToggleButton.onClick.AddListener(ToggleFlameMode);
            }
            if (uiManager.lampPowerButton != null)
            {
                uiManager.lampPowerButton.onClick.RemoveAllListeners();
                uiManager.lampPowerButton.onClick.AddListener(ToggleLampPower);
            }
            if (uiManager.inventoryButton != null)
            {
                uiManager.inventoryButton.onClick.RemoveAllListeners();
                uiManager.inventoryButton.onClick.AddListener(OpenInventoryPanel);
            }
            if (uiManager.optionsButton != null)
            {
                uiManager.optionsButton.onClick.RemoveAllListeners();
                uiManager.optionsButton.onClick.AddListener(OpenOptionsMenu);
            }
            if (uiManager.optionsResumeButton != null)
            {
                uiManager.optionsResumeButton.onClick.RemoveAllListeners();
                uiManager.optionsResumeButton.onClick.AddListener(CloseOptionsMenu);
            }
            if (uiManager.optionsRestartButton != null)
            {
                uiManager.optionsRestartButton.onClick.RemoveAllListeners();
                uiManager.optionsRestartButton.onClick.AddListener(RestartCurrentFloorFromOptions);
            }
            if (uiManager.optionsSaveExitButton != null)
            {
                uiManager.optionsSaveExitButton.onClick.RemoveAllListeners();
                uiManager.optionsSaveExitButton.onClick.AddListener(SaveAndExitToStartScreen);
            }
            if (uiManager.retryFloorButton != null)
            {
                uiManager.retryFloorButton.onClick.RemoveListener(RetryCurrentFloor);
                uiManager.retryFloorButton.onClick.AddListener(RetryCurrentFloor);
            }
            if (uiManager.playAgainButton != null)
            {
                uiManager.playAgainButton.onClick.RemoveListener(PlayAgain);
                uiManager.playAgainButton.onClick.AddListener(PlayAgain);
            }
        }

        public static bool HasSaveData() => PlayerPrefs.GetInt(SaveKeyHasSave, 0) == 1;

        private void EnterStartScreen()
        {
            currentState = GameState.StartScreen;
            currentFloorIndex = 0;
            totalPlayTime = 0f;
            oilAtFloorStart = lampController != null ? lampController.startingOil : 75f;

            lampController?.ResetLampModesForFloor();
            threatManager?.ResetPatrolState();

            playerController?.SetSelectedGender(selectedGender);
            ActivateOnlyFloor(0, showNightstandLampForIntro: true, randomizePuzzles: false);

            if (introManager != null && ActiveFloor != null)
            {
                introManager.PrepareBedIntroPose(ActiveFloor);
            }

            if (uiManager != null)
            {
                uiManager.ShowStartScreen(HasSaveData());
                if (lampController != null)
                {
                    uiManager.UpdateOilUI(lampController.startingOil, 100f);
                }
            }
        }

        public void OnStartButtonClicked()
        {
            if (currentState != GameState.StartScreen && currentState != GameState.Victory) return;
            AudioManager.Instance?.PlayPuzzleClick();

            currentState = GameState.CharacterSelect;
            uiManager?.ShowCharacterSelect();
        }

        public void OnContinueButtonClicked()
        {
            if (currentState != GameState.StartScreen || !HasSaveData()) return;
            AudioManager.Instance?.PlayPuzzleClick();

            int savedFloor = Mathf.Clamp(PlayerPrefs.GetInt(SaveKeyFloor, 0), 0, Mathf.Max(0, (floors?.Length ?? 1) - 1));
            float savedOil = Mathf.Clamp(PlayerPrefs.GetFloat(SaveKeyOil, 75f), 25f, 100f);
            float savedTime = Mathf.Max(0f, PlayerPrefs.GetFloat(SaveKeyTime, 0f));
            selectedGender = (CharacterGender)PlayerPrefs.GetInt(SaveKeyGender, 0);
            string notesJson = PlayerPrefs.GetString(SaveKeyNotes, "");

            playerController?.SetSelectedGender(selectedGender);
            inventoryManager?.DeserializeNotesFromJson(notesJson);

            currentFloorIndex = savedFloor;
            totalPlayTime = savedTime;
            oilAtFloorStart = savedOil;

            introManager?.CancelIntroForDirectStart(ActiveFloor);
            ActivateOnlyFloor(currentFloorIndex, showNightstandLampForIntro: false, randomizePuzzles: true);
            inventoryManager?.ClearNotesForFloor(currentFloorIndex + 1);

            cameraController?.SetViewMode(CameraViewMode.FirstPerson);
            BeginGameplayOnCurrentFloor(resetOilAmount: savedOil, teleportToSpawn: true);
            uiManager?.ShowNotification($"RESUMED FLOOR {currentFloorIndex + 1} — FRESH PUZZLE GENERATED", new Color(1f, 0.86f, 0.45f, 1f), 2.2f);
        }

        public void OnCharacterSelected(CharacterGender gender)
        {
            if (currentState != GameState.CharacterSelect) return;

            selectedGender = gender;
            AudioManager.Instance?.PlayPuzzleClick();
            playerController?.SetSelectedGender(selectedGender);
            inventoryManager?.ClearAllNotes();

            currentState = GameState.Intro;
            currentFloorIndex = 0;
            totalPlayTime = 0f;
            oilAtFloorStart = lampController != null ? lampController.startingOil : 75f;

            // Randomize Floor 1 fresh for the new game
            ActivateOnlyFloor(0, showNightstandLampForIntro: true, randomizePuzzles: true);

            if (introManager != null && ActiveFloor != null)
            {
                introManager.PlayIntro(ActiveFloor, OnIntroComplete);
            }
            else
            {
                OnIntroComplete();
            }
        }

        private void OnIntroComplete()
        {
            float startOil = lampController != null ? lampController.startingOil : 75f;
            BeginGameplayOnCurrentFloor(resetOilAmount: startOil, teleportToSpawn: false);
            uiManager?.ShowNotification("SEARCH DRAWERS FOR 4 PUZZLE PIECES & OIL! (MAX 100%)", new Color(1f, 0.88f, 0.45f, 1f), 2.8f);
        }

        public void RetryCurrentFloor()
        {
            AudioManager.Instance?.PlayPuzzleClick();
            RestartCurrentFloorInternal();
        }

        private void RestartCurrentFloorFromOptions()
        {
            AudioManager.Instance?.PlayPuzzleClick();
            uiManager?.SetOptionsModalVisible(false);
            RestartCurrentFloorInternal();
        }

        private void RestartCurrentFloorInternal()
        {
            introManager?.CancelIntroForDirectStart(ActiveFloor);

            // Clear old notes from THIS floor so newly randomized Floor 2/3 clues never collide with old notes!
            inventoryManager?.ClearNotesForFloor(currentFloorIndex + 1);

            // Re-randomize this floor's puzzle on every Retry / Restart Level!
            ActivateOnlyFloor(currentFloorIndex, showNightstandLampForIntro: false, randomizePuzzles: true);

            float defaultStart = lampController != null ? lampController.startingOil : 75f;
            float retryOil = currentFloorIndex == 0
                ? defaultStart
                : Mathf.Clamp(Mathf.Max(oilAtFloorStart, 65f), 0f, 100f);

            cameraController?.SetViewMode(CameraViewMode.FirstPerson);
            BeginGameplayOnCurrentFloor(resetOilAmount: retryOil, teleportToSpawn: true);
            uiManager?.ShowNotification($"FLOOR {currentFloorIndex + 1} RESTARTED — NEW PUZZLE GENERATED", new Color(1f, 0.85f, 0.45f, 1f), 2.1f);
        }

        public void SaveAndExitToStartScreen()
        {
            AudioManager.Instance?.PlayPuzzleClick();

            PlayerPrefs.SetInt(SaveKeyHasSave, 1);
            PlayerPrefs.SetInt(SaveKeyFloor, currentFloorIndex);
            PlayerPrefs.SetFloat(SaveKeyOil, lampController != null ? Mathf.Clamp(Mathf.Max(lampController.CurrentOil, 35f), 0f, 100f) : 75f);
            PlayerPrefs.SetFloat(SaveKeyTime, totalPlayTime);
            PlayerPrefs.SetInt(SaveKeyGender, (int)selectedGender);
            PlayerPrefs.SetString(SaveKeyNotes, inventoryManager != null ? inventoryManager.SerializeNotesToJson() : "");
            PlayerPrefs.Save();

            playerController?.SetInputEnabled(false);
            lampController?.SetDraining(false);
            uiManager?.SetOptionsModalVisible(false);

            EnterStartScreen();
        }

        public void PlayAgain()
        {
            PlayerPrefs.SetInt(SaveKeyHasSave, 0);
            PlayerPrefs.Save();
            OnStartButtonClicked();
        }

        // =========================================================================
        // CAMERA TOGGLE, OPTIONS MENU, INVENTORY & PUZZLE MODALS
        // =========================================================================

        public void ToggleCameraMode()
        {
            if (currentState != GameState.Playing || cameraController == null) return;
            cameraController.ToggleViewMode();
            uiManager?.UpdateCameraModeButtonLabel(cameraController.CurrentMode);
        }

        public void ToggleFlameMode()
        {
            if (currentState != GameState.Playing || lampController == null) return;
            lampController.ToggleFlameMode();
            string msg = lampController.IsSpiritFlame
                ? "SPIRIT FLAME ACTIVE — REVEALING HIDDEN CLUES (1.5x DRAIN)"
                : "WARM FLAME RESTORED — NORMAL RADIUS & DRAIN";
            Color col = lampController.IsSpiritFlame
                ? new Color(0.36f, 0.94f, 1.0f, 1f)
                : new Color(1.0f, 0.84f, 0.40f, 1f);
            uiManager?.ShowNotification(msg, col, 2.0f);
        }

        public void ToggleLampPower()
        {
            if (currentState != GameState.Playing || lampController == null) return;
            lampController.ToggleLampOnOff();
            if (lampController.IsExtinguished)
            {
                uiManager?.ShowNotification("LAMP EXTINGUISHED — HIDDEN IN THE DARK (OIL PAUSED)", new Color(0.75f, 0.80f, 0.86f, 1f), 1.8f);
            }
            else
            {
                uiManager?.ShowNotification("LANTERN RELIT!", new Color(1.0f, 0.84f, 0.42f, 1f), 1.4f);
            }
        }

        public void OpenOptionsMenu()
        {
            if (currentState != GameState.Playing) return;
            AudioManager.Instance?.PlayPuzzleClick();

            currentState = GameState.ModalPaused;
            playerController?.SetInputEnabled(false);
            lampController?.SetDraining(false);
            uiManager?.SetOptionsModalVisible(true);
        }

        public void CloseOptionsMenu()
        {
            if (currentState != GameState.ModalPaused) return;
            AudioManager.Instance?.PlayPuzzleClick();

            uiManager?.SetOptionsModalVisible(false);
            ResumeFromModal();
        }

        public void OpenInventoryPanel()
        {
            if (currentState != GameState.Playing || inventoryManager == null) return;

            currentState = GameState.ModalPaused;
            playerController?.SetInputEnabled(false);
            lampController?.SetDraining(false);

            inventoryManager.OpenInventory(ResumeFromModal);
        }

        public void OpenStoryNote(string title, string body, bool saveToInventory = true)
        {
            if (currentState != GameState.Playing && currentState != GameState.ModalPaused) return;

            if (saveToInventory && inventoryManager != null)
            {
                int floorNum = ActiveFloor != null ? ActiveFloor.floorNumber : currentFloorIndex + 1;
                inventoryManager.AddNote(title, body, floorNum);
            }

            currentState = GameState.ModalPaused;
            playerController?.SetInputEnabled(false);
            lampController?.SetDraining(false);

            uiManager?.ShowStoryNote(title, body, ResumeFromModal);
        }

        public void OpenTileSortPuzzle(string title, Action onSolved)
        {
            if (currentState != GameState.Playing) return;
            if (uiManager == null || uiManager.tileSortPuzzleUI == null) return;

            currentState = GameState.ModalPaused;
            playerController?.SetInputEnabled(false);
            lampController?.SetDraining(false);

            uiManager.tileSortPuzzleUI.Open(title, onSolved, ResumeFromModal);
        }

        public void OpenKeypadPuzzle(string title, string subtitle, string expectedCode, Action onSolved)
        {
            if (currentState != GameState.Playing) return;
            if (uiManager == null || uiManager.keypadPuzzleUI == null) return;

            currentState = GameState.ModalPaused;
            playerController?.SetInputEnabled(false);
            lampController?.SetDraining(false);

            uiManager.keypadPuzzleUI.Open(title, subtitle, expectedCode, onSolved, ResumeFromModal);
        }

        public void OpenSymbolSequencePuzzle(string title, string subtitle, GothicSymbol[] expectedSequence, Action onSolved)
        {
            if (currentState != GameState.Playing) return;
            if (uiManager == null || uiManager.symbolSequencePuzzleUI == null) return;

            currentState = GameState.ModalPaused;
            playerController?.SetInputEnabled(false);
            lampController?.SetDraining(false);

            uiManager.symbolSequencePuzzleUI.Open(title, subtitle, expectedSequence, onSolved, ResumeFromModal);
        }

        private void ResumeFromModal()
        {
            if (currentState != GameState.ModalPaused) return;

            Time.timeScale = 1f;
            currentState = GameState.Playing;
            playerController?.SetInputEnabled(true);
            lampController?.SetPaused(false);
            lampController?.SetGameActive(true);
            RefreshObjectiveHUD();
        }

        // =========================================================================
        // STAIRCASE LEVEL TRANSITIONS
        // =========================================================================

        public void OnFloorCompleted()
        {
            if (currentState != GameState.Playing) return;

            if (currentFloorIndex + 1 < floors.Length)
            {
                StartCoroutine(StaircaseTransitionRoutine(currentFloorIndex + 1));
            }
            else
            {
                StartCoroutine(FinalVictoryTransitionRoutine());
            }
        }

        private IEnumerator StaircaseTransitionRoutine(int nextFloorIndex)
        {
            currentState = GameState.FloorTransition;
            playerController?.SetInputEnabled(false);
            lampController?.SetDraining(false);

            FloorController nextFloor = floors[nextFloorIndex];
            string bannerTitle = nextFloor != null
                ? $"FLOOR {nextFloor.floorNumber} — {nextFloor.floorSubtitle.ToUpperInvariant()}"
                : $"FLOOR {nextFloorIndex + 1}";
            string bannerSub = nextFloorIndex == 1
                ? "Descending into the Shifting Hall of the Forgotten Haveli..."
                : "The Abyssal Sanctum awaits below...";

            if (uiManager != null)
            {
                uiManager.SetLevelBannerText(bannerTitle, bannerSub);
                yield return uiManager.FadeScreen(1f, 0.55f);
            }

            float carriedOil = lampController != null ? Mathf.Clamp(lampController.CurrentOil, 0f, 100f) : 75f;
            currentFloorIndex = nextFloorIndex;
            oilAtFloorStart = carriedOil;

            // Randomize the new floor's puzzle fresh on entry!
            ActivateOnlyFloor(currentFloorIndex, showNightstandLampForIntro: false, randomizePuzzles: true);
            BeginGameplayOnCurrentFloor(resetOilAmount: carriedOil, teleportToSpawn: true);

            currentState = GameState.FloorTransition;
            playerController?.SetInputEnabled(false);
            lampController?.SetDraining(false);

            yield return new WaitForSeconds(1.25f);

            if (uiManager != null)
            {
                yield return uiManager.FadeScreen(0f, 0.55f);
                uiManager.SetLevelBannerText(string.Empty, string.Empty);
            }

            currentState = GameState.Playing;
            Time.timeScale = 1f;
            playerController?.SetInputEnabled(true);
            lampController?.SetGameActive(true);
        }

        private IEnumerator FinalVictoryTransitionRoutine()
        {
            currentState = GameState.FloorTransition;
            playerController?.SetInputEnabled(false);
            lampController?.SetDraining(false);

            if (uiManager != null)
            {
                uiManager.SetLevelBannerText("ESCAPED THE FORGOTTEN HAVELI", "Dawn breaks beyond the forest trees...");
                yield return uiManager.FadeScreen(1f, 0.6f);
                yield return new WaitForSeconds(0.95f);
                uiManager.SetLevelBannerText(string.Empty, string.Empty);
            }

            PlayerPrefs.SetInt(SaveKeyHasSave, 0);
            PlayerPrefs.Save();

            TriggerVictory();

            if (uiManager != null)
            {
                yield return uiManager.FadeScreen(0f, 0.45f);
            }
        }

        private void BeginGameplayOnCurrentFloor(float resetOilAmount, bool teleportToSpawn)
        {
            FloorController floor = ActiveFloor;
            if (floor == null) return;

            Time.timeScale = 1f;
            currentState = GameState.Playing;

            if (playerController != null)
            {
                playerController.SetIntroPhysicsMode(false);
                if (teleportToSpawn && floor.playerSpawnPoint != null)
                {
                    playerController.TeleportUpright(floor.playerSpawnPoint.position, facingYaw: 0f);
                }
                playerController.SetInputEnabled(true);
            }

            if (lampController != null)
            {
                lampController.ResetLampModesForFloor();
                lampController.SetFloorLightProfile(currentFloorIndex);
                lampController.SetOil(Mathf.Clamp(resetOilAmount, 0f, 100f), snapLight: true);
                lampController.SetGameActive(true);
            }

            threatManager?.ResetPatrolState();
            threatManager?.RefreshSpiritCluesForActiveFloor();

            if (cameraController != null)
            {
                cameraController.SetCutsceneOverride(false);
                cameraController.SnapToTarget();
                uiManager?.UpdateCameraModeButtonLabel(cameraController.CurrentMode);
            }

            if (uiManager != null)
            {
                uiManager.ShowHUD();
                uiManager.UpdateFloorInfo(floor.floorNumber, floor.floorSubtitle, floor.GetObjectiveStatusText());
                if (lampController != null)
                {
                    uiManager.UpdateOilUI(lampController.CurrentOil, 100f);
                }
            }
        }

        private void ActivateOnlyFloor(int index, bool showNightstandLampForIntro, bool randomizePuzzles)
        {
            if (floors == null) return;
            for (int i = 0; i < floors.Length; i++)
            {
                if (floors[i] == null) continue;
                bool active = (i == index);
                floors[i].gameObject.SetActive(active);
                if (active)
                {
                    floors[i].ResetFloor(showNightstandLampForIntro && i == 0, randomizePuzzles);
                }
            }
        }

        private void HandleOilChanged(float current, float max) => uiManager?.UpdateOilUI(current, max);

        private void HandleOilDepleted()
        {
            if (currentState != GameState.Playing) return;

            currentState = GameState.GameOver;
            playerController?.SetInputEnabled(false);
            lampController?.SetDraining(false);
            AudioManager.Instance?.PlayGameOver();

            FloorController floor = ActiveFloor;
            int fNum = floor != null ? floor.floorNumber : currentFloorIndex + 1;
            string fSub = floor != null ? floor.floorSubtitle : "Darkness";
            uiManager?.ShowGameOver(fNum, fSub);
        }

        private void TriggerVictory()
        {
            currentState = GameState.Victory;
            playerController?.SetInputEnabled(false);
            lampController?.SetDraining(false);
            AudioManager.Instance?.PlayVictory();

            float oilLeft = lampController != null ? lampController.CurrentOil : 0f;
            uiManager?.ShowVictory(totalPlayTime, oilLeft, 100f);
        }

        public void NotifyPickup(string message, Color color) => uiManager?.ShowNotification(message, color);

        public void RefreshObjectiveHUD()
        {
            if (ActiveFloor != null && uiManager != null)
            {
                uiManager.UpdateObjectiveOnly(ActiveFloor.GetObjectiveStatusText());
            }
        }

        public void NotifyObjectiveUpdated(string toastMessage, Color color)
        {
            if (ActiveFloor != null && uiManager != null)
            {
                uiManager.UpdateObjectiveOnly(ActiveFloor.GetObjectiveStatusText());
                uiManager.ShowNotification(toastMessage, color, 2.6f);
            }
        }

        public void OnFakeKeyTriggered(float oilDrained)
        {
            cameraController?.Shake(0.45f, 0.35f);
            uiManager?.ShowNotification($"CURSED TRINKET!  -{Mathf.RoundToInt(oilDrained)}% OIL", new Color(1f, 0.28f, 0.22f, 1f), 2.4f);
        }
    }
}
