using System.Collections.Generic;
using UnityEngine;

namespace LastLight
{
    public enum FloorPuzzleMode
    {
        TileSortChestForKey,    // Floor 1: 4 Puzzle Pieces -> Tile-Sort Chest -> Exit Key -> Exit Door
        RandomizedMathKeypad,   // Floor 2: Brand-new random 3-digit code + dynamic math riddles in 3 Notes -> Keypad
        RandomizedSymbolLock    // Floor 3: Brand-new random 4-step Gothic Symbol Sequence in 4 Clue Notes -> Ancient Stone Lock
    }

    /// <summary>
    /// Manages a single Haveli escape-room floor with full per-run puzzle randomization:
    /// - Floor 1: 4 Puzzle Pieces -> randomized Tile-Sort Treasure Chest -> Exit Key.
    /// - Floor 2: Brand-new random 3-digit code & dynamic math riddle Story Notes on every New Game / Retry.
    /// - Floor 3: Brand-new random 4-step Gothic Symbol Sequence (Skull, Moon, Key, Crown) injected into 4 Clue Notes.
    /// </summary>
    public class FloorController : MonoBehaviour
    {
        [Header("Floor Metadata")]
        public int floorNumber = 1;
        public string floorSubtitle = "The Forgotten Chamber";
        public FloorPuzzleMode puzzleMode = FloorPuzzleMode.TileSortChestForKey;
        public int requiredPuzzlePieces = 4;
        public int totalClueNotes = 3;

        [Header("Transforms")]
        public Transform playerSpawnPoint;
        public Transform introBedLyingPoint;
        public Transform introBedStepOffPoint;
        public GameObject nightstandLampObject;

        [Header("Floor Objects")]
        public ExitDoor exitDoor;
        public StaircaseTrigger staircaseTrigger;
        public TreasureChest treasureChest;
        public InteractableContainer[] containers;
        public InteractableContainer[] dynamicClueContainers; // 3 for Floor 2 Math Riddles, 4 for Floor 3 Symbol Plaques
        public SpiritClueMarker[] spiritClueMarkers;          // Hidden cyan wall arrows revealed by Blue Spirit Flame
        public PickupItem[] pickups;
        public DisorientationManager disorientationManager;

        private readonly HashSet<int> collectedPieceIds = new HashSet<int>();
        private readonly HashSet<string> discoveredNoteTitles = new HashSet<string>();
        private bool hasExitKey;

        public int CollectedPuzzlePieces => collectedPieceIds.Count;
        public bool HasAllPuzzlePieces => collectedPieceIds.Count >= requiredPuzzlePieces;
        public int DiscoveredNotesCount => discoveredNoteTitles.Count;
        public bool HasExitKey => hasExitKey;

        // Varied gothic math riddle templates per digit (1..9) so Floor 2 is fresh every single run!
        private static readonly string[][] DigitMathRiddles =
        {
            new[] { "Only a single shadow survives — nine chimes minus eight echoes." }, // unused 0 fallback
            new[]
            {
                "Nine chimes struck, yet eight fell silent. Only the remainder endures.",
                "Four locked gates divided by four — a solitary key remains."
            },
            new[]
            {
                "Seven tallow candles burned at dusk; five guttered out before midnight.",
                "Half of four portraits still watch from the damp corridor."
            },
            new[]
            {
                "Nine stone steps divided by three mark the hidden landing.",
                "Six shadows crept through the hall; three vanished into dust."
            },
            new[]
            {
                "Count half of eight reflections staring from the cracked mirror.",
                "Two iron hinges multiplied by two hold the cellar gate."
            },
            new[]
            {
                "Ten brass bells hang in the belfry; five were torn away.",
                "Half of ten ravens perched upon the rotting rafters."
            },
            new[]
            {
                "Two multiplied by three lanterns went dark in the eastern wing.",
                "Nine portraits lined the wall; three were slashed beyond recognition."
            },
            new[]
            {
                "Fourteen cold whispers divided by two echo down the stairwell.",
                "Four locked wardrobes plus three broken chests guard this hall."
            },
            new[]
            {
                "Two heavy chains multiplied by four bind the mansion doors.",
                "Twelve hours passed in blackness; four were lost to sleep."
            },
            new[]
            {
                "The Haveli's iron bells toll 3 × 3 before dead silence falls.",
                "Five cracked pillars plus four rotting arches frame the sanctum."
            }
        };

