# Roguelike Single-Run Completion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `forth.unity` a self-contained, replayable card-roguelike run with card-Boss victory, defeat, an in-scene result panel, new-run reset, and main-menu return.

**Architecture:** `RoguelikeRunController` owns run transitions and result state. `GameManager` continues to own map-node progression, `CombatController` owns one encounter, and the persistent player/deck/collectible managers expose explicit reset APIs. The roguelike mode never routes to the unrelated bullet-hell scenes.

**Tech Stack:** Unity 2022.3.49f1, C#, NUnit EditMode tests, TextMeshPro, PowerShell harness, GitHub Actions/GameCI.

**Spec:** `docs/superpowers/specs/2026-08-31-roguelike-single-run-design.md`

## Global Constraints

- Modify only the card-roguelike flow under `forth`; do not route to or alter `final.unity`, `thirdscene.unity`, `SampleScene.unity`, arcade scripts, MIDI scripts, or bullet-hell scripts.
- Preserve the existing `NodeContentController` session-token completion gate; terminal run transitions must be idempotent.
- Keep `Resources` card assets immutable; destroy only runtime-created upgraded cards and runtime-created collectibles during reset.
- The result overlay must work against the configured `forth` canvas and create a visible runtime fallback when no scene reference is assigned.
- Verify with `./scripts/harness/precompletion.ps1 -RunBuild` before staging and pushing.
- Stage and push only the reviewed roguelike/run-flow files; do not absorb unrelated dirty working-tree changes.

---

### Task 1: Establish reset-safe run state APIs

**Files:**

- Modify: `Assets/Scripts/PlayerStats.cs:4-459`
- Modify: `Assets/Scripts/CardSystem/DeckManager.cs:6-465`
- Modify: `Assets/Tests/EditMode/NonAssetRuntimeTests.cs`

**Interfaces:**

- Produces `PlayerStats.ResetForNewRun(): void`.
- Produces `DeckManager.ResetForNewRun(): void`.
- Produces `CollectibleManager.ResetForNewRun(): void`.
- `RoguelikeRunController.StartNewRun()` in Task 2 calls all three APIs before generating a map.

- [ ] **Step 1: Add failing reset tests**

  Add `playerObject` to the fixture fields, destroy it in `TearDown`, and add `CollectibleManager.ResetForNewRun();` at the end of `TearDown`. Add this helper to the fixture:

  ```csharp
  private PlayerStats CreatePlayerStats()
  {
      playerObject = new GameObject("PlayerStats");
      PlayerStats stats = playerObject.AddComponent<PlayerStats>();
      stats.InitializeStats();
      return stats;
  }

  private DeckManager CreateDeckManager()
  {
      deckObject = new GameObject("DeckManager");
      return deckObject.AddComponent<DeckManager>();
  }
  ```

  Then add these tests to `NonAssetRuntimeTests`:

  ```csharp
  [Test]
  public void PlayerResetForNewRunRestoresConfiguredBaseValues()
  {
      PlayerStats stats = CreatePlayerStats();
      stats.maxHealth = 145;
      stats.currentHealth = 12;
      stats.maxMana = 14;
      stats.currentMana = 1;
      stats.gold = 3;
      stats.currentShield = 9;
      stats.ApplyStatus(StatusType.Burn, 3);

      stats.ResetForNewRun();

      Assert.That(stats.maxHealth, Is.EqualTo(stats.baseMaxHealth));
      Assert.That(stats.currentHealth, Is.EqualTo(stats.baseMaxHealth));
      Assert.That(stats.maxMana, Is.EqualTo(stats.baseMaxMana));
      Assert.That(stats.currentMana, Is.EqualTo(stats.initialMana));
      Assert.That(stats.gold, Is.EqualTo(stats.startingGold));
      Assert.That(stats.currentShield, Is.Zero);
      Assert.That(stats.GetActiveEffects(), Is.Empty);
  }

  [Test]
  public void DeckAndCollectiblesResetForNewRunClearRuntimeState()
  {
      DeckManager deck = CreateDeckManager();
      CardData source = CreateCard("Source", 2);
      deck.backpack.Add(source);
      Assert.That(deck.TryUpgradeRandomBackpackCard(out CardData upgraded), Is.True);
      deck.hand.Add(CreateCard("Hand", 1));
      CollectibleManager.AddCollectible(CollectibleManager.CreateMaxHealthCollectible());

      deck.ResetForNewRun();
      CollectibleManager.ResetForNewRun();

      Assert.That(deck.backpack, Is.Empty);
      Assert.That(deck.drawPile, Is.Empty);
      Assert.That(deck.hand, Is.Empty);
      Assert.That(deck.discardPile, Is.Empty);
      Assert.That(deck.exhaustPile, Is.Empty);
      Assert.That(CollectibleManager.OwnedCollectibles, Is.Empty);
      Assert.That(upgraded == null, Is.True);
      Object.DestroyImmediate(source);
  }
  ```

