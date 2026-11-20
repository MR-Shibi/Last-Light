using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LastLight
{
    [Serializable]
    public class NoteEntry
    {
        public string title;
        public string body;
        public int floorNumber;
    }

    [Serializable]
    internal class NoteSaveWrapper
    {
        public List<NoteEntry> notes = new List<NoteEntry>();
    }

    /// <summary>
    /// Stores all collected Story/Puzzle Notes, manages the scrollable dark-themed
    /// Inventory UI panel, and serializes collected notes to PlayerPrefs for Save & Exit.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Dark Inventory Panel UI (Auto-Wired)")]
        public GameObject inventoryPanelRoot;
        public RectTransform listContentRoot;
        public Text selectedNoteTitleText;
        public Text selectedNoteBodyText;
        public Text emptyStateText;
        public Text noteCountBadgeText;
        public Button closeButton;

        [Header("Item Button Style")]
        public Sprite buttonSprite;
        public Font uiFont;

        private readonly List<NoteEntry> collectedNotes = new List<NoteEntry>();
        private readonly List<GameObject> spawnedButtons = new List<GameObject>();
        private Action onClosedCallback;

        public IReadOnlyList<NoteEntry> CollectedNotes => collectedNotes;
        public int NoteCount => collectedNotes.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(CloseInventory);
                closeButton.onClick.AddListener(CloseInventory);
            }
            UpdateBadge();
        }

        public bool AddNote(string title, string body, int floorNumber)
        {
            if (string.IsNullOrEmpty(title)) return false;

            for (int i = 0; i < collectedNotes.Count; i++)
            {
                if (collectedNotes[i].title == title)
                {
                    return false; // Already collected
                }
            }

            collectedNotes.Add(new NoteEntry
            {
                title = title,
                body = body,
                floorNumber = floorNumber
            });
            UpdateBadge();
            return true;
        }

        public void ClearAllNotes()
        {
            collectedNotes.Clear();
            UpdateBadge();
        }

        public void ClearNotesForFloor(int floorNumber)
        {
            collectedNotes.RemoveAll(n => n.floorNumber == floorNumber);
            UpdateBadge();
        }

        public void OpenInventory(Action onClosed)
        {
            onClosedCallback = onClosed;
            RebuildListUI();

            if (inventoryPanelRoot != null)
            {
                inventoryPanelRoot.SetActive(true);
            }
            AudioManager.Instance?.PlayNoteOpen();
        }

        public void CloseInventory()
        {
            if (inventoryPanelRoot != null)
            {
                inventoryPanelRoot.SetActive(false);
            }
            AudioManager.Instance?.PlayPuzzleClick();

            Action cb = onClosedCallback;
            onClosedCallback = null;
            cb?.Invoke();
        }

        public string SerializeNotesToJson()
        {
            NoteSaveWrapper wrapper = new NoteSaveWrapper { notes = new List<NoteEntry>(collectedNotes) };
            return JsonUtility.ToJson(wrapper);
        }

        public void DeserializeNotesFromJson(string json)
        {
            collectedNotes.Clear();
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    NoteSaveWrapper wrapper = JsonUtility.FromJson<NoteSaveWrapper>(json);
                    if (wrapper != null && wrapper.notes != null)
                    {
                        collectedNotes.AddRange(wrapper.notes);
                    }
                }
                catch
                {
                    // Ignore malformed saved JSON
                }
            }
            UpdateBadge();
        }

        private void RebuildListUI()
        {
            for (int i = 0; i < spawnedButtons.Count; i++)
            {
                if (spawnedButtons[i] != null) Destroy(spawnedButtons[i]);
            }
            spawnedButtons.Clear();

            bool hasNotes = collectedNotes.Count > 0;
            if (emptyStateText != null) emptyStateText.gameObject.SetActive(!hasNotes);

            if (!hasNotes)
            {
                if (selectedNoteTitleText != null) selectedNoteTitleText.text = "NO NOTES COLLECTED";
                if (selectedNoteBodyText != null) selectedNoteBodyText.text = "Search drawers, wardrobes, nightstands, and writing desks to uncover tower lore and puzzle clues.";
                return;
            }

            if (listContentRoot != null)
            {
                for (int i = 0; i < collectedNotes.Count; i++)
                {
                    int idx = i;
                    NoteEntry entry = collectedNotes[i];
                    GameObject itemObj = CreateNoteListButton(entry, idx);
                    spawnedButtons.Add(itemObj);
                }
            }

            // Show most recently collected note by default
            SelectNote(collectedNotes.Count - 1);
        }

        private GameObject CreateNoteListButton(NoteEntry entry, int index)
        {
            GameObject go = new GameObject($"NoteItem_{index}", typeof(RectTransform));
            go.transform.SetParent(listContentRoot, false);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 82f;
            le.minHeight = 82f;

            Image bg = go.AddComponent<Image>();
            bg.sprite = buttonSprite;
            bg.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            bg.color = new Color(0.14f, 0.14f, 0.16f, 0.96f);

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.18f, 1.14f, 1.05f, 1f);
            cb.pressedColor = new Color(0.85f, 0.78f, 0.60f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(() => SelectNote(index));

            GameObject txtObj = new GameObject("Label", typeof(RectTransform));
            txtObj.transform.SetParent(go.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(18f, 6f);
            trt.offsetMax = new Vector2(-18f, -6f);

            Text txt = txtObj.AddComponent<Text>();
            txt.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = $"[Floor {entry.floorNumber}]  {entry.title}";
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.color = new Color(0.95f, 0.85f, 0.56f, 1f);
            txt.alignment = TextAnchor.MiddleLeft;
            txt.raycastTarget = false;

            return go;
        }

        private void SelectNote(int index)
        {
            if (index < 0 || index >= collectedNotes.Count) return;
            AudioManager.Instance?.PlayPuzzleClick();

            NoteEntry entry = collectedNotes[index];
            if (selectedNoteTitleText != null)
            {
                selectedNoteTitleText.text = $"{entry.title}   (FLOOR {entry.floorNumber})";
            }
            if (selectedNoteBodyText != null)
            {
                selectedNoteBodyText.text = entry.body;
            }
        }

        private void UpdateBadge()
        {
            if (noteCountBadgeText != null)
            {
                noteCountBadgeText.text = $"INVENTORY ({collectedNotes.Count})";
            }
        }
    }
}
