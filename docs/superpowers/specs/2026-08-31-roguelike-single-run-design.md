# Roguelike Single-Run Completion Design

## Goal

Make `Assets/Scenes/forth.unity` a self-contained, replayable card-roguelike mode. A run starts on the map, progresses through all node types, resolves the card-based Boss, and ends in an in-scene victory or defeat panel. The player can begin a clean new run or return to the main menu.

## Scope

Included:

- One complete run in `forth`: map selection, Camp, Event, Treasure, Shop, normal battle, elite battle, card-based Boss, result panel, new run, and return to menu.
- Explicit run reset for player statistics, deck state, run-local card upgrades, gold, collectibles, active content, and map state.
- Defeat handling for all card combat encounters.
- EditMode regression coverage for result routing, idempotence, and reset behavior.
- Local harness verification, a focused Git commit, and push to `origin/main` after the tests and build succeed.

Excluded:

- Save/load and cross-session progression.
- Changes to `final.unity`, `thirdscene.unity`, `SampleScene.unity`, the arcade mode, MIDI mode, or the bullet-hell Boss mode.
- New final illustration assets; existing visual fallbacks remain valid.

## Current Problem

`GameManager.TryCompleteCurrentNode` treats a map Boss as a scene transition to `final`. That scene is an unrelated bullet-hell mode, so a roguelike Boss currently requires two different Boss fights. In addition, `CombatController` logs card-combat defeat without resolving the run, and persistent managers retain state when a player tries another run.

## Design

### Run Ownership

Add a `RoguelikeRunController` to the `forth` scene. It is the only component that owns run state transitions:

- `StartNewRun()` creates a clean run and opens a new generated map.
- `CompleteRun()` shows the victory result.
- `FailRun()` shows the defeat result.
- `ReturnToMainMenu()` clears the run and loads `start`.

`GameManager` remains responsible for selecting and completing map nodes. It delegates map-Boss victory to `RoguelikeRunController` instead of loading another scene. `CombatController` reports player defeat to the controller instead of silently stopping combat.

All terminal transitions are idempotent. Once victory or defeat has started, later callbacks from a combat UI, reward UI, or stale node content cannot replace the result or reopen the map.

### State Reset Contract

Starting or clearing a run must restore exactly the state that a freshly opened `forth` scene would have:

- `PlayerStats`: base health, energy, mana cap, gold, shield, temporary effects, combat flags, and per-turn state.
- `DeckManager`: original hidden pool, empty backpack and combat piles, no run-local upgraded `CardData` clones, and no stale card UI events.
- `CollectibleManager`: owned collectibles and turn-local discounts.
- `NodeContentManager`: no active node content or reward UI.
- `MapGenerator` and `GameManager`: no prior selected/completed node, no stale session token, a fresh map, and a visible map container.

Reset is called before map generation so no map node can access state from a previous run. Runtime-only card clones and runtime-only collectible data are destroyed when no longer referenced; immutable `Resources` assets are never changed.

### Result UI

Add a result overlay owned by the roguelike mode. It displays either victory or defeat and exposes two actions:

- `Start new run` invokes `RoguelikeRunController.StartNewRun()`.
- `Return to main menu` invokes `RoguelikeRunController.ReturnToMainMenu()`.

The result UI must use the existing `forth` canvas when it is configured and must create a readable runtime fallback when the scene reference or prefab is absent. It is inactive during normal map and node play, blocks map input while visible, and does not reference `final.unity`.

### Combat and Boss Routing

Normal and elite card-combat victory continues to display its reward before the corresponding map node completes. Card-combat defeat ends the run immediately and does not show a reward.

For a map Boss, card-combat victory completes combat and immediately completes the run. It must not grant a normal card reward, reopen the map, or load `final`. Boss configuration remains part of the existing `CombatController` / `NodeContentManager` flow, but it gains an explicit Boss-mode setting so behavior does not depend on an elite-enemy count heuristic.

### Legacy Isolation

The roguelike mode may load only `start` when the player explicitly chooses to leave the mode. It must never load `final`, `thirdscene`, or `SampleScene`. The unrelated bullet-hell flow and its PlayerPrefs unlock behavior are unchanged.

## Validation

Add EditMode tests that prove:

1. A fresh run resets player, deck, collectible, map, and content state.
2. Card-combat defeat reports exactly one roguelike defeat result.
3. Card-based Boss victory reports exactly one roguelike victory result and does not request a legacy scene.
4. Result actions create a clean new run or request only the main-menu scene.
5. Existing node session and reward idempotence tests remain green.

Before push, run:

```powershell
./scripts/harness/precompletion.ps1 -RunBuild
```

Review the staged diff and commit only the validated roguelike/run-flow files. Push the resulting commit to `origin/main` only after the full command succeeds.

## Risks

- The project currently has unrelated uncommitted changes. Staging must be file-scoped and reviewed so no unrelated work is pushed accidentally.
- The exact `forth` canvas hierarchy may be incomplete. The result overlay therefore needs a runtime fallback and an EditMode test that does not require scene-specific art assets.
- The current persistent managers were built before explicit run boundaries. Their reset APIs must preserve the existing Resource assets and Unity object lifetimes.
