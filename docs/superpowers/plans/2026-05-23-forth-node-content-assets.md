# Forth Node Content Assets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish the `forth` roguelike node content by creating event resources, content prefabs, generated art assets, and scene bindings.

**Architecture:** Keep the existing runtime fallback path, but add project assets so `forth.unity` uses explicit content prefabs and `Resources/Events` assets. Generate event illustrations with `gpt-image-2`, import them as Unity sprites, then reference them from `EventData` assets.

**Tech Stack:** Unity 2022 YAML assets/prefabs, C# MonoBehaviours, `Assets/Resources`, OpenAI `gpt-image-2` via imagegen CLI, PowerShell verification scripts.

---

### Task 1: Event Resource Coverage

**Files:**
- Modify: `Assets/Scripts/NodeSystem/Events/EventDataCreator.cs`
- Create: `Assets/Resources/Events/Event_*.asset`

- [ ] Add creator support for all 12 events listed in `docs/roguelike/events-and-content.md`.
- [ ] Create `Assets/Resources/Events` assets for those 12 events.
- [ ] Verify there are 12 event assets and each has at least 3 choices.

### Task 2: Generated Event Art

**Files:**
- Create: `Assets/Sprites/Events/*.png`
- Create: `Assets/Sprites/Events/*.png.meta`
- Modify: `Assets/Resources/Events/Event_*.asset`

- [ ] Generate 12 event illustrations using `gpt-image-2`.
- [ ] Import each generated PNG as a Unity sprite.
- [ ] Attach the matching sprite to each `EventData.illustration`.

### Task 3: Explicit Node Prefabs

**Files:**
- Create: `Assets/Prefabs/NodeContent/*.prefab`
- Create: `Assets/Prefabs/NodeContent/*.prefab.meta`
- Modify: `Assets/Scenes/forth.unity`

- [ ] Create explicit prefabs for event, treasure, shop, battle, elite battle, and boss fallback content.
- [ ] Bind all `NodeContentManager` prefab slots in `forth.unity`.
- [ ] Preserve `NodeContentManager` runtime fallback as safety.

### Task 4: Verification

**Files:**
- Read: `Assets/Scenes/forth.unity`
- Read: Unity `Editor.log`

- [ ] Run event asset coverage checks.
- [ ] Run prefab binding checks for `forth.unity`.
- [ ] Run missing script and missing GUID scans.
- [ ] Confirm Unity compiles after asset/script changes.