        public void ResetFloor(bool showNightstandLampForIntro, bool randomizePuzzles = true)
        {
            collectedPieceIds.Clear();
            discoveredNoteTitles.Clear();
            hasExitKey = false;

            if (nightstandLampObject != null)
            {
                nightstandLampObject.SetActive(showNightstandLampForIntro);
            }

            if (containers != null)
            {
                for (int i = 0; i < containers.Length; i++)
                {
                    if (containers[i] != null)
                    {
                        containers[i].ownerFloor = this;
                        containers[i].ResetState();
                    }
                }
            }

            if (pickups != null)
            {
                for (int i = 0; i < pickups.Length; i++)
                {
                    if (pickups[i] != null)
                    {
                        pickups[i].ownerFloor = this;
                        pickups[i].ResetState();
                    }
                }
            }

            if (treasureChest != null)
            {
                treasureChest.ownerFloor = this;
                treasureChest.ResetState();
            }

            if (exitDoor != null)
            {
                exitDoor.ownerFloor = this;
                exitDoor.ResetState();
            }

            if (staircaseTrigger != null)
            {
                staircaseTrigger.ownerFloor = this;
                staircaseTrigger.ResetState();
            }

            if (disorientationManager != null)
            {
                disorientationManager.ResetProps();
            }

            if (spiritClueMarkers != null)
            {
                for (int i = 0; i < spiritClueMarkers.Length; i++)
                {
                    if (spiritClueMarkers[i] != null) spiritClueMarkers[i].SetSpiritVisible(false);
                }
            }

            if (randomizePuzzles)
            {
                RandomizeFloorPuzzle();
            }

            MansionThreatManager.Instance?.AssignCursedDrawersForFloor(this);
        }

        /// <summary>
        /// Generates brand-new randomized puzzle solutions and injects matching clues into this floor's notes.
        /// </summary>
        public void RandomizeFloorPuzzle()
        {
            if (puzzleMode == FloorPuzzleMode.RandomizedMathKeypad)
            {
                int d1 = Random.Range(1, 10);
                int d2 = Random.Range(1, 10);
                int d3 = Random.Range(1, 10);
                string newCode = $"{d1}{d2}{d3}";

                if (exitDoor != null)
                {
                    exitDoor.lockType = DoorLockType.CodeLocked;
                    exitDoor.requiredCode = newCode;
                }

                int[] digits = { d1, d2, d3 };
                string[] ordinals = { "FIRST", "SECOND", "THIRD" };
                string[] romans = { "I", "II", "III" };

                if (dynamicClueContainers != null)
                {
                    for (int i = 0; i < Mathf.Min(3, dynamicClueContainers.Length); i++)
                    {
                        InteractableContainer c = dynamicClueContainers[i];
                        if (c == null) continue;
                        string[] pool = DigitMathRiddles[digits[i]];
                        string chosenRiddle = pool[Random.Range(0, pool.Length)];

                        c.noteTitle = $"MATH RIDDLE {romans[i]} — {ordinals[i]} DIGIT";
                        c.noteBody = $"\"{chosenRiddle}\"\n\n" +
                                     $"(Saved to INVENTORY. This reveals the {ordinals[i]} digit of Floor 2's Red Exit Door Keypad.)";
                    }
                }
            }
            else if (puzzleMode == FloorPuzzleMode.RandomizedSymbolLock)
            {
                GothicSymbol[] seq = { GothicSymbol.Skull, GothicSymbol.Moon, GothicSymbol.Key, GothicSymbol.Crown };
                // Fisher-Yates shuffle for a brand-new 4-step Gothic Symbol Sequence every run/retry
                for (int i = seq.Length - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    GothicSymbol tmp = seq[i];
                    seq[i] = seq[j];
                    seq[j] = tmp;
                }

                if (exitDoor != null)
                {
                    exitDoor.lockType = DoorLockType.SymbolSequenceLocked;
                    exitDoor.requiredSymbolSequence = seq;
                }

                string[] stepNames = { "FIRST (STEP 1)", "SECOND (STEP 2)", "THIRD (STEP 3)", "FOURTH (STEP 4)" };
                string[] romans = { "I", "II", "III", "IV" };

                if (dynamicClueContainers != null)
                {
                    for (int i = 0; i < Mathf.Min(4, dynamicClueContainers.Length); i++)
                    {
                        InteractableContainer c = dynamicClueContainers[i];
                        if (c == null) continue;
                        string symName = SymbolSequencePuzzleUI.GetSymbolDisplayName(seq[i]);

                        c.noteTitle = $"SANCTUM PLAQUE {romans[i]} — {stepNames[i]}";
                        c.noteBody = $"An ancient gothic plaque is etched in dried wax:\n\n" +
                                     $"\"{stepNames[i]}:  Invoke the  {symName}  upon the Ancient Stone Lock.\"\n\n" +
                                     $"(Saved to INVENTORY. Find all 4 plaques and enter the exact symbol order at the Red Exit Gate.)";
                    }
                }
            }
        }

