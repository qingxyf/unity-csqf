# Non-Asset Runtime And CI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Unity roguelike runtime resilient when visual assets or prefabs are missing, implement the reserved random card upgrade path, and provide reproducible local/CI verification.

**Architecture:** Keep gameplay rules in the existing managers (`DeckManager`, `EventManager`, `MapGenerator`, and `NodeVisualizer`). Missing assets are presentation concerns and must fall back to generated GameObjects or hidden optional images. Verification lives outside runtime code in `scripts/harness`, with GitHub Actions invoking Unity's EditMode tests and a project build.

**Tech Stack:** Unity 2022.3.49f1, C#, NUnit EditMode tests, PowerShell harness scripts, GitHub Actions/GameCI.

---

### Task 1: Runtime asset fallbacks

**Files:**
- Modify: `Assets/Scripts/NodeSystem/MapGenerator.cs`
- Modify: `Assets/Scripts/NodeSystem/NodeVisualizer.cs`
- Modify: `Assets/Scripts/NodeSystem/GameManager.cs`

- [ ] Make map/container/node creation safe when scene references are unassigned.
- [ ] Make connection rendering skip or generate a minimal line renderer when its visual prefab is absent.
- [ ] Ensure the node content manager is created at runtime when the scene omits it.
- [ ] Preserve existing prefab behavior when references are assigned.

### Task 2: Implement random card upgrades

**Files:**
- Modify: `Assets/Scripts/CardSystem/CardData.cs`
- Modify: `Assets/Scripts/CardSystem/DeckManager.cs`
- Modify: `Assets/Scripts/NodeSystem/Events/EventManager.cs`
- Modify: `Assets/Scripts/NodeSystem/Events/EventData.cs`

- [ ] Track one run-local upgrade level on cloned `CardData` objects without mutating shared Resources assets.
- [ ] Add a deterministic, testable `TryUpgradeRandomBackpackCard` API that reduces cost by one and keeps effect/art references.
- [ ] Apply `EventChoice.upgradeRandomCard` and include a useful result message when no eligible card exists.

### Task 3: Tests and CI harness

**Files:**
- Modify: `Assets/Tests/EditMode/RoguelikeV02RulesTests.cs`
- Create: `Assets/Tests/EditMode/NonAssetRuntimeTests.cs`
- Create: `scripts/harness/check-architecture.ps1`
- Create: `scripts/harness/precompletion.ps1`
- Create: `.github/workflows/unity-ci.yml`

- [ ] Remove the compile gate that prevents the existing rules tests from running in CI.
- [ ] Add EditMode coverage for card upgrades and missing visual references.
- [ ] Add a PowerShell architecture/static check with repair hints.
- [ ] Add a PowerShell precompletion runner that executes static checks and Unity tests/builds when an Editor is available.
- [ ] Add a GitHub Actions workflow pinned to Unity 2022.3.49f1 that runs EditMode tests and a standalone build.

### Task 4: Documentation and verification

**Files:**
- Modify: `docs/technical-overview.md`
- Modify: `README.md`

- [ ] Document card upgrades, fallback behavior, and verification commands.
- [ ] Run `git diff --check`, the architecture check, and the precompletion runner.
- [ ] Record whether local Unity execution is unavailable and leave CI as the authoritative Unity verification path.