- [ ] **Step 2: Run the new tests and verify they fail**

  Run:

  ```powershell
  & 'E:\unity\2022.3.49f1c1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter NonAssetRuntimeTests -testResults artifacts/ci/run-reset-red.xml -logFile artifacts/ci/run-reset-red.log
  ```

  Expected: compilation failure because `ResetForNewRun` does not exist on the three state owners.

- [ ] **Step 3: Implement explicit reset APIs**

  In `PlayerStats`, add `public int baseMaxMana = 10;` beside the serialized base stat configuration and use it rather than a hard-coded mana cap. Add a public reset method that clears every run-local stat and delegates base-value setup to the existing initialization path:

  ```csharp
  public void ResetForNewRun()
  {
      maxHealth = baseMaxHealth;
      currentHealth = baseMaxHealth;
      maxMana = baseMaxMana;
      currentMana = initialMana;
      gold = startingGold;
      currentShield = 0;
      damageSourcesThisTurn.Clear();
      activeEffects.Clear();
      hasFlameShield = false;
      flatDamageReductionNextHit = 0;
      isUntargetable = false;
      regenerationTurns = 0;
      extraDrawsNextTurn = 0;
      extraManaNextTurn = 0;
      delayedHealNextTurn = 0;
      natureGuardActive = false;
      thornCounterAttackDamage = 0;
      lostHealthThisTurn = false;
      starPrayerTurns = 0;
      starPrayerShield = 0;
      starPrayerHeal = 0;
      isInitialCombatTurn = false;
  }
  ```

  In `DeckManager`, collect all cards from `hiddenCardPool`, `backpack`, `drawPile`, `hand`, `discardPile`, and `exhaustPile` before clearing them. Destroy only unique cards with `upgradeLevel > 0`; then call `InitializeHiddenPool()`, `DeckChanged?.Invoke()`, and `HandChanged?.Invoke()`.

  In `CollectibleManager`, keep a private `HashSet<CollectibleData>` for collectibles created by the existing `Create*Collectible` factory methods. Each factory registers its newly created object. `ResetForNewRun()` destroys only members of that set, clears it, and calls the existing `Clear()` method. Do not destroy collectible assets that were supplied from elsewhere.

- [ ] **Step 4: Run the reset tests and verify they pass**

  Run the command from Step 2 with `run-reset-green.xml` and `run-reset-green.log`.

  Expected: the `NonAssetRuntimeTests` fixture passes, including the two new reset tests.

- [ ] **Step 5: Commit the independently verified state boundary**

  ```powershell
  git add -- Assets/Scripts/PlayerStats.cs Assets/Scripts/CardSystem/DeckManager.cs Assets/Tests/EditMode/NonAssetRuntimeTests.cs
  git diff --cached --check
  git commit -m "feat: add roguelike run reset state"
  ```

### Task 2: Add the run lifecycle controller and result overlay

**Files:**

