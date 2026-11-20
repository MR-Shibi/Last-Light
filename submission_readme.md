# Last Light

**Category:** Mobile Game Development (Android APK)  
**Event:** ACM NUCES Karachi × TEKNOFEST Pakistan — Coder's Cup '26 / Game Jam 2026  
**Theme:** LOST  

---

## Team Information
* **Team Name:** `Accidental_Game_Devs`
* **Team Members:**
  * `Shoaib Hayat`
  * `Huzaifa Ahmed Bari`
  * `Uzair Ibrahim`
  * `Danish Ahmed`

---

## Game Overview & Storyline
While camping deep in the jungle, you are ambushed and kidnapped by bandits (*Daku*) and locked inside a pitch-black, decaying old mansion (*The Forgotten Haveli*). Armed only with an antique oil lantern whose flame steadily fades over 4 minutes, you must search drawers and cupboards, uncover scattered notes, solve randomized escape puzzles across multiple floors, hide in the dark when bandits patrol nearby, and descend the staircases to escape before your last light dies out.

---

## Theme Interpretation ("LOST")
* **Physical & Sensory Disorientation:** The mansion has zero ambient light—your only source of sight is a shrinking circle of lantern light. When your oil hits `0%`, darkness swallows you completely (*"You are lost."*).
* **Shifting Environment & Unpredictability:** Distant mansion props subtly rearrange behind you in the dark, and puzzle codes/sequences randomize on every retry or new run so you can never rely purely on memory.
* **Risk vs. Reward Survival:** Switching to the **Blue Spirit Flame** reveals hidden clues in the dark at the cost of faster oil burn, while hearing approaching bandit footsteps forces you to extinguish your own lamp and wait blind in the dark until danger passes.

---

## How to Play & Mobile Controls
* **Move:** Touch & drag the **Virtual Joystick** (bottom-left) *(WASD / Arrow keys in Unity Editor)*.
* **Look / Camera View:** Tap the **`1P / 3P`** HUD button to switch between **First-Person** and **Over-the-Shoulder Third-Person** views.
* **Open / Close Containers:** Walk near cupboards, wardrobes, or drawers and tap the wooden **`OPEN` / `CLOSE`** button to inspect them for oil flasks, puzzle pieces, or story notes *(drawers automatically close after 2.5 seconds; watch out for cursed spider drawers!)*.
* **Lamp On / Off:** Tap **`LAMP ON/OFF`** to extinguish your lantern and hide when a Bandit Patrol warning appears.
* **Spirit Flame Toggle:** Tap **`FLAME: WARM / SPIRIT`** to reveal hidden glowing wall clues.
* **Notes Inventory:** Tap **`INVENTORY`** to re-read collected crumpled-paper clues and riddles anytime.
* **Options (`⚙`):** Pause gameplay, **Restart Level**, or **Save & Exit** from the top-right menu.

---

## Puzzles & Progression
1. **Floor 1 — Tile-Sort Treasure Lock:** Search cupboards and drawers to gather 4 scattered puzzle pieces and arrange them in order at the treasure chest to claim the exit key.
2. **Floor 2 — Randomized Math Riddle Lock:** Collect 3 hidden story notes containing randomized math riddles and enter the resulting 3-digit code on the exit keypad.
3. **Floor 3 — Randomized Gothic Symbol Lock:** Uncover the randomized 4-symbol sequence hidden across the final hall and input it on the ancient gate lock to escape the mansion.

---

## Asset Credits & Third-Party Resources
* **Game Engine:** Unity 3D (Android Build Support)
* **Fonts & UI:** Unity UI / TextMeshPro & custom procedural gothic/parchment UI graphics

---

## AI-Use Declaration
In accordance with competition rules, AI tools were used strictly as development assistants during the competition session:
* **Antigravity CLI (`agy`) & Google Gemini:** Assisted with generating and debugging C# gameplay scripts (`LampController`, `InteractableContainer`, `CutsceneManager`, `MansionThreatManager`), procedural audio synthesis, UI layout scripting, and the `AutoSceneBuilder` Unity Editor automation tool.
* **Original Team Work:** All game concept design, storyline adaptation, puzzle structure, level pacing, visual direction, asset integration, testing, and Android APK packaging were designed, directed, and assembled by our team during the event.
