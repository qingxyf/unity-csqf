using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class RiceCampEventChainPlayModeTests
{
    private GameObject gameObject;
    private GameManager game;
    private readonly List<GameObject> nodes = new List<GameObject>();

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EventPool.ResetForNewRun();
        CollectibleManager.ResetForNewRun();
        gameObject = new GameObject("Rice event test game");
        game = gameObject.AddComponent<GameManager>();
        yield return null;
        PlayerStats.Instance.ResetForNewRun();
        DeckManager.Instance.ResetForNewRun();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (game != null && game.contentManager != null) game.contentManager.ClearCurrentContent();
        yield return null;
        foreach (GameObject node in nodes) if (node != null) Object.Destroy(node);
        nodes.Clear();
        if (EnemyManager.Instance != null) Object.Destroy(EnemyManager.Instance.gameObject);
        if (CardEffectManager.Instance != null) Object.Destroy(CardEffectManager.Instance.gameObject);
        if (DeckManager.Instance != null) Object.Destroy(DeckManager.Instance.gameObject);
        if (PlayerStats.Instance != null) Object.Destroy(PlayerStats.Instance.gameObject);
        if (gameObject != null) Object.Destroy(gameObject);
        EnemyManager.Instance = null;
        CardEffectManager.Instance = null;
        DeckManager.Instance = null;
        PlayerStats.Instance = null;
        GameManager.Instance = null;
        EventPool.ResetForNewRun();
        CollectibleManager.ResetForNewRun();
        yield return null;
    }

    [UnityTest]
    public IEnumerator SmallMealDoesNotUnlockAndAnExhaustedPoolWaitsForContinue()
    {
        PlayerStats.Instance.currentHealth = 40;
        EventManager camp = EnterEvent("Event_野生营地");
        yield return null;
        AssertEventArtDoesNotOverlapTextOrChoices(camp);
        Button firstChoice = Choices(camp)[0];
        firstChoice.onClick.Invoke();
        firstChoice.onClick.Invoke();
        Assert.That(PlayerStats.Instance.currentHealth, Is.EqualTo(55));
        Assert.That(PlayerStats.Instance.maxHealth, Is.EqualTo(100));
        Assert.That(EventPool.IsUnlocked("rice-owner-reckoning"), Is.False);
        camp.continueButton.onClick.Invoke();
        yield return null;
        Assert.That(game.CurrentNodeCompleted, Is.True);

        EventManager exhausted = EnterEvent("Event_野生营地");
        yield return null;
        Assert.That(exhausted.CurrentEvent, Is.Null);
        Assert.That(exhausted.titleText.text, Is.EqualTo("静谧的路口"));
        Assert.That(exhausted.choiceButtonContainer.gameObject.activeSelf, Is.False);
        Assert.That(exhausted.resultPanel.activeSelf, Is.True);
        Assert.That(game.CurrentNodeCompleted, Is.False);
        exhausted.continueButton.onClick.Invoke();
        yield return null;
        Assert.That(game.CurrentNodeCompleted, Is.True);
    }

    [UnityTest]
    public IEnumerator FullMealUnlocksReckoningAndPaymentConsumesItOnlyOnce()
    {
        PlayerStats.Instance.currentHealth = 40;
        PlayerStats.Instance.gold = 123;
        EventManager camp = EnterEvent("Event_野生营地");
        yield return null;
        Assert.That(Choices(camp)[1].GetComponentInChildren<TMPro.TextMeshProUGUI>().text,
            Does.Contain("新的走向"));
        Choices(camp)[1].onClick.Invoke();
        Assert.That(PlayerStats.Instance.currentHealth, Is.EqualTo(78));
        Assert.That(PlayerStats.Instance.maxHealth, Is.EqualTo(108));
        Assert.That(EventPool.IsUnlocked("rice-owner-reckoning"), Is.True);
        Assert.That(camp.resultText.text, Does.Contain("新的走向"));
        camp.continueButton.onClick.Invoke();
        yield return null;

        EventManager reckoning = EnterEvent("Event_大白饭的讨债人");
        yield return null;
        AssertEventArtDoesNotOverlapTextOrChoices(reckoning);
        Assert.That(reckoning.CurrentEvent.eventId, Is.EqualTo("rice-owner-reckoning"));
        Button pay = Choices(reckoning)[0];
        pay.onClick.Invoke();
        Assert.That(PlayerStats.Instance.gold, Is.Zero);
        PlayerStats.Instance.GainGold(7);
        pay.onClick.Invoke();
        Assert.That(PlayerStats.Instance.gold, Is.EqualTo(7), "A stale choice must not collect money twice.");
        Assert.That(reckoning.ActiveEventCombat, Is.Null);
        reckoning.continueButton.onClick.Invoke();
        yield return null;
        Assert.That(game.CurrentNodeCompleted, Is.True);
        Assert.That(EventPool.Draw(new List<EventData> { LoadEvent("Event_野生营地"), LoadEvent("Event_大白饭的讨债人") }), Is.Null);
    }

    [UnityTest]
    public IEnumerator PaymentAlsoWorksWithAnEmptyWallet()
    {
        EventPool.UnlockEvent("rice-owner-reckoning");
        PlayerStats.Instance.gold = 0;
        EventManager reckoning = EnterEvent("Event_大白饭的讨债人");
        yield return null;
        Choices(reckoning)[0].onClick.Invoke();
        Assert.That(PlayerStats.Instance.gold, Is.Zero);
        Assert.That(reckoning.resultPanel.activeSelf, Is.True);
        reckoning.continueButton.onClick.Invoke();
        yield return null;
        Assert.That(game.CurrentNodeCompleted, Is.True);
    }

    [UnityTest]
    public IEnumerator ChallengeKeepsItsEventSessionUntilTheVictoryRewardCompletes()
    {
        EventPool.UnlockEvent("rice-owner-reckoning");
        EventManager reckoning = EnterEvent("Event_大白饭的讨债人");
        yield return null;
        Node originalNode = game.CurrentNode;
        int originalSession = game.CurrentContentSession;
        Button challenge = Choices(reckoning)[1];
        challenge.onClick.Invoke();
        challenge.onClick.Invoke();
        yield return null;
        yield return null;

        CombatController combat = reckoning.ActiveEventCombat;
        Assert.That(combat, Is.Not.Null);
        Assert.That(reckoning.GetComponentsInChildren<CombatController>(true), Has.Length.EqualTo(1));
        Assert.That(combat.BoundNode, Is.SameAs(originalNode));
        Assert.That(combat.BoundSessionToken, Is.EqualTo(originalSession));
        Assert.That(game.CurrentNode, Is.SameAs(originalNode));
        Assert.That(game.CurrentNodeCompleted, Is.False);
        Assert.That(combat.isBossBattle, Is.False);
        Assert.That(combat.isEliteBattle, Is.True);
        Assert.That(combat.GetComponentInChildren<HandView>(), Is.Not.Null);
        Enemy[] enemies = combat.GetComponentsInChildren<Enemy>();
        Assert.That(enemies, Has.Length.EqualTo(1));
        Enemy challenger = enemies[0];
        Assert.That(challenger.enemyName, Is.EqualTo("蓝汐·米饭守护者"));
        Assert.That(challenger.maxHealth, Is.EqualTo(240));
        Assert.That(challenger.currentHealth, Is.EqualTo(240));
        Assert.That(challenger.baseAttack, Is.EqualTo(24));
        Assert.That(challenger.currentShield, Is.EqualTo(20));
        Assert.That(challenger.GetComponent<RoguelikeEnemyPresentation>().idleSprite,
            Is.SameAs(Resources.Load<GameObject>("Enemies/RiceKeeper").GetComponent<RoguelikeEnemyPresentation>().idleSprite));
        Assert.That(reckoning.continueButton.interactable, Is.False);
        reckoning.continueButton.onClick.Invoke();
        Assert.That(game.CurrentNodeCompleted, Is.False, "The hidden event continue callback cannot bypass combat.");

        int previousGold = PlayerStats.Instance.gold;
        challenger.TakeDamage(9999);
        combat.RequestEndPlayerTurn();
        yield return null;
        yield return null;
        Assert.That(combat.AcceptsPlayerActions, Is.False);
        RewardChoiceUI reward = combat.GetComponentInChildren<RewardChoiceUI>();
        Assert.That(reward, Is.Not.Null);
        Assert.That(reward.BoundNode, Is.SameAs(originalNode));
        Assert.That(PlayerStats.Instance.gold, Is.EqualTo(previousGold + 35));
        Assert.That(CollectibleManager.OwnedCollectibles.Count, Is.EqualTo(1));
        Assert.That(game.CurrentNodeCompleted, Is.False);
        reward.skipButton.onClick.Invoke();
        yield return null;
        Assert.That(game.CurrentNodeCompleted, Is.True);
        Assert.That(game.contentManager.HasCurrentContent, Is.False);
    }

    [UnityTest]
    public IEnumerator OldUnselectedChoiceCannotUnlockTheChainAfterRestart()
    {
        EventManager camp = EnterEvent("Event_野生营地");
        yield return null;
        Button staleChoice = Choices(camp)[1];
        Assert.That(game.RunController.StartNewRun(), Is.True);
        // Destroy is deferred until frame end, so invoke the old callback in
        // precisely the window where its object still exists.
        staleChoice.onClick.Invoke();
        Assert.That(EventPool.IsUnlocked("rice-owner-reckoning"), Is.False);
        Assert.That(PlayerStats.Instance.maxHealth, Is.EqualTo(100));
        Assert.That(EventPool.Draw(new List<EventData> { LoadEvent("Event_野生营地") }), Is.Not.Null);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ChallengeDefeatEndsTheRunAndRestartResetsBothChainAndCombat()
    {
        EventPool.UnlockEvent("rice-owner-reckoning");
        EventManager reckoning = EnterEvent("Event_大白饭的讨债人");
        yield return null;
        Choices(reckoning)[1].onClick.Invoke();
        yield return null;
        yield return null;
        PlayerStats.Instance.currentHealth = 1;
        PlayerStats.Instance.currentShield = 0;
        reckoning.ActiveEventCombat.GetComponentInChildren<Enemy>().AttackPlayer();
        yield return null;
        yield return null;
        Assert.That(game.RunController.CurrentResult, Is.EqualTo(RoguelikeRunResult.Defeat));
        Assert.That(game.CurrentNodeCompleted, Is.False);
        Assert.That(game.contentManager.HasCurrentContent, Is.False);
        Assert.That(game.RunController.StartNewRun(), Is.True);
        yield return null;
        Assert.That(EventPool.IsUnlocked("rice-owner-reckoning"), Is.False);
        Assert.That(Object.FindObjectsOfType<CombatController>(), Is.Empty);
        Assert.That(EventPool.Draw(new List<EventData> { LoadEvent("Event_野生营地") }), Is.Not.Null);
        Assert.That(EventPool.Draw(new List<EventData> { LoadEvent("Event_大白饭的讨债人") }), Is.Null);
    }

    private EventManager EnterEvent(string resourceName)
    {
        GameObject nodeObject = new GameObject("Rice chain event node");
        nodes.Add(nodeObject);
        Node node = nodeObject.AddComponent<Node>();
        node.type = NodeType.Event;
        node.isActive = true;
        game.SelectNode(node);
        EventManager manager = Object.FindObjectOfType<EventManager>();
        Assert.That(manager, Is.Not.Null);
        manager.autoLoadResources = false;
        manager.eventPool = new List<EventData> { LoadEvent(resourceName) };
        return manager;
    }

    private static EventData LoadEvent(string resourceName)
    {
        EventData evt = Resources.Load<EventData>("Events/" + resourceName);
        Assert.That(evt, Is.Not.Null, resourceName);
        return evt;
    }

    private static Button[] Choices(EventManager manager)
    {
        return manager.choiceButtonContainer.GetComponentsInChildren<Button>();
    }

    private static void AssertEventArtDoesNotOverlapTextOrChoices(EventManager manager)
    {
        Canvas.ForceUpdateCanvases();
        Assert.That(manager.illustrationImage, Is.Not.Null);
        Assert.That(manager.illustrationImage.sprite, Is.Not.Null);
        Assert.That(manager.illustrationImage.gameObject.activeSelf, Is.True);
        Assert.That(manager.illustrationImage.preserveAspect, Is.True);
        RectTransform panel = manager.illustrationImage.transform.parent as RectTransform;
        Bounds art = BoundsIn(panel, manager.illustrationImage.rectTransform);
        Bounds text = BoundsIn(panel, manager.descriptionText.rectTransform);
        Bounds choices = BoundsIn(panel, manager.choiceButtonContainer as RectTransform);
        Bounds title = BoundsIn(panel, manager.titleText.rectTransform);
        Assert.That(art.max.x, Is.LessThan(text.min.x));
        Assert.That(choices.max.y, Is.LessThan(art.min.y));
        Assert.That(art.max.y, Is.LessThan(title.min.y));
        Assert.That(choices.min.y, Is.GreaterThanOrEqualTo(panel.rect.yMin));
    }

    private static Bounds BoundsIn(RectTransform panel, RectTransform child)
    {
        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners);
        Bounds result = new Bounds(panel.InverseTransformPoint(corners[0]), Vector3.zero);
        for (int i = 1; i < corners.Length; i++) result.Encapsulate(panel.InverseTransformPoint(corners[i]));
        return result;
    }
}