- Create: `Assets/Scripts/NodeSystem/RoguelikeRunController.cs`
- Create: `Assets/Scripts/NodeSystem/RoguelikeRunController.cs.meta`
- Create: `Assets/Scripts/NodeSystem/RoguelikeResultPanel.cs`
- Create: `Assets/Scripts/NodeSystem/RoguelikeResultPanel.cs.meta`
- Modify: `Assets/Scripts/NodeSystem/GameManager.cs:4-158`
- Modify: `Assets/Scripts/NodeSystem/NodeContentManager.cs:3-183`
- Modify: `Assets/Tests/EditMode/NonAssetRuntimeTests.cs`

**Interfaces:**

- Consumes `PlayerStats.ResetForNewRun()`, `DeckManager.ResetForNewRun()`, and `CollectibleManager.ResetForNewRun()` from Task 1.
- Produces `enum RoguelikeRunResult { None, Victory, Defeat }`.
- Produces `RoguelikeRunController.StartNewRun(): bool`, `CompleteRun(): bool`, `FailRun(): bool`, `ReturnToMainMenu(): void`, and `LastRequestedSceneName: string`.
- Produces `RoguelikeResultPanel.Show(RoguelikeRunResult result, UnityAction newRun, UnityAction returnToMenu): void` and `Hide(): void`.
- Produces `GameManager.ResetMapForNewRun(): void` and `GameManager.RunController`.

- [ ] **Step 1: Add failing run-transition tests**

  Add a `runControllerObject` and `resultPanelObject` to `NonAssetRuntimeTests` cleanup. Add a test that uses asset-free map generation and a result panel component:

  ```csharp
  [Test]
  public void RunControllerShowsExactlyOneTerminalResultAndResetsForANewRun()
  {
      generatorObject = new GameObject("RunMap");
      MapGenerator generator = generatorObject.AddComponent<MapGenerator>();
      generator.totalDepth = 1;
      generator.nodesPerLayer = 1;
      generator.nodeTemplate = null;
      generator.mapContainer = null;
      generator.nodeVisualizer = null;

      contentManagerObject = new GameObject("RunContent");
      NodeContentManager content = contentManagerObject.AddComponent<NodeContentManager>();
      gameManagerObject = new GameObject("RunGameManager");
      GameManager game = gameManagerObject.AddComponent<GameManager>();
      game.mapGenerator = generator;
      game.contentManager = content;
      deckObject = new GameObject("RunDeck");
      DeckManager deck = deckObject.AddComponent<DeckManager>();
      playerObject = new GameObject("RunStats");
      PlayerStats stats = playerObject.AddComponent<PlayerStats>();

      resultPanelObject = new GameObject("RunResultPanel");
      RoguelikeResultPanel panel = resultPanelObject.AddComponent<RoguelikeResultPanel>();
      runControllerObject = new GameObject("RunController");
      RoguelikeRunController run = runControllerObject.AddComponent<RoguelikeRunController>();
      run.gameManager = game;
      run.resultPanel = panel;

      Assert.That(run.StartNewRun(), Is.True);
      Assert.That(run.CompleteRun(), Is.True);
      Assert.That(run.FailRun(), Is.False);
      Assert.That(run.CurrentResult, Is.EqualTo(RoguelikeRunResult.Victory));
      Assert.That(panel.IsVisible, Is.True);

      deck.backpack.Add(CreateCard("Transient", 1));
      stats.gold = 1;
      Assert.That(run.StartNewRun(), Is.True);
      Assert.That(run.CurrentResult, Is.EqualTo(RoguelikeRunResult.None));
      Assert.That(panel.IsVisible, Is.False);
      Assert.That(deck.backpack, Is.Empty);
      Assert.That(stats.gold, Is.EqualTo(stats.startingGold));
  }
  ```

- [ ] **Step 2: Run the controller test and verify it fails**

  Run:

  ```powershell
  & 'E:\unity\2022.3.49f1c1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter NonAssetRuntimeTests.RunControllerShowsExactlyOneTerminalResultAndResetsForANewRun -testResults artifacts/ci/run-controller-red.xml -logFile artifacts/ci/run-controller-red.log
  ```

  Expected: compilation failure because `RoguelikeRunController`, `RoguelikeResultPanel`, and `GameManager.ResetMapForNewRun` do not exist.

