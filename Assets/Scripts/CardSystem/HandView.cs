using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HandView : MonoBehaviour
{
    private static readonly Vector2 HandCardHitboxSize = new Vector2(196f, 292f);
    // The sprite proxy and card root each apply the hand scale.  At the
    // readable 0.46 hand scale, the authored 848 x 1264 frame occupies about
    // 179 x 267 canvas units.  Text positions below map to that final frame.
    private static readonly Vector2 HandCardFrameSize = new Vector2(179f, 267f);
    private const float MinimumHandCardScale = 0.46f;
    private const float HandBottomPadding = 12f;

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
    public float hoverScale = 1.4f;

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
            ? Mathf.Min(cardSpacing, GetSafeFanWidth() / (hand.Count - 1))
            : 0f;
        float startX = -((hand.Count - 1) * spacing) * 0.5f;

        for (int i = 0; i < hand.Count; i++)
        {
            CardData card = hand[i];
            float normalized = hand.Count > 1 ? Mathf.Lerp(-1f, 1f, i / (float)(hand.Count - 1)) : 0f;
            float angle = -normalized * fanAngle;
            Vector2 position = new Vector2(
                startX + i * spacing,
                GetSafeHandBaseline() - Mathf.Abs(normalized) * arcDepth);
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
            float readableScale = Mathf.Max(cardScale, MinimumHandCardScale);
            Vector3 cardBaseScale = new Vector3(readableScale, readableScale, readableScale);
            PrepareCardInstance(instance, card, cardBaseScale, slot.transform);

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
        // The sprite-backed prefabs do not expose a RectTransform of their own.
        // Give their UI parent a full-card hit target instead of the old scaled
        // thumbnail-sized area so every visible card can be hovered and played.
        rect.sizeDelta = HandCardHitboxSize;
        return slot;
    }

    private float GetSafeHandBaseline()
    {
        RectTransform container = handContainer as RectTransform;
        Canvas canvas = container != null ? container.GetComponentInParent<Canvas>() : null;
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (container == null || canvasRect == null)
            return Mathf.Max(yOffset, HandCardHitboxSize.y * 0.5f + HandBottomPadding + arcDepth);

        Vector3 canvasBottom = canvasRect.TransformPoint(
            new Vector3(canvasRect.rect.center.x, canvasRect.rect.yMin, 0f));
        float bottomInHandSpace = container.InverseTransformPoint(canvasBottom).y;
        float minimumVisibleCenter = bottomInHandSpace
            + GetRotatedCardHalfHeight()
            + HandBottomPadding
            + arcDepth;
        return Mathf.Max(yOffset, minimumVisibleCenter);
    }

    private float GetRotatedCardHalfHeight()
    {
        float radians = Mathf.Abs(fanAngle) * Mathf.Deg2Rad;
        return HandCardHitboxSize.y * 0.5f * Mathf.Cos(radians)
            + HandCardHitboxSize.x * 0.5f * Mathf.Sin(radians);
    }

    private void PrepareCardInstance(GameObject instance, CardData card, Vector3 baseScale, Transform slot)
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
        if (display == null) return;

        display.Setup(card);
        CreateConsistentHandCardSurface(instance, card, slot);
        MovePrefabTextIntoHandCanvas(instance, display, slot);
    }

    private float GetSafeFanWidth()
    {
        RectTransform container = handContainer as RectTransform;
        Canvas canvas = container != null ? container.GetComponentInParent<Canvas>() : null;
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (container == null || canvasRect == null) return maxFanWidth;

        Vector3 left = canvasRect.TransformPoint(new Vector3(canvasRect.rect.xMin, canvasRect.rect.center.y, 0f));
        Vector3 right = canvasRect.TransformPoint(new Vector3(canvasRect.rect.xMax, canvasRect.rect.center.y, 0f));
        float canvasWidthInHandSpace = container.InverseTransformPoint(right).x
            - container.InverseTransformPoint(left).x;
        float sideMargin = GetRotatedCardHalfWidth() + HandBottomPadding;
        return Mathf.Min(maxFanWidth, Mathf.Max(0f, canvasWidthInHandSpace - sideMargin * 2f));
    }

    private float GetRotatedCardHalfWidth()
    {
        float radians = Mathf.Abs(fanAngle) * Mathf.Deg2Rad;
        return HandCardHitboxSize.x * 0.5f * Mathf.Cos(radians)
            + HandCardHitboxSize.y * 0.5f * Mathf.Sin(radians);
    }

    private void MovePrefabTextIntoHandCanvas(GameObject instance, CardDisplay display, Transform slot)
    {
        // Authored card prefabs retain their original world-space Canvas for
        // reward/backpack presentation.  In the combat hand that Canvas has
        // absolute positions, so its text lands off screen.  Move only the
        // existing serialized TMP fields into the combat Canvas and leave the
        // prefab asset and its data binding intact.
        Canvas[] prefabCanvases = instance.GetComponentsInChildren<Canvas>(true);
        foreach (Canvas prefabCanvas in prefabCanvases)
            prefabCanvas.enabled = false;

        // Map serialized field locations from the authored frame: title scroll
        // center y=735 maps to -22, and description center y=1000 maps to -78.
        // Keep both regions within the 179 x 267 proxy frame rather than over
        // the character illustration or title scroll.
        LayoutHandText(display.nameText, slot, new Vector2(12f, -22f), new Vector2(HandCardFrameSize.x - 49f, 28f), 17f, TextAlignmentOptions.Center);
        LayoutHandText(display.costText, slot, new Vector2(-66f, 100f), new Vector2(38f, 30f), 17f, TextAlignmentOptions.Center);
        LayoutHandText(display.descriptionText, slot, new Vector2(0f, -78f), new Vector2(HandCardFrameSize.x - 29f, 70f), 12f, TextAlignmentOptions.TopLeft);
    }

    private void CreateConsistentHandCardSurface(GameObject instance, CardData card, Transform slot)
    {
        // Templates mix full frames and unframed portraits.  Hide the template
        // proxies in combat and build one stable UI surface so every element
        // has the same readable frame, illustration area, and text regions.
        foreach (Image image in instance.GetComponentsInChildren<Image>(true))
            if (image.gameObject.name.StartsWith("[UI_Proxy]_")) image.gameObject.SetActive(false);
        foreach (SpriteRenderer renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.enabled = false;

        GameObject surface = new GameObject("HandCardSurface", typeof(RectTransform), typeof(Image), typeof(Outline));
        surface.transform.SetParent(slot, false);
        surface.transform.SetSiblingIndex(0);
        RectTransform surfaceRect = surface.GetComponent<RectTransform>();
        surfaceRect.anchorMin = new Vector2(0.5f, 0.5f);
        surfaceRect.anchorMax = new Vector2(0.5f, 0.5f);
        surfaceRect.sizeDelta = HandCardFrameSize;

        Image background = surface.GetComponent<Image>();
        background.color = new Color(0.055f, 0.065f, 0.09f, 0.97f);
        background.raycastTarget = false;
        Outline outline = surface.GetComponent<Outline>();
        outline.effectColor = GetElementColor(card != null ? card.element : CardElement.Neutral);
        outline.effectDistance = new Vector2(3f, -3f);

        GameObject artObject = new GameObject("HandCardIllustration", typeof(RectTransform), typeof(Image));
        artObject.transform.SetParent(surface.transform, false);
        RectTransform artRect = artObject.GetComponent<RectTransform>();
        artRect.anchorMin = new Vector2(0.5f, 0.5f);
        artRect.anchorMax = new Vector2(0.5f, 0.5f);
        artRect.anchoredPosition = new Vector2(0f, 48f);
        artRect.sizeDelta = new Vector2(145f, 104f);
        Image art = artObject.GetComponent<Image>();
        art.sprite = GetHandIllustration(card, instance);
        art.preserveAspect = true;
        art.raycastTarget = false;

        GameObject textPanel = new GameObject("HandCardTextPanel", typeof(RectTransform), typeof(Image));
        textPanel.transform.SetParent(surface.transform, false);
        RectTransform textPanelRect = textPanel.GetComponent<RectTransform>();
        textPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        textPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        textPanelRect.anchoredPosition = new Vector2(0f, -57f);
        textPanelRect.sizeDelta = new Vector2(158f, 112f);
        Image textPanelImage = textPanel.GetComponent<Image>();
        textPanelImage.color = new Color(0.015f, 0.02f, 0.03f, 0.8f);
        textPanelImage.raycastTarget = false;

        GameObject costBadge = new GameObject("HandCardCostBadge", typeof(RectTransform), typeof(Image));
        costBadge.transform.SetParent(surface.transform, false);
        RectTransform costBadgeRect = costBadge.GetComponent<RectTransform>();
        costBadgeRect.anchorMin = new Vector2(0.5f, 0.5f);
        costBadgeRect.anchorMax = new Vector2(0.5f, 0.5f);
        costBadgeRect.anchoredPosition = new Vector2(-66f, 100f);
        costBadgeRect.sizeDelta = new Vector2(38f, 30f);
        Image costBadgeImage = costBadge.GetComponent<Image>();
        costBadgeImage.color = new Color(0.015f, 0.02f, 0.03f, 0.86f);
        costBadgeImage.raycastTarget = false;
    }

    private static Sprite GetHandIllustration(CardData card, GameObject instance)
    {
        Sprite preferred = card != null ? card.cardArt : null;
        if (!LooksLikeFullCardFrame(preferred)) return preferred;

        foreach (SpriteRenderer renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Sprite candidate = renderer.sprite;
            if (candidate != null && candidate != preferred && !LooksLikeFullCardFrame(candidate))
                return candidate;
        }
        return preferred;
    }

    private static bool LooksLikeFullCardFrame(Sprite sprite)
    {
        if (sprite == null) return false;
        Rect rect = sprite.rect;
        return rect.width >= 800f && rect.height >= 1200f
            && rect.height / rect.width > 1.35f;
    }

    private static Color GetElementColor(CardElement element)
    {
        switch (element)
        {
            case CardElement.Light: return new Color(0.94f, 0.76f, 0.28f, 1f);
            case CardElement.Fire: return new Color(0.94f, 0.3f, 0.16f, 1f);
            case CardElement.Nature: return new Color(0.28f, 0.75f, 0.38f, 1f);
            case CardElement.Water: return new Color(0.22f, 0.68f, 0.96f, 1f);
            case CardElement.Shadow: return new Color(0.62f, 0.38f, 0.88f, 1f);
            default: return new Color(0.76f, 0.76f, 0.8f, 1f);
        }
    }

    private static void LayoutHandText(
        TextMeshProUGUI text,
        Transform slot,
        Vector2 position,
        Vector2 size,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        if (text == null) return;

        text.transform.SetParent(slot, false);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        text.fontSize = fontSize;
        text.fontSizeMax = fontSize;
        text.fontSizeMin = Mathf.Max(9f, fontSize * 0.7f);
        text.enableAutoSizing = true;
        text.enableWordWrapping = true;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.95f, 0.98f, 1f);
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
