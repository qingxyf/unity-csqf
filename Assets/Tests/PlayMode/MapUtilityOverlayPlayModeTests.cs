using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class MapUtilityOverlayPlayModeTests
{
    private GameObject legacyPanel;
    private GameObject deckObject;
    private GameObject eventObject;
    private readonly List<CardData> cards = new List<CardData>();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (legacyPanel != null) Object.Destroy(legacyPanel);
        if (deckObject != null) Object.Destroy(deckObject);
        if (eventObject != null) Object.Destroy(eventObject);
        foreach (CardData card in cards)
            if (card != null) Object.Destroy(card);
        cards.Clear();
        DeckManager.Instance = null;
        yield return null;
        // OnDestroy also retires the independently owned overlay.
        yield return null;
    }

    [UnityTest]
    public IEnumerator InitiallyInactiveBackpackOpensReadableScrollableInventoryAndCanReopen()
    {
        deckObject = new GameObject("Utility Test Deck");
        DeckManager deck = deckObject.AddComponent<DeckManager>();
        deck.backpack.Clear();
        for (int i = 0; i < 18; i++)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.cardName = "测试卡牌 " + i;
            card.element = CardElement.Fire;
            card.cost = 2;
            card.description = "造成 12 点伤害";
            cards.Add(card);
            deck.backpack.Add(card);
        }
        legacyPanel = new GameObject("Legacy Backpack Panel");
        BackpackDisplay display = legacyPanel.AddComponent<BackpackDisplay>();
        display.backpackPanel = legacyPanel;
        legacyPanel.SetActive(false);

        display.ToggleBackpack();
        yield return null;
        Assert.That(display.IsOpen, Is.True);
        GameObject overlay = GameObject.Find("背包覆盖层");
        Assert.That(overlay, Is.Not.Null);
        Canvas.ForceUpdateCanvases();
        AssertBodyFitsCanvas(overlay);
        ScrollRect scroll = overlay.GetComponentInChildren<ScrollRect>();
        Assert.That(scroll.content.childCount, Is.EqualTo(18));
        Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
        string firstText = scroll.content.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text;
        Assert.That(firstText, Does.Contain("测试卡牌 0"));
        Assert.That(firstText, Does.Contain("[火]"));
        Assert.That(firstText, Does.Contain("费用 2"));
        Assert.That(firstText, Does.Contain("12"));
        AssertOverlayBlocksMap(overlay);

        scroll.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
        yield return null;
        AssertRectFits((RectTransform)scroll.content.GetChild(17), scroll.viewport);

        overlay.transform.Find("Body/关闭").GetComponent<Button>().onClick.Invoke();
        yield return null;
        Assert.That(display.IsOpen, Is.False);
        display.ToggleBackpack();
        yield return null;
        Assert.That(display.IsOpen, Is.True);
        Assert.That(GameObject.Find("背包覆盖层"), Is.SameAs(overlay));
        Assert.That(scroll.content.childCount, Is.EqualTo(18));
    }

    [UnityTest]
    public IEnumerator InitiallyInactiveRulesOpenFullScrollableTextAndCanClose()
    {
        legacyPanel = new GameObject("Legacy Rules Panel");
        RulesDisplay display = legacyPanel.AddComponent<RulesDisplay>();
        display.rulesPanel = legacyPanel;
        display.rulesContent = string.Join("\n", new string[80]).Replace("\n", "\n完整规则说明");
        legacyPanel.SetActive(false);
        display.ToggleRules();
        yield return null;

        Assert.That(display.IsOpen, Is.True);
        GameObject overlay = GameObject.Find("规则覆盖层");
        Assert.That(overlay, Is.Not.Null);
        Canvas.ForceUpdateCanvases();
        AssertBodyFitsCanvas(overlay);
        ScrollRect scroll = overlay.GetComponentInChildren<ScrollRect>();
        TextMeshProUGUI text = scroll.content.GetComponentInChildren<TextMeshProUGUI>();
        Assert.That(text.text, Is.EqualTo(display.rulesContent));
        Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
        Assert.That(text.rectTransform.rect.height, Is.GreaterThanOrEqualTo(text.preferredHeight - 0.1f));
        overlay.transform.Find("Body/关闭").GetComponent<Button>().onClick.Invoke();
        yield return null;
        Assert.That(display.IsOpen, Is.False);
    }

    private void AssertOverlayBlocksMap(GameObject overlay)
    {
        eventObject = new GameObject("Utility Test Events", typeof(EventSystem));
        EventSystem events = eventObject.GetComponent<EventSystem>();
        PointerEventData pointer = new PointerEventData(events)
        {
            position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
        };
        List<RaycastResult> hits = new List<RaycastResult>();
        events.RaycastAll(pointer, hits);
        Assert.That(hits.Count, Is.GreaterThan(0));
        Assert.That(hits[0].gameObject == overlay || hits[0].gameObject.transform.IsChildOf(overlay.transform), Is.True);
        Assert.That(overlay.GetComponent<Image>().raycastTarget, Is.True);
    }

    private static void AssertBodyFitsCanvas(GameObject overlay)
    {
        AssertRectFits((RectTransform)overlay.transform.Find("Body"), overlay.GetComponent<RectTransform>());
    }

    private static void AssertRectFits(RectTransform child, RectTransform container)
    {
        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners);
        foreach (Vector3 corner in corners)
        {
            Vector3 local = container.InverseTransformPoint(corner);
            Assert.That(local.x, Is.InRange(container.rect.xMin - 0.1f, container.rect.xMax + 0.1f));
            Assert.That(local.y, Is.InRange(container.rect.yMin - 0.1f, container.rect.yMax + 0.1f));
        }
    }
}