- [ ] **Step 3: Implement the lifecycle and panel**

  Create `RoguelikeRunController` with this complete public and private transition contract:

  ```csharp
  public enum RoguelikeRunResult { None, Victory, Defeat }

  public class RoguelikeRunController : MonoBehaviour
  {
      public GameManager gameManager;
      public RoguelikeResultPanel resultPanel;
      public string mainMenuSceneName = "start";
      public RoguelikeRunResult CurrentResult { get; private set; }
      public string LastRequestedSceneName { get; private set; }
      public bool IsTerminal => CurrentResult != RoguelikeRunResult.None;

      public bool StartNewRun()
      {
          GameManager manager = ResolveGameManager();
          if (manager == null)
              return false;
          PlayerStats.EnsureInstance();
          DeckManager.EnsureInstance();
          PlayerStats.Instance.ResetForNewRun();
          DeckManager.Instance.ResetForNewRun();
          CollectibleManager.ResetForNewRun();
          CurrentResult = RoguelikeRunResult.None;
          LastRequestedSceneName = null;
          EnsureResultPanel().Hide();
          return manager.ResetMapForNewRun();
      }

      public bool CompleteRun() => TrySetResult(RoguelikeRunResult.Victory);
      public bool FailRun() => TrySetResult(RoguelikeRunResult.Defeat);

      public void ReturnToMainMenu()
      {
          ClearRunState();
          LastRequestedSceneName = mainMenuSceneName;
          if (Application.isPlaying)
              SceneManager.LoadScene(mainMenuSceneName);
      }

      private GameManager ResolveGameManager()
      {
          if (gameManager == null)
              gameManager = GameManager.Instance;
          return gameManager;
      }

      private RoguelikeResultPanel EnsureResultPanel()
      {
          if (resultPanel != null)
              return resultPanel;
          resultPanel = GetComponentInChildren<RoguelikeResultPanel>(true);
          if (resultPanel != null)
              return resultPanel;
          GameObject panelObject = new GameObject("RoguelikeRunResultPanel");
          panelObject.transform.SetParent(transform, false);
          resultPanel = panelObject.AddComponent<RoguelikeResultPanel>();
          return resultPanel;
      }

      private bool TrySetResult(RoguelikeRunResult result)
      {
          if (IsTerminal)
              return false;
          GameManager manager = ResolveGameManager();
          if (manager != null)
          {
              manager.contentManager?.ClearCurrentContent();
              if (manager.mapGenerator != null && manager.mapGenerator.mapContainer != null)
                  manager.mapGenerator.mapContainer.gameObject.SetActive(false);
          }
          CurrentResult = result;
          EnsureResultPanel().Show(result, StartNewRun, ReturnToMainMenu);
          return true;
      }

      private void ClearRunState()
      {
          PlayerStats.EnsureInstance();
          DeckManager.EnsureInstance();
          PlayerStats.Instance.ResetForNewRun();
          DeckManager.Instance.ResetForNewRun();
          CollectibleManager.ResetForNewRun();
          GameManager manager = ResolveGameManager();
          if (manager != null && manager.contentManager != null)
              manager.contentManager.ClearCurrentContent();
      }
  }
  ```

  The code above gives all lifecycle helpers their exact responsibilities. `RoguelikeResultPanel.Show` assigns its result state, creates its fallback under the first active Canvas when required, or creates a screen-space-overlay Canvas when the scene has none, removes prior listeners from both buttons, then adds the two supplied callbacks. `Hide` disables the overlay GameObject and clears `IsVisible`.

  `RoguelikeResultPanel` stores `IsVisible` and the displayed `RoguelikeRunResult`; it creates a full-screen semi-transparent `Image`, title `TextMeshProUGUI`, body text, and two `Button` controls beneath the first active `Canvas`, or beneath its own screen-space-overlay fallback Canvas, if no serialized panel exists. `Show` removes prior button listeners before adding the supplied callbacks. `Hide` deactivates the root panel.

  In `GameManager`, add `public RoguelikeRunController runController;` and a public `RunController` property. Resolve it in `Awake` by finding or adding the component to the GameManager object. Replace one-time `InitializeGame` map creation with `runController.StartNewRun()` when a run controller is present. Implement `ResetMapForNewRun()` to clear content, clear current node/session state, call `mapGenerator.GenerateMap()`, call `mapGenerator.ReopenMap()`, set `gameStarted = true`, and return `true` when the map generator exists.

  In `NodeContentManager`, add a read-only `HasCurrentContent` property and leave `ClearCurrentContent()` as the sole content destruction path.

