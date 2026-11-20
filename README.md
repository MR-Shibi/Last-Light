# Last Light

> *"Find your way... or don't."*

**Category:** Mobile Game Development (Android APK)  
**Event:** ACM NUCES Karachi × TEKNOFEST Pakistan — Coder's Cup '26 / Game Jam 2026  
**Theme:** LOST  
**Engine:** Unity 6 (`6000.6.9f1`) · Universal Render Pipeline (URP) · New Input System  

---

## Team Information

- **Team Name:** `Accidental_Game_Devs`
- **Team Members:**
  - `Shoaib Hayat`
  - `Huzaifa Ahmed Bari`
  - `Uzair Ibrahim`
  - `Danish Ahmed`

---

## Game Overview & Storyline

**Last Light** is a stylized low-poly 3D mobile horror escape game set inside a pitch-black decaying mansion (*The Forgotten Haveli*).

After choosing your survivor (**Male** or **Female**) on a dark-gothic character select screen with live rotating 3D previews, a **2-act animated opening cutscene** plays:
1. **Act 1 — Jungle Campfire Ambush:** While resting beside a flickering campfire in a pine forest clearing, two masked bandits (*Daku*) emerge from the trees and ambush you.
2. **Act 2 — The Forgotten Haveli Wake-Up:** You awaken on a carved four-poster bed in complete darkness, step onto the floorboards, and pick up an antique brass oil lantern from the bedside table.

Armed only with your fading lantern, you must search furniture across **3 distinct mansion floors**, solve **randomized escape puzzles**, switch to the **Blue Spirit Flame** to uncover hidden clues, survive **Cursed Spider Drawers**, extinguish your lamp to hide from **Bandit Patrols**, and descend the staircases before your last light dies out.

---

## Theme Interpretation — "LOST"

- **Pure Darkness & Shrinking Sight:** Ambient light and directional lights are completely disabled (`Color.black`, intensity `0`). Your handheld antique lantern and a faint red glow above each floor's exit door are the only light sources inside the mansion. When oil hits `0%`, darkness swallows you completely (*"You are lost."*).
- **Disorienting Shifting Halls:** Decorative mansion props subtly shift behind you outside your lantern's light radius (`DisorientationManager`), and every floor's puzzle solutions randomize fresh on every New Game or Retry.
- **Hide in the Dark:** Every 35 seconds, approaching bandit footsteps force you to choose between extinguishing your own light—going completely blind in the dark—or risking being spotted and losing precious oil.

---

## Final Version Features

### Core Survival & Lamp Mechanics (`LampController.cs`)
- **Linear 4-Minute Oil Drain:** A full `100%` lantern drains linearly to `0%` over exactly **240 seconds** (`100 / 240 ≈ 0.4167%/sec`).
- **Dynamic Radius & Strict 100% Cap:** Lantern `PointLight.range` shrinks linearly from `6.5` units (at `100%`) down to `1.5` units (at `0%`). Oil flasks restore `+20%` and are strictly capped at `100%`.
- **Blue Spirit Flame Toggle (`FLAME: WARM / SPIRIT`):**
  - **Warm Mode (Default):** Warm amber light (`6.5` max range) and standard linear drain.
  - **Spirit Mode:** Cool cyan/blue light (`4.0` tighter range, `1.5x` oil drain) that reveals **glowing cyan wall clues/arrows** pointing toward uncollected puzzle containers and **faint red `X` marks** on empty or cursed drawers.
- **Quick Extinguish (`LAMP: ON / OFF`):** Instantly turns off the lantern to hide from patrolling bandits and pauses passive oil drain while extinguished.

### High-Tension Horror Twists (`MansionThreatManager.cs`)
- **Bandit Patrol ("Hide in the Dark"):** Every **35 seconds** during active gameplay, a **5-second** warning triggers with accelerating heavy footsteps SFX, a pulsing crimson screen vignette, and the alert *"EXTINGUISH LAMP! HE SEES THE LIGHT!"*. Turning the lamp OFF before the 5 seconds finish keeps you hidden safely; leaving it ON triggers a sharp ambush stinger, camera shake, and `-15%` oil.
- **Cursed Spider Drawers:** On every floor reset, **2 random non-essential drawers** are marked **Cursed**. Opening one unleashes a leaping red-eyed spider that drains `10%` oil unless you tap **CLOSE** before the drawer's `2.5s` auto-close finishes.

### Exploration, Furniture & Atmosphere
- **Auto-Closing Interactive Containers (`InteractableContainer.cs`):** Five distinct antique furniture styles (**Tall Wardrobe**, **3-Drawer Chest**, **Bedside Nightstand**, **Writing Desk**, and **Apothecary Chest**) smoothly slide or swing open, stay open for **2.5 seconds** (or close immediately on manual **CLOSE**), and automatically animate shut.
- **Horror Mansion Atmosphere (`AmbientCritters.cs`):** Animated low-poly **scurrying floor rats** (with proximity squeak SFX within `4.5` units), translucent **cobwebs**, hanging 8-legged **spiders with unlit red eyes** (zero scene light emitted), unlit multi-tiered **wrought-iron chandeliers**, ceiling timber beams, wall wainscoting, broken chairs, and varied **framed gothic oil paintings**.
- **First-Person & Over-the-Shoulder Camera (`CameraController.cs`):** Defaults to immersive **First-Person** with a swaying handheld antique brass lantern in the lower-right, with an instant **`1P / 3P`** HUD toggle to a wall-collision-safe **Over-the-Shoulder Third-Person** view.
- **Themed Gothic UI (`UIStyleBuilder.cs`):** Ornate oil vial gauge, **broken weathered wood plank** `OPEN / CLOSE` button, **crumpled aged parchment** Story Note modal & scrollable **Notes Inventory**, and an Options gear menu (`Resume`, `Restart Level`, `Save & Exit` with `CONTINUE` persistence).
- **Procedural Horror Audio (`AudioManager.cs`):** Continuous eerie mansion wind/drone ambience, campfire crackle, ambush stinger, approaching heavy bandit footsteps, rat squeaks, drawer creaks, puzzle clicks, low-oil heartbeat (`< 25%`), and custom `AudioClip` override slots.
- **Auto Character Asset Loader (`CharacterBuilder.cs`):** Automatically detects and rigs imported `.fbx`/`.glb`/`.obj`/`.prefab` humanoid models placed in `Assets/Models/`, parenting the antique brass lantern directly to `HumanBodyBones.RightHand`, with stylized faceted low-poly fallbacks built in.

