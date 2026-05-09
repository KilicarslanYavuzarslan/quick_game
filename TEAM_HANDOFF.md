# Stellar Sweep — Team Handoff
*Art & Audio teammates: everything you need to slot in your assets.*
*AI agents: this is the full project context — read this before touching any file.*

---

## What the game is

A minesweeper-style puzzle game with a compass theme. The player is shown a hidden 8×8 grid and must find and collect stars belonging to two constellations (Orion and Lyra) in the correct order. A compass needle always points at the next target star. The player gets 2 wrong-click allowances before losing.

**Win:** Collect all stars from both constellations in order.
**Lose:** Make 3 wrong clicks total (across any constellation, in any order).

---

## Project specs

- **Engine:** Unity 6.3, URP 2D
- **Canvas:** Screen Space – Overlay (no camera reference needed for UI)
- **Resolution:** 1920×1080 (set Game View to this)
- **Grid:** 8 columns × 8 rows, spawned at runtime from a prefab
- **Constellations:** Orion (5 stars), Lyra (5 stars) — positions set in ScriptableObject assets

---

## Folder structure

```
Assets/
├── Scripts/
│   ├── Core/         GameManager, ScoreManager, HintSystem
│   ├── Grid/         GridManager, Cell
│   ├── UI/           UIManager, CompassController, ConstellationSelector, MainMenuManager
│   ├── Audio/        (empty — AudioManager goes here in Step 10)
│   └── Data/         ConstellationData, LevelData (ScriptableObject definitions)
├── Prefabs/          Cell prefab (the grid tile)
├── ScriptableObjects/
│   ├── Levels/       Level01.asset
│   └── Constellations/ Orion.asset, Lyra.asset
├── Sprites/
│   ├── UI/           compass dial, needle, buttons, panels
│   └── Grid/         cell states (hidden, revealed, collected, failed), star visuals
├── Audio/
│   ├── SFX/          correct, wrong, constellation-complete, level-complete clips
│   └── Music/        background loop
```

---

## Scene hierarchy (MainGame)

```
Canvas (Screen Space – Overlay)
├── GridContainer          ← GridLayoutGroup, holds all 64 cells
├── SelectorContainer      ← HorizontalLayoutGroup, constellation buttons spawn here
├── CompassWidget          ← CompassController component lives here
│   ├── Needle             ← the rotating Image
│   └── ConstellationLabel ← TMP text showing active constellation name
├── WinPanel               ← disabled by default, shown on win
│   ├── ScoreText
│   └── PlayAgainButton
└── LosePanel              ← disabled by default, shown on lose
    └── PlayAgainButton

HintSystem                 ← empty GameObject with HintSystem component
GameManager                ← empty GameObject
GridManager                ← empty GameObject
ScoreManager               ← empty GameObject
ConstellationSelector      ← empty GameObject with ConstellationSelector component
```

---

## How the cell state machine works

Every cell on the grid is one of four states. Each state has its own visual:

| State | When | Visual needed |
|---|---|---|
| **Hidden** | Default — star/empty unknown | Dark tile, no info |
| **Revealed** | Empty cell was clicked | Slightly lighter tile + hint number (1/2/3 or blank) |
| **Collected** | Correct star clicked | Star shown in constellation color (dimmed 50%) |
| **Failed** | Wrong star clicked | Star shown in red `#E24B4A` |

The hint number colors:
- Distance 1 → gold `#EF9F27`
- Distance 2 → orange `#E5891F`
- Distance 3 → gray `#909090`
- Distance 4+ → blank (no number)

---

## Visual asset slots — what to make and where to put them

### Cell prefab (`Assets/Prefabs/Cell prefab`)
Open the prefab and find the **Cell** component. It has these SerializeField slots:

**Sprites (assign in prefab):**
- `Hidden Sprite` — the default dark tile face. Recommend: subtle dark texture or dark blue panel.
- `Revealed Sprite` — slightly lighter/different texture to show the cell was opened.
- `Collected Sprite` — background for a successfully collected star cell. Can match Hidden or be slightly glowing.
- `Failed Sprite` — background for a wrong-click star. Red tint or cracked look.

**Background Colors (fallback if no sprite assigned):**
Already set in code with hex values. You can leave sprites empty and only use color, or assign sprites and the color is overridden to white automatically.

**The star Image child:**
- The `Star Image` slot on Cell holds the star visual that appears on Collected and Failed cells.
- For **Collected**: the star is tinted with `constellation.color * 0.5f` (half-brightness constellation color).
- For **Failed**: the star is forced to red `#E24B4A`.
- Recommendation: a simple star/circle sprite works well. It will be tinted at runtime so keep the sprite itself white.

**The hint Text child:**
- This is a TextMeshPro component. The text and color are set by code — no action needed on the TMP itself unless you want to change the font.
- If you want a custom font: open the Cell prefab, select the TMP child, assign your font asset there.

---

### Compass widget (`CompassWidget` in Canvas hierarchy)