- [ ] **Step 4: Run the controller test and verify it passes**

  Run the Step 2 command with `run-controller-green.xml` and `run-controller-green.log`.

  Expected: the controller accepts the first terminal result, rejects the second, hides the result on a new run, and resets run state.

- [ ] **Step 5: Commit the lifecycle boundary**

  ```powershell
  git add -- Assets/Scripts/NodeSystem/RoguelikeRunController.cs Assets/Scripts/NodeSystem/RoguelikeRunController.cs.meta Assets/Scripts/NodeSystem/RoguelikeResultPanel.cs Assets/Scripts/NodeSystem/RoguelikeResultPanel.cs.meta Assets/Scripts/NodeSystem/GameManager.cs Assets/Scripts/NodeSystem/NodeContentManager.cs Assets/Tests/EditMode/NonAssetRuntimeTests.cs
  git diff --cached --check
  git commit -m "feat: add roguelike run lifecycle"
  ```

### Task 3: Route card combat defeat and Boss victory into the run lifecycle

**Files:**

- Modify: `Assets/Scripts/CardSystem/CombatController.cs:6-542`
- Modify: `Assets/Scripts/NodeSystem/NodeContentManager.cs:27-153`
- Modify: `Assets/Scripts/NodeSystem/GameManager.cs:119-158`
- Modify: `Assets/Tests/EditMode/NonAssetRuntimeTests.cs`
- Modify: `Assets/Tests/EditMode/MapInputAlignmentTests.cs`

**Interfaces:**

- Consumes `RoguelikeRunController.FailRun(): bool`, `CompleteRun(): bool`, and `GameManager.RunController` from Task 2.
- Produces `CombatController.isBossBattle: bool` and `CombatController.ResolvePlayerDefeat(): bool`.
- Produces boss-node routing that never loads `final`.

