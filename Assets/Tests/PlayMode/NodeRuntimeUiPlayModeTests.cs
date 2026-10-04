using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class NodeRuntimeUiPlayModeTests
{
    private GameObject gameObject;
    private GameObject nodeObject;
    private readonly List<CardData> cards = new List<CardData>();
    private GameManager game;
    private NodeContentManager contentManager;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EventPool.ResetForNewRun();
        gameObject = new GameObject("Runtime Game");
        game = gameObject.AddComponent<GameManager>();

        // GameManager.Start initializes a run and clears node content. Let it
        // complete before selecting the test node so it cannot invalidate UI
        // created by a test on the first yielded frame.
        yield return null;

        contentManager = game.contentManager;
        Assert.That(contentManager, Is.Not.Null);

        Assert.That(DeckManager.Instance, Is.Not.Null);
        Assert.That(PlayerStats.Instance, Is.Not.Null);
        DeckManager.Instance.ResetForNewRun();
        PlayerStats.Instance.ResetForNewRun();
        PlayerStats.Instance.gold = 500;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (contentManager != null)
            contentManager.ClearCurrentContent();
        yield return null;

        DestroyObject(nodeObject);
        foreach (CardData card in cards)
            if (card != null) Object.Destroy(card);
        cards.Clear();

        if (DeckManager.Instance != null)
            DestroyObject(DeckManager.Instance.gameObject);
        if (PlayerStats.Instance != null)
            DestroyObject(PlayerStats.Instance.gameObject);
        DestroyObject(gameObject);

        CollectibleManager.ResetForNewRun();
        EventPool.ResetForNewRun();
        DeckManager.Instance = null;
        PlayerStats.Instance = null;
        GameManager.Instance = null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator EventFallbackBuildsChoicesAndContinueCompletesNode()
    {
        CreateNodeRuntime(NodeType.Event, 0);
        yield return null;

        EventManager manager = Object.FindObjectOfType<EventManager>();
        Assert.That(manager, Is.Not.Null);
        Assert.That(manager.eventPool.Count, Is.EqualTo(20));
        Assert.That(manager.titleText.text, Is.Not.Empty);
        Assert.That(manager.descriptionText.text, Is.Not.Empty);
        Assert.That(manager.choiceButtonContainer, Is.Not.Null);
        Assert.That(manager.choiceButtonContainer.GetComponent<RectTransform>(), Is.Not.Null);
        Button[] buttons = manager.choiceButtonContainer.GetComponentsInChildren<Button>();
        Assert.That(buttons.Length, Is.EqualTo(manager.CurrentEvent.choices.Count));
        Canvas.ForceUpdateCanvases();
        AssertButtonsHaveUsableLayout(manager.choiceButtonContainer, buttons, 48f);
        Assert.That(CountNamedChildren(manager.gameObject, "EventPanel"), Is.EqualTo(1));
        AssertRuntimeCanvas(manager.gameObject);

        manager.choiceButtonContainer.GetComponentInChildren<Button>().onClick.Invoke();
        yield return null;
        Assert.That(manager.resultPanel.activeSelf, Is.True);
        manager.continueButton.onClick.Invoke();
        yield return null;
        Assert.That(game.CurrentNodeCompleted, Is.True);
        Assert.That(contentManager.HasCurrentContent, Is.False);
    }

    [UnityTest]
    public IEnumerator RewardFallbackLetsPlayerClaimCardAndCompletesNode()
    {
        CreateNodeRuntime(NodeType.Treasure, 1);
        yield return null;

        RewardChoiceUI reward = Object.FindObjectOfType<RewardChoiceUI>();
        Assert.That(reward, Is.Not.Null);
        Assert.That(reward.choiceContainer.GetComponent<RectTransform>(), Is.Not.Null);
        Button[] buttons = reward.choiceContainer.GetComponentsInChildren<Button>();
        Assert.That(buttons.Length, Is.EqualTo(1));
        Canvas.ForceUpdateCanvases();
        AssertButtonsHaveUsableLayout(reward.choiceContainer, buttons, 64f);
        buttons[0].onClick.Invoke();
        yield return null;

        Assert.That(DeckManager.Instance.backpack.Count, Is.EqualTo(1));
        Assert.That(game.CurrentNodeCompleted, Is.True);
        Assert.That(contentManager.HasCurrentContent, Is.False);
    }

    [UnityTest]
    public IEnumerator RewardFallbackSupportsFourChoiceTwoPickEliteConfiguration()
    {
        CreateNodeRuntime(NodeType.Treasure, 4);
        RewardChoiceUI reward = Object.FindObjectOfType<RewardChoiceUI>();
        Assert.That(reward, Is.Not.Null);

        // CombatController configures elite victory rewards as four choices
        // with two picks. Set those values before RewardChoiceUI.Start runs.
        reward.choiceCount = 4;
        reward.maxPicks = 2;
        reward.title = "精英奖励";
        yield return null;

        Button[] buttons = reward.choiceContainer.GetComponentsInChildren<Button>();
        Assert.That(buttons.Length, Is.EqualTo(4));
        Canvas.ForceUpdateCanvases();
        AssertButtonsHaveUsableLayout(reward.choiceContainer, buttons, 64f);

        buttons[0].onClick.Invoke();
        buttons[1].onClick.Invoke();
        yield return null;
        Assert.That(game.CurrentNodeCompleted, Is.False);

        reward.skipButton.onClick.Invoke();
        yield return null;
        Assert.That(DeckManager.Instance.backpack.Count, Is.EqualTo(2));
        Assert.That(game.CurrentNodeCompleted, Is.True);
        Assert.That(contentManager.HasCurrentContent, Is.False);
    }

    [UnityTest]
    public IEnumerator ShopFallbackBuildsEightOffersLetsPlayerBuyAndLeave()
    {
        CreateNodeRuntime(NodeType.Shop, 4);
        yield return null;

        ShopManager shop = Object.FindObjectOfType<ShopManager>();
        Assert.That(shop, Is.Not.Null);
        Assert.That(shop.offerContainer.GetComponent<RectTransform>(), Is.Not.Null);
        Button[] buttons = shop.offerContainer.GetComponentsInChildren<Button>();
        Assert.That(buttons.Length, Is.EqualTo(8));
        Canvas.ForceUpdateCanvases();
        AssertButtonsHaveUsableLayout(shop.offerContainer, buttons, 62f);
        AssertRuntimeCanvas(shop.gameObject);
        RectTransform offersRect = shop.offerContainer.GetComponent<RectTransform>();
        RectTransform leaveRect = shop.leaveButton.GetComponent<RectTransform>();
        RectTransform panelRect = offersRect.parent as RectTransform;
        Bounds titleBounds = GetBoundsInContainer(panelRect, shop.titleText.rectTransform);
        foreach (Button button in buttons)
        {
            Bounds offerBounds = GetBoundsInContainer(panelRect, button.transform as RectTransform);
            Assert.That(offerBounds.max.y, Is.LessThan(titleBounds.min.y),
                "Shop offers must not cover the title or its gold count.");
        }
        Assert.That(offersRect.anchoredPosition.y - offersRect.sizeDelta.y / 2f,
            Is.GreaterThan(leaveRect.anchoredPosition.y + leaveRect.sizeDelta.y / 2f));

        buttons[0].onClick.Invoke();
        yield return null;
        Assert.That(DeckManager.Instance.backpack.Count, Is.EqualTo(1));

        shop.leaveButton.onClick.Invoke();
        yield return null;
        Assert.That(game.CurrentNodeCompleted, Is.True);
        Assert.That(contentManager.HasCurrentContent, Is.False);
    }

    private void CreateNodeRuntime(NodeType type, int cardCount)
    {
        DeckManager deck = DeckManager.Instance;
        deck.ResetForNewRun();
        deck.hiddenCardPool.Clear();
        for (int i = 0; i < cardCount; i++)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.cardName = "Runtime Card " + i;
            card.cost = 1;
            cards.Add(card);
            deck.hiddenCardPool.Add(card);
        }

        PlayerStats.Instance.ResetForNewRun();
        PlayerStats.Instance.gold = 500;

        nodeObject = new GameObject("Runtime Node");
        Node node = nodeObject.AddComponent<Node>();
        node.type = type;
        node.isActive = true;
        game.SelectNode(node);
    }

    private static void AssertRuntimeCanvas(GameObject uiObject)
    {
        CanvasScaler scaler = uiObject.GetComponentInChildren<CanvasScaler>();
        Assert.That(scaler, Is.Not.Null);
        Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1280f, 720f)));
        Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f));
    }

    private static int CountNamedChildren(GameObject root, string name)
    {
        int count = 0;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) count++;
        return count;
    }

    private static void AssertButtonsHaveUsableLayout(Transform container, Button[] buttons, float expectedHeight)
    {
        RectTransform containerRect = container as RectTransform;
        Assert.That(containerRect, Is.Not.Null);

        List<Bounds> buttonBounds = new List<Bounds>();
        foreach (Button button in buttons)
        {
            RectTransform buttonRect = button.transform as RectTransform;
            Assert.That(buttonRect, Is.Not.Null);
            Assert.That(buttonRect.rect.width, Is.GreaterThan(0f));
            Assert.That(buttonRect.rect.height, Is.EqualTo(expectedHeight).Within(0.1f));

            Bounds bounds = GetBoundsInContainer(containerRect, buttonRect);
            Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(containerRect.rect.xMin - 0.1f));
            Assert.That(bounds.max.x, Is.LessThanOrEqualTo(containerRect.rect.xMax + 0.1f));
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(containerRect.rect.yMin - 0.1f));
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(containerRect.rect.yMax + 0.1f));
            buttonBounds.Add(bounds);
        }

        for (int i = 0; i < buttonBounds.Count; i++)
            for (int j = i + 1; j < buttonBounds.Count; j++)
                Assert.That(buttonBounds[i].max.y <= buttonBounds[j].min.y || buttonBounds[j].max.y <= buttonBounds[i].min.y,
                    Is.True, "Runtime layout must not overlap interactive buttons.");
    }

    private static Bounds GetBoundsInContainer(RectTransform container, RectTransform child)
    {
        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners);
        Bounds bounds = new Bounds(container.InverseTransformPoint(corners[0]), Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
            bounds.Encapsulate(container.InverseTransformPoint(corners[i]));
        return bounds;
    }

    private static void DestroyObject(GameObject target)
    {
        if (target != null) Object.Destroy(target);
    }
}