        public string GetObjectiveStatusText()
        {
            if (exitDoor != null && exitDoor.IsUnlocked)
            {
                return floorNumber >= 3
                    ? "GATE OPEN — DESCEND THE FINAL STAIRS TO ESCAPE!"
                    : "DOOR OPEN — DESCEND THE STAIRCASE";
            }

            switch (puzzleMode)
            {
                case FloorPuzzleMode.TileSortChestForKey:
                    if (hasExitKey) return "EXIT KEY FOUND — UNLOCK THE RED EXIT DOOR";
                    if (HasAllPuzzlePieces) return "ALL 4 PIECES FOUND — SOLVE THE TREASURE CHEST";
                    return $"FIND PUZZLE PIECES IN DRAWERS: {CollectedPuzzlePieces} / {requiredPuzzlePieces}";

                case FloorPuzzleMode.RandomizedMathKeypad:
                    if (DiscoveredNotesCount >= totalClueNotes)
                        return "ALL 3 RIDDLES FOUND — ENTER CODE AT RED EXIT DOOR";
                    return $"FIND MATH RIDDLE NOTES: {DiscoveredNotesCount} / {totalClueNotes}";

                case FloorPuzzleMode.RandomizedSymbolLock:
                    if (DiscoveredNotesCount >= totalClueNotes)
                        return "ALL 4 PLAQUES FOUND — ENTER SYMBOLS AT ANCIENT GATE";
                    return $"FIND GOTHIC SYMBOL PLAQUES: {DiscoveredNotesCount} / {totalClueNotes}";
            }

            return "SEARCH THE HAVELI";
        }

        public void OnPuzzlePieceCollected(int pieceNumber)
        {
            collectedPieceIds.Add(pieceNumber);
            string toast = HasAllPuzzlePieces
                ? $"PUZZLE PIECE #{pieceNumber} ({CollectedPuzzlePieces}/{requiredPuzzlePieces}) — SOLVE THE CHEST!"
                : $"PUZZLE PIECE #{pieceNumber} FOUND ({CollectedPuzzlePieces}/{requiredPuzzlePieces})";

            GameManager.Instance?.NotifyObjectiveUpdated(toast, new Color(1f, 0.86f, 0.36f, 1f));
        }

        public void OnStoryNoteDiscovered(string title)
        {
            if (!string.IsNullOrEmpty(title)) discoveredNoteTitles.Add(title);
            GameManager.Instance?.RefreshObjectiveHUD();
        }

        public void OnChestExitKeyAwarded()
        {
            hasExitKey = true;
            GameManager.Instance?.NotifyObjectiveUpdated("CHEST OPENED — EXIT KEY ACQUIRED!", new Color(1f, 0.88f, 0.36f, 1f));
        }

        public void OnChestCodeTabletAwarded()
        {
            GameManager.Instance?.RefreshObjectiveHUD();
        }

        public void OnExitDoorUnlocked()
        {
            GameManager.Instance?.NotifyObjectiveUpdated("DOOR SWUNG OPEN — WALK DOWN THE STAIRS!", new Color(1f, 0.88f, 0.40f, 1f));
        }

        public void OnLockedDoorTouched()
        {
            string msg;
            if (puzzleMode == FloorPuzzleMode.TileSortChestForKey)
                msg = "LOCKED — SOLVE THE TREASURE CHEST FOR THE EXIT KEY";
            else if (puzzleMode == FloorPuzzleMode.RandomizedMathKeypad)
                msg = "LOCKED — ENTER THE 3-DIGIT MATH CODE FROM YOUR NOTES";
            else
                msg = "LOCKED — ENTER THE 4-SYMBOL SEQUENCE FROM THE SANCTUM PLAQUES";

            GameManager.Instance?.NotifyPickup(msg, new Color(1f, 0.45f, 0.38f, 1f));
        }

        public void OnStaircaseDescended()
        {
            GameManager.Instance?.OnFloorCompleted();
        }
    }
}