- [ ] **Step 1: Add failing encounter-routing tests**

  Add tests that use the public methods introduced in this task:

  ```csharp
  [Test]
  public void CardCombatDefeatEndsTheRunWithoutGrantingAReward()
  {
      RoguelikeRunController run = CreateActiveRun(out GameManager game);
      GameObject combatObject = new GameObject("DefeatCombat");
      CombatController combat = combatObject.AddComponent<CombatController>();
      combat.completeNodeOnVictory = true;

      Assert.That(combat.ResolvePlayerDefeat(), Is.True);
      Assert.That(run.CurrentResult, Is.EqualTo(RoguelikeRunResult.Defeat));
      Assert.That(combat.ResolvePlayerDefeat(), Is.False);
      Object.DestroyImmediate(combatObject);
  }

  [Test]
  public void BossNodeCompletionShowsRoguelikeVictoryWithoutLegacyScene()
  {
      RoguelikeRunController run = CreateActiveRun(out GameManager game);
      Node boss = CreateActiveNode(NodeType.Boss);
      game.SelectNode(boss);
      int session = game.CurrentContentSession;

      Assert.That(game.TryCompleteCurrentNode(boss, session), Is.True);
      Assert.That(run.CurrentResult, Is.EqualTo(RoguelikeRunResult.Victory));
      Assert.That(run.LastRequestedSceneName, Is.Null);
      Object.DestroyImmediate(boss.gameObject);
  }

  private RoguelikeRunController CreateActiveRun(out GameManager game)
  {
      generatorObject = new GameObject("RoutingMap");
      MapGenerator generator = generatorObject.AddComponent<MapGenerator>();
      generator.totalDepth = 1;
      generator.nodesPerLayer = 1;
      generator.nodeTemplate = null;
      generator.mapContainer = null;
      generator.nodeVisualizer = null;
      contentManagerObject = new GameObject("RoutingContent");
      NodeContentManager content = contentManagerObject.AddComponent<NodeContentManager>();
      gameManagerObject = new GameObject("RoutingGameManager");
      game = gameManagerObject.AddComponent<GameManager>();
      game.mapGenerator = generator;
      game.contentManager = content;
      deckObject = new GameObject("RoutingDeck");
      deckObject.AddComponent<DeckManager>();
      playerObject = new GameObject("RoutingPlayer");
      playerObject.AddComponent<PlayerStats>();
      resultPanelObject = new GameObject("RoutingResult");
      RoguelikeResultPanel panel = resultPanelObject.AddComponent<RoguelikeResultPanel>();
      runControllerObject = new GameObject("RoutingRunController");
      RoguelikeRunController run = runControllerObject.AddComponent<RoguelikeRunController>();
      run.gameManager = game;
      run.resultPanel = panel;
      game.runController = run;
      Assert.That(run.StartNewRun(), Is.True);
      return run;
  }

  private static Node CreateActiveNode(NodeType type)
  {
      GameObject nodeObject = new GameObject(type + "Node");
      Node node = nodeObject.AddComponent<Node>();
      node.type = type;
      node.isActive = true;
      return node;
  }
  ```

  Update `MapInputAlignmentTests.FinalSceneIsEnabledForBossCompletion` to `RoguelikeBossDoesNotRouteToLegacyFinalScene`, asserting that the `GameManager` source does not contain `SceneManager.LoadScene("final")`; retain the existing test that verifies all node prefabs and icons in `forth`.

- [ ] **Step 2: Run the routing tests and verify they fail**

  Run:

  ```powershell
  & 'E:\unity\2022.3.49f1c1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter NonAssetRuntimeTests.CardCombatDefeatEndsTheRunWithoutGrantingAReward -testResults artifacts/ci/run-routing-red.xml -logFile artifacts/ci/run-routing-red.log
  ```

  Expected: compilation failure because `ResolvePlayerDefeat` and the boss routing contract are absent.

- [ ] **Step 3: Implement encounter routing**

  In `CombatController`, add `public bool isBossBattle;` beside the existing elite setting. Set `showCardRewardOnVictory = false` for Boss content. Add this public, idempotent defeat path:

  ```csharp
  public bool ResolvePlayerDefeat()
  {
      if (!combatActive)
          return false;

      combatActive = false;
      resolvingTurn = false;
      if (DeckManager.Instance != null)
          DeckManager.Instance.EndCombat();

      RoguelikeRunController run = GameManager.Instance != null
          ? GameManager.Instance.RunController
          : FindObjectOfType<RoguelikeRunController>();
      return run != null && run.FailRun();
  }
  ```

  Call `ResolvePlayerDefeat()` after enemy-turn damage and from `Update()` before victory checking so self-damage and status damage also terminate a run. In `CheckVictory()`, call `CompleteCombat()` immediately when `isBossBattle` is true, before reward generation.

  In `NodeContentManager.EnsureContentController` and `CreateFallbackContent`, set `isBossBattle = nodeType == NodeType.Boss`, set `isEliteBattle = nodeType == NodeType.EliteBattle`, retain the existing elite enemy count only for elite encounters, and set `showCardRewardOnVictory = nodeType != NodeType.Boss`.

  In `GameManager.TryCompleteCurrentNode`, replace the Boss `SceneManager.LoadScene("final")` branch with: clear content, call `RunController.CompleteRun()`, and return its result. Do not reopen the map for a completed Boss. The no-run-controller fallback must log an error and reopen the map rather than loading any legacy scene.