The compass has two parts — the dial (background) and the needle (rotating child). Both are Image components.

**CompassWidget (the dial):**
- This is just a background Image — assign your compass rose / dial sprite here.
- Size is set in the Rect Transform — resize the CompassWidget to match your sprite's natural size.
- The `Compass Widget` SerializeField slot on CompassController is wired to this object's RectTransform. **Do not rename the object.**

**Needle (child of CompassWidget):**
- Assign your needle sprite to the Image component on the `Needle` child.
- **Critical:** The sprite must point **UP** at 0° rotation. If your needle art points right, you need to rotate it in your art tool before importing, or set the Image's Rect Transform Z rotation to −90 permanently (not via code).
- **Critical:** The Needle's pivot must be at **(0.5, 0)** — bottom-center — so it rotates from its base like a real compass needle. Set this in the Rect Transform pivot field (the target icon in the top-left of the Inspector). If the pivot is at center (0.5, 0.5) it will spin from its middle.
- The needle Image color is changed by code: gold `#EF9F27` normally, red `#E24B4A` on wrong click. Keep the sprite itself white so the color tint works correctly.
- Recommended size: something tall and thin — e.g. 20×80 px in the scene.

**ConstellationLabel (child of CompassWidget):**
- This is a TMP text object. Code sets its text to the active constellation name (e.g. "Orion" / "Lyra") or "Done!" when that constellation is complete.
- Position it below the dial. Style the font however you like.

---

### Win / Lose panels

Both panels are full-screen Canvas panels that start disabled and activate on game end.

**WinPanel** needs:
- A `winScoreText` TMP field wired in UIManager → shows final score + "⭐ PERFECT ⭐" if zero mistakes.
- A Play Again button wired to `UIManager.OnPlayAgainClicked()`.
- Optionally a Main Menu button wired to `UIManager.OnMainMenuClicked()`.

**LosePanel** needs:
- A `loseMistakesText` TMP field wired in UIManager → shows mistake count.
- A Play Again button wired to `UIManager.OnPlayAgainClicked()`.

---

### Constellation selector buttons

These are **generated entirely from code** — no prefab needed, no manual button creation. ConstellationSelector spawns one button per constellation at runtime, sized 160×50, labeled with the constellation name, colored with `constellation.color` (full brightness when active, 35% when inactive). You do not need to create these buttons.

If you want to change the button size or font size, edit these fields in the ConstellationSelector Inspector:
- `Button Size` (default 160×50)
- `Font Size` (default 18)

---

### Background / general UI

No specific slot for a background image — add a full-canvas Image behind everything in the hierarchy as you see fit. The grid and UI will render on top.

---

## Audio asset slots (Step 10 — not yet implemented)

AudioManager is planned but not written yet. When it is, it will have these SerializeField clips to assign:

- `correctClip` — plays on a correct star collected
- `wrongClip` — plays on a wrong click / failed star
- `constellationCompleteClip` — plays when all stars in one constellation are collected
- `levelCompleteClip` — plays on full win
- `bgMusic` — background music, loops from game start

For now, create the audio files and leave them in `Assets/Audio/SFX/` and `Assets/Audio/Music/`. They'll be wired in once AudioManager is written.

---

## Data — setting star positions

Star positions are set in the ScriptableObject assets, not in code.

**Path:** `Assets/ScriptableObjects/Constellations/`

Open `Orion.asset` and `Lyra.asset` in the Inspector. Each has:
- `Constellation Name` — display name shown in label and buttons
- `Color` — drives button color, collected star tint, and selector highlight
- `Star Positions` — array of `Vector2Int`. Index 0 = first star the player must click, Index 1 = second, and so on. **Order matters — this is the collection sequence.**

Grid coordinates: `(0,0)` is top-left, `(7,7)` is bottom-right. X = column, Y = row.

Make sure no two constellations share the same coordinate — `LevelData.asset` will warn in the Console if they overlap.

---

## Key rules for teammates editing code

- All managers are **Singletons** — do not create duplicate GameObjects with the same component, only one of each should exist in the scene.
- All art/audio references are `[SerializeField]` — assign in Inspector, never hardcode paths.
- `GameManager` owns all game state and win/lose logic. Do not call `cell.SetState()` from anywhere except GameManager and GridManager.
- `HintSystem.RefreshAllHints()` must be called any time the active constellation changes or a star is collected. GameManager already does this.
- `CompassController.UpdateTarget()` must be called any time the target star changes. GameManager already does this.
- Do not use `Input.GetKeyDown` or old Input System APIs — this project uses the **New Input System** package.

---

## What's left to implement (Steps 9–12)

| Step | What | Owner |
|---|---|---|
| 9 | ScoreManager events + UIManager HUD (live score, mistake counter, progress per constellation) | Aslan |
| 10 | AudioManager singleton + SFX hookup | Aslan (clips from audio teammate) |
| 11 | Animations: star pulse/glow, correct pop, wrong shake | Aslan + art |
| 12 | Playtest, Windows build, itch.io upload | Aslan |