---

## Floors & Randomized Puzzles (`FloorController.cs`)

| Floor | Name | Escape Objective |
| :--- | :--- | :--- |
| **Floor 1** | *The Forgotten Chamber* | Find **4 scattered Puzzle Pieces** inside wardrobes, chests, and desks, then solve the **Tile-Sort Treasure Chest** (`1 → 4`) to claim the Exit Key. |
| **Floor 2** | *The Shifting Hall* | Locate **3 randomized Math Riddle notes** (saved to your Inventory) to deduce a freshly generated 3-digit code for the **Exit Keypad**. |
| **Floor 3** | *The Abyssal Sanctum* | Uncover **4 randomized Gothic Symbol Plaques** (*Skull, Moon, Key, Crown*) and enter their exact order into the **Ancient Stone Lock** to escape the Haveli. |

---

## Controls

| Action | Mobile Touch HUD | Desktop / Unity Editor |
| :--- | :--- | :--- |
| **Move** | Virtual Joystick (bottom-left) | `W A S D` / Arrow Keys |
| **Look Around** | Drag right side of screen | Mouse Drag / Arrow Keys |
| **Open / Close Furniture & Interact** | Broken Plank `OPEN / CLOSE` button or tap object | `E` / `Space` / Click |
| **Toggle Lamp ON / OFF** | `LAMP: ON / OFF` button (bottom-right) | `F` |
| **Toggle Warm / Spirit Flame** | `FLAME: WARM / SPIRIT` button (bottom-right) | `Q` |
| **Switch Camera View** | `1P / 3P` button (top-right) | Click HUD button |
| **Read Clues & Notes** | `INVENTORY` button (top-right) | Click HUD button |
| **Pause / Restart / Save & Exit** | Gear `⚙` Options button (top-right) | Click HUD button |

---

## One-Click Setup & Android APK Build (`AutoSceneBuilder.cs`)

Zero manual scene wiring or drag-and-drop is required:

1. Open the project in **Unity `6000.6.9f1`** (with Android Build Support installed for APK export).
2. In the Unity top menu bar, click **`GameJam > Build Last Light`** to procedurally generate all meshes, materials, UI sprites, critters, lighting, UI canvases, and `Assets/Scenes/LastLight.unity`.
3. Press **Play** to test immediately in the Editor.
4. Click **`GameJam > Build Android APK`** to export the landscape Android build directly to `Builds/LastLight.apk`.

---

## Key Project Structure

```text
Assets/
├── Editor/
│   ├── AutoSceneBuilder.cs       # Zero-drag-drop scene builder & one-click Android APK exporter
│   └── UIStyleBuilder.cs         # Procedural gothic frames, broken plank, crumpled parchment, paintings & vignette
├── Scenes/
│   └── LastLight.unity           # Auto-generated main playable scene
└── Scripts/
    ├── GameManager.cs            # Top-level state machine, Save/Continue, floor progression & modals
    ├── LampController.cs         # 4-minute linear oil drain, 100% cap, Warm/Spirit flame & Lamp ON/OFF
    ├── MansionThreatManager.cs   # 35s Bandit Patrol events, Cursed Drawers & Spirit Flame clue visibility
    ├── InteractableContainer.cs  # 2.5s auto-closing furniture, rewards, spider traps & spirit markers
    ├── CutsceneManager.cs        # 2-act Jungle Campfire Ambush + Old Haveli Bed Wake-Up movie
    ├── FloorController.cs        # Per-floor randomized puzzles & objective tracking
    ├── CameraController.cs       # First-Person default + wall-safe Over-the-Shoulder 3P toggle & shake
    ├── PlayerController.cs       # Mobile joystick + keyboard/mouse input & proximity interaction
    ├── CharacterBuilder.cs       # Procedural low-poly meshes + imported humanoid right-hand lantern binder
    ├── AmbientCritters.cs        # Scurrying rats (proximity squeak) & hanging red-eyed spiders
    ├── DisorientationManager.cs  # Shifts decorative props outside lantern radius
    ├── AudioManager.cs           # Procedural horror ambience, footsteps, stingers, heartbeat & SFX
    ├── InventoryManager.cs       # Scrollable crumpled-paper clues/notes inventory
    ├── UIManager.cs              # HUD, gauges, modals, vignettes & screen transitions
    └── TileSortPuzzleUI.cs / KeypadPuzzleUI.cs / SymbolSequencePuzzleUI.cs
```

---

## AI-Use Declaration

In accordance with competition guidelines, AI development tools were used strictly as coding assistants during the jam:
- **Antigravity CLI (`agy`) & Google Gemini:** Assisted with C# gameplay scripting, procedural geometry/texture/audio generation, debugging, and Unity Editor build automation (`AutoSceneBuilder.cs`).
- **Original Team Work:** Game design, storyline direction, horror atmosphere & mechanics tuning, puzzle progression, testing, and Android APK packaging were conceived, directed, and validated by **`Accidental_Game_Devs`**.