- [ ] **Step 4: Run all routing tests and verify they pass**

  Run:

  ```powershell
  & 'E:\unity\2022.3.49f1c1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter "NonAssetRuntimeTests|MapInputAlignmentTests" -testResults artifacts/ci/run-routing-green.xml -logFile artifacts/ci/run-routing-green.log
  ```

  Expected: defeat is terminal and idempotent, Boss victory sets only roguelike victory, and no test expects the bullet-hell scene.

- [ ] **Step 5: Commit the encounter routing**

  ```powershell
  git add -- Assets/Scripts/CardSystem/CombatController.cs Assets/Scripts/NodeSystem/NodeContentManager.cs Assets/Scripts/NodeSystem/GameManager.cs Assets/Tests/EditMode/NonAssetRuntimeTests.cs Assets/Tests/EditMode/MapInputAlignmentTests.cs
  git diff --cached --check
  git commit -m "feat: finish roguelike boss and defeat routing"
  ```

### Task 4: Configure the `forth` scene and lock the complete flow with tests

**Files:**

- Modify: `Assets/Scenes/forth.unity`
- Modify: `Assets/Tests/EditMode/MapInputAlignmentTests.cs`
- Modify: `Assets/Tests/EditMode/NonAssetRuntimeTests.cs`
- Modify: `docs/technical-overview.md`
- Modify: `README.md`

**Interfaces:**

- Consumes `RoguelikeRunController` and `RoguelikeResultPanel` from Task 2.
- Consumes the Boss encounter flags and completion behavior from Task 3.
- Produces a configured `forth` scene with a visible result-panel route and documented self-contained flow.

- [ ] **Step 1: Add a failing scene-configuration test**

  Add this test to `MapInputAlignmentTests`:

  ```csharp
  [Test]
  public void ForthSceneContainsTheSelfContainedRoguelikeRunController()
  {
      EditorSceneManager.OpenScene("Assets/Scenes/forth.unity", OpenSceneMode.Single);

      RoguelikeRunController run = UnityEngine.Object.FindObjectOfType<RoguelikeRunController>();
      Assert.That(run, Is.Not.Null);
      Assert.That(run.mainMenuSceneName, Is.EqualTo("start"));
      Assert.That(run.resultPanel, Is.Not.Null);
  }
  ```

- [ ] **Step 2: Run the scene test and verify it fails**

  Run:

  ```powershell
  & 'E:\unity\2022.3.49f1c1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter MapInputAlignmentTests.ForthSceneContainsTheSelfContainedRoguelikeRunController -testResults artifacts/ci/forth-run-scene-red.xml -logFile artifacts/ci/forth-run-scene-red.log
  ```

  Expected: failure because the `forth` scene has not yet serialized the run controller and panel references.

- [ ] **Step 3: Configure the scene and document the flow**

  Open `Assets/Scenes/forth.unity` in Unity 2022.3.49f1. On the existing GameManager object, add `RoguelikeRunController`. Add `RoguelikeResultPanel` to an inactive child GameObject under the scene Canvas. Assign the GameManager and panel references, set `mainMenuSceneName` to `start`, and ensure the panel is inactive before Play mode. Save the scene through Unity so its YAML and `.meta` references remain valid.

  Update `docs/technical-overview.md` and `README.md` to state: Boss nodes are card battles; victory and defeat remain inside `forth`; `final` is an unrelated bullet-hell mode; result actions start a clean run or load `start`; save/load remains out of scope.

- [ ] **Step 4: Run the scene and flow tests and verify they pass**

  Run:

  ```powershell
  & 'E:\unity\2022.3.49f1c1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testFilter "MapInputAlignmentTests|NonAssetRuntimeTests" -testResults artifacts/ci/forth-run-scene-green.xml -logFile artifacts/ci/forth-run-scene-green.log
  ```

  Expected: the configured scene exposes a result panel, every node route remains configured, and the complete asset-free flow tests pass.

