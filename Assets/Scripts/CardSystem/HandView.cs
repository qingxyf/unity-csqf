using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HandView : MonoBehaviour
{
    [Header("手牌区域")]
    public Transform handContainer;
    public GameObject fallbackCardPrefab;
    public float cardScale = 0.34f;
    public float cardSpacing = 104f;
    public float yOffset = -68f;
    public float fanAngle = 18f;
    public float arcDepth = 36f;
    public float maxFanWidth = 860f;
    public float hoverLift = 132f;
    public float hoverScale = 1.2f;

    private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (DeckManager.Instance == null) return;
        DeckManager.Instance.HandChanged -= Refresh;
        DeckManager.Instance.HandChanged += Refresh;
    }

    private void Unsubscribe()
    {
        if (DeckManager.Instance == null) return;
        DeckManager.Instance.HandChanged -= Refresh;
    }

    public void Refresh()
    {
        if (handContainer == null) handContainer = transform;
        if (DeckManager.Instance == null) return;

        foreach (Transform child in handContainer)
            Destroy(child.gameObject);

        List<CardData> hand = DeckManager.Instance.hand;
        float spacing = hand.Count > 1
            ? Mathf.Min(cardSpacing, maxFanWidth / (hand.Count - 1))
            : 0f;
        float startX = -((hand.Count - 1) * spacing) * 0.5f;

        for (int i = 0; i < hand.Count; i++)
        {
            CardData card = hand[i];
            float normalized = hand.Count > 1 ? Mathf.Lerp(-1f, 1f, i / (float)(hand.Count - 1)) : 0f;
            float angle = -normalized * fanAngle;
            Vector2 position = new Vector2(
                startX + i * spacing,
                yOffset - Mathf.Abs(normalized) * arcDepth);
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            GameObject slot = CreateCardSlot(card, handContainer);
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.anchoredPosition = position;
            slotRect.localRotation = rotation;
            slotRect.localScale = Vector3.one;

            GameObject prefab = GetPrefab(card);
            GameObject instance = prefab != null
                ? Instantiate(prefab, slot.transform)
                : CreateRuntimeCard(card, slot.transform);
            Vector3 cardBaseScale = new Vector3(cardScale, cardScale, cardScale);
            PrepareCardInstance(instance, card, cardBaseScale);

            EnsureCardRaycastTarget(slot);

            if (slot.GetComponent<CardCaster>() == null)
                slot.AddComponent<CardCaster>();

            HandCardHover hover = slot.GetComponent<HandCardHover>();
            if (hover == null) hover = slot.AddComponent<HandCardHover>();
            hover.Configure(position, rotation, Vector3.one, hoverLift, hoverScale);
        }
    }

    private GameObject GetPrefab(CardData card)
    {
        if (card == null) return fallbackCardPrefab;

        if (prefabCache.TryGetValue(card.cardName, out GameObject cached))
            return cached != null ? cached : fallbackCardPrefab;

        GameObject prefab = CardResourceUtility.LoadCardPrefab(card);
        if (prefab != null)
        {
            prefabCache[card.cardName] = prefab;
            return prefab;
        }

        return fallbackCardPrefab;
    }

    private GameObject CreateCardSlot(CardData card, Transform parent)
    {
        string slotName = card != null ? $"{card.cardName}_HandSlot" : "Card_HandSlot";
        GameObject slot = new GameObject(slotName, typeof(RectTransform));
        slot.transform.SetParent(parent, false);

        RectTransform rect = slot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300f, 420f) * cardScale;
        return slot;
    }

    private void PrepareCardInstance(GameObject instance, CardData card, Vector3 baseScale)
    {
        if (instance == null) return;

        instance.transform.localScale = baseScale;
        if (instance.TryGetComponent(out RectTransform rectTransform))
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
        }
        else
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
        }

        CardDisplay display = instance.GetComponentInChildren<CardDisplay>(true);
        if (display != null) display.Setup(card);
    }

    private void EnsureCardRaycastTarget(GameObject instance)
    {
        if (instance == null || instance.GetComponent<RectTransform>() == null) return;

        Graphic graphic = instance.GetComponent<Graphic>();
        if (graphic == null)
        {
            Image hitArea = instance.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);
            graphic = hitArea;
        }

        graphic.raycastTarget = true;
    }

    private GameObject CreateRuntimeCard(CardData card, Transform parent)
    {
        GameObject cardObject = new GameObject(card != null ? card.cardName : "RuntimeCard");
        cardObject.transform.SetParent(parent, false);

        RectTransform rect = cardObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, 380f);

        Image background = cardObject.AddComponent<Image>();
        background.color = new Color(0.12f, 0.12f, 0.16f, 1f);

        Button button = cardObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.18f, 0.18f, 0.26f, 1f);
        colors.pressedColor = new Color(0.08f, 0.08f, 0.12f, 1f);
        button.colors = colors;

        TextMeshProUGUI name = CreateText("Name", cardObject.transform, card != null ? card.cardName : "卡牌", 26, TextAlignmentOptions.Center, new Vector2(240f, 56f), new Vector2(0f, 135f));
        TextMeshProUGUI cost = CreateText("Cost", cardObject.transform, card != null ? card.cost.ToString() : "0", 24, TextAlignmentOptions.Center, new Vector2(56f, 56f), new Vector2(-105f, 135f));
        TextMeshProUGUI description = CreateText("Description", cardObject.transform, card != null ? card.description : "", 18, TextAlignmentOptions.TopLeft, new Vector2(230f, 210f), new Vector2(0f, -25f));

        CardDisplay display = cardObject.AddComponent<CardDisplay>();
        display.nameText = name;
        display.costText = cost;
        display.descriptionText = description;
        display.Setup(card);

        return cardObject;
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
}
