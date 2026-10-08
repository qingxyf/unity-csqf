using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : NodeContentController
{
    [Header("Shop Settings")]
    public int offerCount = 4;
    public int removeBaseCost = 50;
    public int removeCostIncrease = 25;
    public int healCost = 30;
    public int healAmount = 20;
    public int refreshCost = 15;
    public bool sellCollectibles = true;

    [Header("UI")]
    public TextMeshProUGUI titleText;
    public Transform offerContainer;
    public GameObject offerButtonPrefab;
    public Button leaveButton;

    private readonly List<CardData> offers = new List<CardData>();
    private readonly List<System.Action> refreshPriceLabels = new List<System.Action>();
    private int removeServiceUses;
    private CollectibleData collectibleOffer;
    private CollectibleData guaranteedCollectible;
    private bool guaranteedCollectiblePurchased;

    private void Start()
    {
        DeckManager.EnsureInstance();
        PlayerStats.EnsureInstance();
        EnsureUI();
        guaranteedCollectible = CollectibleManager.ClaimNextShopCollectible();
        RefreshOffers();
    }

    public int GetCardPrice(CardData card)
    {
        if (card == null) return 40;

        int basePrice;
        if (card.cost <= 1) basePrice = 40;
        else if (card.cost <= 3) basePrice = 60;
        else basePrice = 90;

        return ApplyShopDiscount(basePrice);
    }

    public int GetRemoveServiceCost()
    {
        return ApplyShopDiscount(removeBaseCost + removeServiceUses * removeCostIncrease);
    }

    public int GetHealServiceCost() => ApplyShopDiscount(healCost);
    public int GetRefreshServiceCost() => ApplyShopDiscount(refreshCost);

    public void RefreshOffers()
    {
        if (offerContainer == null || offerButtonPrefab == null) return;

        foreach (Transform child in offerContainer)
            Destroy(child.gameObject);

        offers.Clear();
        refreshPriceLabels.Clear();
        offers.AddRange(DeckManager.Instance.GetRewardChoices(offerCount));

        foreach (CardData card in offers)
            CreateCardOffer(card);

        if (sellCollectibles || guaranteedCollectible != null)
            CreateCollectibleOffer();

        CreateServiceButton(
            () => $"随机移除卡牌  {GetRemoveServiceCost()} 金币",
            "从背包中随机移除一张卡牌。",
            RemoveCardService);

        CreateServiceButton(
            () => $"恢复 {healAmount} 生命  {GetHealServiceCost()} 金币",
            "在下一场战斗前治疗伤势。",
            BuyHeal);

        CreateServiceButton(
            () => $"刷新货架  {GetRefreshServiceCost()} 金币",
            "重新生成一批卡牌商品。",
            BuyRefresh);
    }

    private int ApplyShopDiscount(int price)
    {
        int discountPercent = CollectibleManager.GetShopDiscountPercent();
        return Mathf.Max(1, Mathf.CeilToInt(price * (100 - discountPercent) / 100f));
    }

    private void CreateCardOffer(CardData card)
    {
        GameObject buttonObject = Instantiate(offerButtonPrefab, offerContainer);
        buttonObject.SetActive(true);

        TextMeshProUGUI text = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
        System.Action updateLabel = () =>
        {
            if (text != null)
                text.text = $"{card.cardName}  {GetCardPrice(card)} 金币  费用:{card.cost}\n<size=75%>{card.description}</size>";
        };
        refreshPriceLabels.Add(updateLabel);
        updateLabel();

        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            CardData selected = card;
            button.onClick.AddListener(() => BuyCard(selected, buttonObject));
        }
    }

    private void CreateServiceButton(System.Func<string> label, string description, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = Instantiate(offerButtonPrefab, offerContainer);
        buttonObject.SetActive(true);

        TextMeshProUGUI text = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
        System.Action updateLabel = () =>
        {
            if (text != null) text.text = $"{label()}\n<size=75%>{description}</size>";
        };
        refreshPriceLabels.Add(updateLabel);
        updateLabel();

        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                if (!button.interactable) return;
                button.interactable = false;
                action();
                if (button != null) button.interactable = true;
            });
        }
    }

    private void CreateCollectibleOffer()
    {
        collectibleOffer = guaranteedCollectible != null && !guaranteedCollectiblePurchased
            ? guaranteedCollectible : CollectibleManager.CreateRandomCollectible();
        if (collectibleOffer == null)
            return;

        GameObject buttonObject = Instantiate(offerButtonPrefab, offerContainer);
        buttonObject.SetActive(true);

        TextMeshProUGUI text = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
        CollectibleData selected = collectibleOffer;
        System.Action updateLabel = () =>
        {
            if (text != null && selected != null)
                text.text = $"{selected.collectibleName}  {ApplyShopDiscount(selected.shopPrice)} 金币\n<size=75%>{selected.description}</size>";
        };
        refreshPriceLabels.Add(updateLabel);
        updateLabel();

        CollectibleUiUtility.AddIconToOffer(buttonObject, collectibleOffer);

        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => BuyCollectible(selected, buttonObject));
        }
    }

    private void BuyCard(CardData card, GameObject buttonObject)
    {
        if (PlayerStats.Instance == null || DeckManager.Instance == null) return;

        Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
        if (button != null && !button.interactable) return;
        if (button != null) button.interactable = false;

        int price = GetCardPrice(card);
        if (!PlayerStats.Instance.SpendGold(price))
        {
            if (button != null) button.interactable = true;
            return;
        }

        if (!DeckManager.Instance.TakeCardFromHiddenPool(card))
            DeckManager.Instance.AddCardToBackpack(card);

        Destroy(buttonObject);
        UpdateTitle();
    }

    private void BuyCollectible(CollectibleData collectible, GameObject buttonObject)
    {
        if (collectible == null || PlayerStats.Instance == null) return;

        Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
        if (button != null && !button.interactable) return;
        if (button != null) button.interactable = false;

        int price = ApplyShopDiscount(collectible.shopPrice);
        if (!PlayerStats.Instance.SpendGold(price))
        {
            if (button != null) button.interactable = true;
            return;
        }

        if (!CollectibleManager.AddCollectible(collectible))
        {
            PlayerStats.Instance.GainGold(price);
            if (button != null) button.interactable = true;
            return;
        }
        if (collectible == guaranteedCollectible)
            guaranteedCollectiblePurchased = true;
        Destroy(buttonObject);
        UpdateTitle();
    }

    private void RemoveCardService()
    {
        if (PlayerStats.Instance == null || DeckManager.Instance == null) return;

        int cost = GetRemoveServiceCost();
        if (!PlayerStats.Instance.SpendGold(cost))
            return;

        if (DeckManager.Instance.RemoveRandomBackpackCard(out CardData removedCard))
        {
            removeServiceUses++;
            Debug.Log($"Shop removed card: {removedCard.cardName}");
            RefreshOffers();
        }
        else
        {
            PlayerStats.Instance.GainGold(cost);
        }

        UpdateTitle();
    }

    private void BuyHeal()
    {
        if (PlayerStats.Instance == null) return;
        if (!PlayerStats.Instance.SpendGold(GetHealServiceCost())) return;

        PlayerStats.Instance.Heal(healAmount);
        UpdateTitle();
    }

    private void BuyRefresh()
    {
        if (PlayerStats.Instance == null) return;
        if (!PlayerStats.Instance.SpendGold(GetRefreshServiceCost())) return;

        RefreshOffers();
        UpdateTitle();
    }

    private void LeaveShop()
    {
        TryCompleteNode();
    }

    private void EnsureUI()
    {
        if (titleText != null && offerContainer != null && offerButtonPrefab != null && leaveButton != null)
            return;

        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("RuntimeShopCanvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = new GameObject("ShopPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(820f, 700f);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.06f, 0.05f, 0.03f, 0.94f);

        titleText = CreateText("Title", panel.transform, "", 32, TextAlignmentOptions.Center, new Vector2(720f, 60f), new Vector2(0f, 305f));

        GameObject offersObject = new GameObject("Offers", typeof(RectTransform));
        offersObject.transform.SetParent(panel.transform, false);
        RectTransform offersRect = offersObject.GetComponent<RectTransform>();
        offersRect.sizeDelta = new Vector2(720f, 540f);
        // Keep the first offer below the title and the last above Leave.
        offersRect.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup layout = offersObject.AddComponent<VerticalLayoutGroup>();
        // Eight default offers need to fit their 62px preferred heights inside
        // this 540px container (7 * 6px gaps leaves a small margin).
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        offerContainer = offersObject.transform;

        offerButtonPrefab = CreateButtonObject("OfferButtonPrefab", "商品", new Vector2(720f, 62f));
        offerButtonPrefab.SetActive(false);
        offerButtonPrefab.transform.SetParent(transform, false);

        GameObject leaveObject = CreateButtonObject("LeaveButton", "离开", new Vector2(180f, 48f));
        leaveObject.transform.SetParent(panel.transform, false);
        leaveObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -315f);
        leaveButton = leaveObject.GetComponent<Button>();
        leaveButton.onClick.AddListener(LeaveShop);

        UpdateTitle();
    }

    private void UpdateTitle()
    {
        foreach (System.Action refreshLabel in refreshPriceLabels)
            refreshLabel();
        if (titleText == null) return;

        int gold = PlayerStats.Instance != null ? PlayerStats.Instance.gold : 0;
        titleText.text = $"商店  金币:{gold}  藏品:{CollectibleManager.OwnedCollectibles.Count}";
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment, Vector2 sizeDelta, Vector2 position)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.rectTransform.sizeDelta = sizeDelta;
        tmp.rectTransform.anchoredPosition = position;
        return tmp;
    }

    private GameObject CreateButtonObject(string name, string label, Vector2 size)
    {
        GameObject buttonObject = new GameObject(name);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = size.y;
        layoutElement.preferredHeight = size.y;
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.25f, 0.18f, 0.08f, 1f);
        Button button = buttonObject.AddComponent<Button>();

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.38f, 0.26f, 0.12f, 1f);
        colors.pressedColor = new Color(0.16f, 0.11f, 0.06f, 1f);
        button.colors = colors;

        CreateText("Text", buttonObject.transform, label, 18, TextAlignmentOptions.Center, size, Vector2.zero);
        return buttonObject;
    }
}