- [ ] **Step 5: Commit scene configuration and documentation**

  ```powershell
  git add -- Assets/Scenes/forth.unity Assets/Tests/EditMode/MapInputAlignmentTests.cs Assets/Tests/EditMode/NonAssetRuntimeTests.cs docs/technical-overview.md README.md
  git diff --cached --check
  git commit -m "feat: add roguelike run result interface"
  ```

### Task 5: Run full verification, review the outgoing diff, and push

**Files:**

- Modify if required by a failed check: only the files named in Tasks 1-4.
- Review: `.github/workflows/unity-ci.yml`, `scripts/harness/check-architecture.ps1`, `scripts/harness/lint.ps1`, and the staged Git diff.

**Interfaces:**

- Consumes all production and test interfaces from Tasks 1-4.
- Produces one reviewed commit series on `main` and a push to `origin/main`.

- [ ] **Step 1: Run the complete local harness including the player build**

  Run:

  ```powershell
  ./scripts/harness/precompletion.ps1 -RunBuild
  ```

  Expected: architecture check, node lifecycle lint, whitespace check, Unity C# compilation, all EditMode tests, and Windows Standalone build pass.

- [ ] **Step 2: Read the test result and build evidence**

  Run:

  ```powershell
  $xml = Get-Content -Raw artifacts/ci/editmode-results.xml
  [regex]::Match($xml, '<test-run[^>]+>').Value
  Get-Content artifacts/ci/build.log -Tail 20
  ```

  Expected: the test-run reports `result="Passed"` with `failed="0"`; the build log ends with return code `0`.

- [ ] **Step 3: Review only the outgoing changes**

  Run:

  ```powershell
  git status --short
  git diff --check
  git diff origin/main -- Assets/Scripts/NodeSystem/RoguelikeRunController.cs Assets/Scripts/NodeSystem/RoguelikeResultPanel.cs Assets/Scripts/NodeSystem/GameManager.cs Assets/Scripts/NodeSystem/NodeContentManager.cs Assets/Scripts/CardSystem/CombatController.cs Assets/Scripts/CardSystem/DeckManager.cs Assets/Scripts/PlayerStats.cs Assets/Scenes/forth.unity Assets/Tests/EditMode/NonAssetRuntimeTests.cs Assets/Tests/EditMode/MapInputAlignmentTests.cs docs/technical-overview.md README.md
  ```

  Expected: no whitespace errors and no unrelated legacy-scene changes in the outgoing diff.

- [ ] **Step 4: Create the final scoped commit if Tasks 1-4 did not already create it**

  Run:

  ```powershell
  git add -- Assets/Scripts/NodeSystem/RoguelikeRunController.cs Assets/Scripts/NodeSystem/RoguelikeRunController.cs.meta Assets/Scripts/NodeSystem/RoguelikeResultPanel.cs Assets/Scripts/NodeSystem/RoguelikeResultPanel.cs.meta Assets/Scripts/NodeSystem/GameManager.cs Assets/Scripts/NodeSystem/NodeContentManager.cs Assets/Scripts/CardSystem/CombatController.cs Assets/Scripts/CardSystem/DeckManager.cs Assets/Scripts/PlayerStats.cs Assets/Scenes/forth.unity Assets/Tests/EditMode/NonAssetRuntimeTests.cs Assets/Tests/EditMode/MapInputAlignmentTests.cs docs/technical-overview.md README.md
  git diff --cached --check
  git commit -m "feat: complete roguelike single-run flow"
  ```

  Expected: only reviewed roguelike-flow files are staged and committed.

- [ ] **Step 5: Push the verified commit series**

  Run:

  ```powershell
  git push origin main
  ```

  Expected: `origin/main` advances with the spec and verified roguelike-flow commits. Inspect the GitHub Actions run after the push; its EditMode and Windows build jobs must report success before calling the remote branch stable.
