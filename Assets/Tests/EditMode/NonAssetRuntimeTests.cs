using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class NonAssetRuntimeTests
{
    private GameObject generatorObject;
    private GameObject visualizerObject;
    private GameObject deckObject;
    private GameObject playerObject;
    private GameObject firstNodeObject;
    private GameObject secondNodeObject;
    private GameObject gameManagerObject;
    private GameObject contentManagerObject;
    private GameObject rewardObject;
    private GameObject campManagerObject;
    private GameObject runControllerObject;
    private GameObject resultPanelObject;
    private GameObject combatObject;
    private CardData rewardCard;

    [TearDown]
    public void TearDown()
    {
        Destroy(generatorObject);
        Destroy(visualizerObject);
        Destroy(deckObject);
        Destroy(playerObject);
        Destroy(firstNodeObject);
        Destroy(secondNodeObject);
        Destroy(gameManagerObject);
        Destroy(contentManagerObject);
        Destroy(rewardObject);
        Destroy(campManagerObject);
        Destroy(runControllerObject);
        Destroy(resultPanelObject);
        Destroy(combatObject);
        if (rewardCard != null)
            Object.DestroyImmediate(rewardCard);
        DeckManager.Instance = null;
        PlayerStats.Instance = null;
        GameManager.Instance = null;
        if (EnemyManager.Instance != null)
            Object.DestroyImmediate(EnemyManager.Instance.gameObject);
        if (CardEffectManager.Instance != null)
            Object.DestroyImmediate(CardEffectManager.Instance.gameObject);
        EnemyManager.Instance = null;
        CardEffectManager.Instance = null;
        CollectibleManager.ResetForNewRun();
    }

    [Test]
    public void MapGeneratorBuildsPlayableNodesWithoutPrefabs()
    {
        generatorObject = new GameObject("RuntimeMapGenerator");
        MapGenerator generator = generatorObject.AddComponent<MapGenerator>();
        generator.totalDepth = 1;
        generator.nodesPerLayer = 1;
        generator.visibleDepthAhead = 2;
        generator.nodeTemplate = null;
        generator.mapContainer = null;
        generator.nodeVisualizer = null;

        generator.GenerateMap();

        Assert.That(generator.mapContainer, Is.Not.Null);
        Assert.That(generator.GetNodesByDepth().Count, Is.EqualTo(3));
        Assert.That(generator.GetNodesByDepth()[0].Count, Is.EqualTo(1));
        Assert.That(generator.GetNodesByDepth()[2][0].type, Is.EqualTo(NodeType.Boss));
        Assert.That(generator.GetNodesByDepth()[0][0].GetComponent<Collider2D>(), Is.Not.Null);
    }

    [Test]
    public void NodeVisualizerBuildsFallbackConnectionWithoutLinePrefab()
    {
        visualizerObject = new GameObject("RuntimeNodeVisualizer");
        NodeVisualizer visualizer = visualizerObject.AddComponent<NodeVisualizer>();
        visualizer.lineRendererPrefab = null;

        firstNodeObject = new GameObject("FirstNode");
        secondNodeObject = new GameObject("SecondNode");
        Node first = firstNodeObject.AddComponent<Node>();
        Node second = secondNodeObject.AddComponent<Node>();
        firstNodeObject.transform.position = Vector3.zero;
        secondNodeObject.transform.position = Vector3.right;
        first.nextNodes.Add(second);

        visualizer.VisualizeConnections(new System.Collections.Generic.List<System.Collections.Generic.List<Node>>
        {
            new System.Collections.Generic.List<Node> { first, second }
        });

        Assert.That(visualizerObject.GetComponentsInChildren<LineRenderer>().Length, Is.EqualTo(1));
    }

    [Test]
    public void CardUpgradeClonesBackpackCardWithoutMutatingSourceAsset()
    {
        deckObject = new GameObject("DeckManager");
        DeckManager deck = deckObject.AddComponent<DeckManager>();
        CardData source = ScriptableObject.CreateInstance<CardData>();
        source.cardName = "测试卡";
        source.description = "原始描述";
        source.cost = 2;
        deck.backpack.Add(source);

        bool upgraded = deck.TryUpgradeRandomBackpackCard(out CardData result);

        Assert.That(upgraded, Is.True);
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.SameAs(source));
        Assert.That(result.upgradeLevel, Is.EqualTo(1));
        Assert.That(result.cost, Is.EqualTo(1));
        Assert.That(result.baseCardName, Is.EqualTo("测试卡"));
        Assert.That(source.cost, Is.EqualTo(2));
        Assert.That(deck.backpack[0], Is.SameAs(result));

        Object.DestroyImmediate(result);
        Object.DestroyImmediate(source);
    }

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
        CardData handCard = CreateCard("Hand", 1);
        deck.hand.Add(handCard);
        CollectibleData collectible = CollectibleManager.CreateMaxHealthCollectible();
        CollectibleManager.AddCollectible(collectible);

        deck.ResetForNewRun();
        CollectibleManager.ResetForNewRun();

        Assert.That(deck.backpack, Is.Empty);
        Assert.That(deck.drawPile, Is.Empty);
        Assert.That(deck.hand, Is.Empty);
        Assert.That(deck.discardPile, Is.Empty);
        Assert.That(deck.exhaustPile, Is.Empty);
        Assert.That(CollectibleManager.OwnedCollectibles, Is.Empty);
        Assert.That(upgraded == null, Is.True);
        Assert.That(collectible == null, Is.True);
        Object.DestroyImmediate(source);
        Object.DestroyImmediate(handCard);
    }

    [Test]
    public void DeckResetForNewRunDestroysAnUpgradedCardRemovedBeforeTheReset()
    {
        DeckManager deck = CreateDeckManager();
        CardData source = CreateCard("Removed upgraded source", 2);
        deck.backpack.Add(source);
        Assert.That(deck.TryUpgradeRandomBackpackCard(out CardData upgraded), Is.True);
        Assert.That(deck.RemoveRandomBackpackCard(out CardData removed), Is.True);
        Assert.That(removed, Is.SameAs(upgraded));

        deck.ResetForNewRun();

        Assert.That(upgraded == null, Is.True);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void DeckResetForNewRunPreservesNonRuntimeCardsMarkedAsUpgraded()
    {
        DeckManager deck = CreateDeckManager();
        CardData authoredCard = CreateCard("Authored upgraded card", 1);
        authoredCard.upgradeLevel = 1;
        deck.backpack.Add(authoredCard);

        deck.ResetForNewRun();

        Assert.That(authoredCard == null, Is.False);
        Object.DestroyImmediate(authoredCard);
    }

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

    [Test]
    public void NewRunResetsCardEffectStateFromThePreviousRun()
    {
        RoguelikeRunController run = CreateActiveRun(out _);
        GameObject effectObject = new GameObject("RunCardEffects");
        CardEffectManager effectManager = effectObject.AddComponent<CardEffectManager>();
        CardEffectManager.Instance = effectManager;
        effectManager.doubleChantUseCount = 2;
        effectManager.shiningGloryDamageReduction = 20;
        effectManager.nextCardDoubleEffect = true;

        Assert.That(run.StartNewRun(), Is.True);

        Assert.That(effectManager.doubleChantUseCount, Is.Zero);
        Assert.That(effectManager.shiningGloryDamageReduction, Is.Zero);
        Assert.That(effectManager.nextCardDoubleEffect, Is.False);
    }

    [Test]
    public void RunResultButtonsStartANewRunOrRequestTheMainMenu()
    {
        RoguelikeRunController run = CreateActiveRun(out _);
        Assert.That(run.CompleteRun(), Is.True);

        Button newRunButton = GetResultButton(run.resultPanel, "newRunButton");
        Button returnToMenuButton = GetResultButton(run.resultPanel, "returnToMenuButton");

        Assert.That(newRunButton, Is.Not.Null);
        Assert.That(returnToMenuButton, Is.Not.Null);

        newRunButton.onClick.Invoke();
        Assert.That(run.CurrentResult, Is.EqualTo(RoguelikeRunResult.None));
        Assert.That(run.resultPanel.IsVisible, Is.False);

        Assert.That(run.CompleteRun(), Is.True);
        returnToMenuButton.onClick.Invoke();
        Assert.That(run.LastRequestedSceneName, Is.EqualTo("start"));
    }

    [Test]
    public void CardCombatDefeatEndsTheRunWithoutGrantingAReward()
    {
        RoguelikeRunController run = CreateActiveRun(out _);
        combatObject = new GameObject("DefeatCombat");
        CombatController combat = combatObject.AddComponent<CombatController>();
        combat.autoBuildUI = false;
        combat.StartCombat();

        Assert.That(combat.ResolvePlayerDefeat(), Is.True);
        Assert.That(run.CurrentResult, Is.EqualTo(RoguelikeRunResult.Defeat));
        Assert.That(combat.ResolvePlayerDefeat(), Is.False);
    }

    [Test]
    public void BossNodeCompletionShowsRoguelikeVictoryWithoutLegacyScene()
    {
        RoguelikeRunController run = CreateActiveRun(out GameManager game);
        Node boss = CreateActiveNode(NodeType.Boss);
        firstNodeObject = boss.gameObject;
        game.SelectNode(boss);
        int session = game.CurrentContentSession;

        Assert.That(game.TryCompleteCurrentNode(boss, session), Is.True);
        Assert.That(run.CurrentResult, Is.EqualTo(RoguelikeRunResult.Victory));
        Assert.That(run.LastRequestedSceneName, Is.Null);
    }

    [Test]
    public void BossNodeStartsOneCardBossAndCompletesTheRunWithoutCardRewards()
    {
        RoguelikeRunController run = CreateActiveRun(out GameManager game);
        Node boss = CreateActiveNode(NodeType.Boss);
        firstNodeObject = boss.gameObject;
        game.SelectNode(boss);

        CombatController bossCombat = Object.FindObjectOfType<CombatController>();
        Assert.That(bossCombat, Is.Not.Null);
        Assert.That(bossCombat.BoundNode, Is.SameAs(boss));
        bossCombat.autoBuildUI = false;
        bossCombat.StartCombat();

        Assert.That(bossCombat.isBossBattle, Is.True);
        Assert.That(bossCombat.showCardRewardOnVictory, Is.False);
        Assert.That(EnemyManager.Instance.ActiveEnemies.Count, Is.EqualTo(1));
        Enemy bossEnemy = EnemyManager.Instance.ActiveEnemies[0];
        Assert.That(bossEnemy.maxHealth, Is.EqualTo(320));
        Assert.That(bossEnemy.baseAttack, Is.EqualTo(26));

        bossCombat.CompleteCombat();

        Assert.That(run.CurrentResult, Is.EqualTo(RoguelikeRunResult.Victory));
        Assert.That(Object.FindObjectOfType<RewardChoiceUI>(), Is.Null);
    }

    [Test]
    public void EnemyTurnDefeatShowsTheRunResultThroughTheCombatFlow()
    {
        RoguelikeRunController run = CreateActiveRun(out GameManager game);
        Node battle = CreateActiveNode(NodeType.Battle);
        firstNodeObject = battle.gameObject;
        game.SelectNode(battle);

        CombatController combat = Object.FindObjectOfType<CombatController>();
        Assert.That(combat, Is.Not.Null);
        combat.autoBuildUI = false;
        combat.StartCombat();
        PlayerStats.Instance.SetHealth(1);

        combat.RequestEndPlayerTurn();

        Assert.That(run.CurrentResult, Is.EqualTo(RoguelikeRunResult.Defeat));
        Assert.That(run.resultPanel.IsVisible, Is.True);
    }

    [Test]
    public void CompletingASelectedNodeIsIdempotent()
    {
        generatorObject = new GameObject("RuntimeMapForCompletion");
        MapGenerator generator = generatorObject.AddComponent<MapGenerator>();
        generator.totalDepth = 0;
        generator.nodesPerLayer = 1;
        generator.nodeTemplate = null;
        generator.mapContainer = null;
        generator.nodeVisualizer = null;
        generator.GenerateMap();

        contentManagerObject = new GameObject("RuntimeContentManager");
        contentManagerObject.AddComponent<NodeContentManager>();

        gameManagerObject = new GameObject("RuntimeGameManager");
        GameManager manager = gameManagerObject.AddComponent<GameManager>();
        manager.mapGenerator = generator;
        manager.contentManager = contentManagerObject.GetComponent<NodeContentManager>();

        Node camp = generator.GetNodesByDepth()[0][0];
        manager.SelectNode(camp);

        Assert.That(manager.TryCompleteCurrentNode(camp), Is.True);
        Assert.That(manager.TryCompleteCurrentNode(camp), Is.False);
        Assert.That(manager.CurrentNodeCompleted, Is.True);
    }

    [Test]
    public void StaleContentSessionCannotCompleteTheNextNode()
    {
        generatorObject = new GameObject("RuntimeMapForSession");
        MapGenerator generator = generatorObject.AddComponent<MapGenerator>();
        generator.totalDepth = 1;
        generator.nodesPerLayer = 1;
        generator.nodeTemplate = null;
        generator.mapContainer = null;
        generator.nodeVisualizer = null;
        generator.GenerateMap();

        contentManagerObject = new GameObject("RuntimeContentForSession");
        contentManagerObject.AddComponent<NodeContentManager>();
        gameManagerObject = new GameObject("RuntimeGameManagerForSession");
        GameManager manager = gameManagerObject.AddComponent<GameManager>();
        manager.mapGenerator = generator;
        manager.contentManager = contentManagerObject.GetComponent<NodeContentManager>();

        Node camp = generator.GetNodesByDepth()[0][0];
        manager.SelectNode(camp);
        int firstSession = manager.CurrentContentSession;
        Assert.That(manager.TryCompleteCurrentNode(camp, firstSession), Is.True);

        Node next = generator.GetNodesByDepth()[1][0];
        manager.SelectNode(next);
        int secondSession = manager.CurrentContentSession;

        Assert.That(secondSession, Is.GreaterThan(firstSession));
        Assert.That(manager.TryCompleteCurrentNode(next, firstSession), Is.False);
        Assert.That(manager.CurrentNodeCompleted, Is.False);
    }

    [Test]
    public void RewardSelectionLocksBeforeApplyingASecondCard()
    {
        deckObject = new GameObject("RewardDeck");
        DeckManager deck = deckObject.AddComponent<DeckManager>();
        DeckManager.Instance = deck;
        rewardCard = ScriptableObject.CreateInstance<CardData>();
        rewardCard.cardName = "奖励卡";
        deck.hiddenCardPool.Add(rewardCard);

        rewardObject = new GameObject("RewardController");
        RewardChoiceUI reward = rewardObject.AddComponent<RewardChoiceUI>();
        MethodInfo pick = typeof(RewardChoiceUI).GetMethod("PickSingleCard", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(pick, Is.Not.Null);

        pick.Invoke(reward, new object[] { rewardCard });
        pick.Invoke(reward, new object[] { rewardCard });

        Assert.That(deck.backpack.Count, Is.EqualTo(1));
    }

    private static void Destroy(GameObject target)
    {
        if (target != null)
            Object.DestroyImmediate(target);
    }

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

    private static CardData CreateCard(string cardName, int cost)
    {
        CardData card = ScriptableObject.CreateInstance<CardData>();
        card.cardName = cardName;
        card.cost = cost;
        return card;
    }

    private static Button GetResultButton(RoguelikeResultPanel panel, string fieldName)
    {
        FieldInfo field = typeof(RoguelikeResultPanel).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return field.GetValue(panel) as Button;
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
        GameManager.Instance = game;
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
}
