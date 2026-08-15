# Roguelike Card v0.2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved v0.2 roguelike card loop across combat, rewards, camp, shop, and events.

**Architecture:** Keep changes inside the existing runtime managers. `PlayerStats` owns player resources, `CombatController` owns combat turn flow and reward handoff, node managers own their own runtime UI and service rules.

**Tech Stack:** Unity 2022 C#, NUnit EditMode tests, existing Resources card/event data.

---

### Task 1: Lock Rules With Tests

**Files:**
- Create: `Assets/Tests/EditMode/RoguelikeV02RulesTests.cs`

- [ ] Test default mana, direct health payment, basic attack turn ending, shop pricing, and event cost semantics.

### Task 2: Player Resources

**Files:**
- Modify: `Assets/Scripts/PlayerStats.cs`

- [ ] Set default max mana to 3.
- [ ] Add gold state and helper methods.
- [ ] Add direct health payment that bypasses shield and reductions.
- [ ] Preserve run-level max health changes after battle.

### Task 3: Combat Turn Flow

**Files:**
- Modify: `Assets/Scripts/CardSystem/CombatController.cs`
- Modify: `Assets/Scripts/CardSystem/CardEffectManager.cs`
- Modify: `Assets/Scripts/CardSystem/Effects/Neutral/WaitAndSeeEffect.cs`
- Modify: `Assets/Scripts/CardSystem/Effects/Neutral/SetUpCampEffect.cs`

- [ ] Add a safe full-turn request API.
- [ ] Add runtime basic attack button.
- [ ] Make end-turn cards request the full turn transition.
- [ ] Grant gold and reward choices on victory for battle and elite nodes.

### Task 4: Node Services

**Files:**
- Modify: `Assets/Scripts/NodeSystem/RewardChoiceUI.cs`
- Modify: `Assets/Scripts/NodeSystem/CampManager.cs`
- Modify: `Assets/Scripts/NodeSystem/ShopManager.cs`
- Modify: `Assets/Scripts/NodeSystem/Events/EventManager.cs`

- [ ] Add skip rewards for treasure/card rewards.
- [ ] Replace camp one-shot reward with service choices.
- [ ] Rework shop into gold-priced cards and services.
- [ ] Use direct health payment for event costs and gamble failures.

### Task 5: Documentation And Verification

**Files:**
- Modify: `docs/technical-overview.md`
- Modify: `README.md`

- [ ] Update rule descriptions.
- [ ] Compile Assembly-CSharp and EditMode tests.
- [ ] Run missing script/reference scan.
