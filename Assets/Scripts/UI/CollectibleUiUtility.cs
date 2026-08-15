using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class CollectibleUiUtility
{
    public static void RebuildIconBar(Transform container, IReadOnlyList<CollectibleData> collectibles, int maxIcons = 8)
    {
        if (container == null) return;

        foreach (Transform child in container)
            Object.Destroy(child.gameObject);

        if (collectibles == null) return;

        int count = Mathf.Min(collectibles.Count, maxIcons);
        for (int i = 0; i < count; i++)
            CreateIconCell(collectibles[i], container, new Vector2(36f, 36f), true);

        if (collectibles.Count > maxIcons)
            CreateTextCell($"+{collectibles.Count - maxIcons}", container, new Vector2(42f, 36f));
    }

    public static void AddIconToOffer(GameObject offerObject, CollectibleData collectible)
    {
        if (offerObject == null || collectible == null || collectible.icon == null) return;

        RectTransform offerRect = offerObject.GetComponent<RectTransform>();
        if (offerRect != null && offerRect.sizeDelta.x < 720f)
            offerRect.sizeDelta = new Vector2(720f, offerRect.sizeDelta.y);

        GameObject iconObject = CreateIconCell(collectible, offerObject.transform, new Vector2(48f, 48f), false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(34f, 0f);

        TextMeshProUGUI text = offerObject.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.alignment = TextAlignmentOptions.Left;
            text.rectTransform.sizeDelta = new Vector2(610f, text.rectTransform.sizeDelta.y);
            text.rectTransform.anchoredPosition = new Vector2(54f, 0f);
        }
    }

    private static GameObject CreateIconCell(CollectibleData collectible, Transform parent, Vector2 size, bool includeTooltipText)
    {
        GameObject cell = new GameObject(collectible != null ? $"CollectibleIcon_{collectible.collectibleId}" : "CollectibleIcon");
        cell.transform.SetParent(parent, false);

        RectTransform rect = cell.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        Image image = cell.AddComponent<Image>();
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        if (collectible != null)
            image.sprite = collectible.icon;

        if (includeTooltipText && collectible != null)
        {
            GameObject label = new GameObject("TooltipText");
            label.transform.SetParent(cell.transform, false);
            TextMeshProUGUI text = label.AddComponent<TextMeshProUGUI>();
            text.text = collectible.collectibleName;
            text.fontSize = 10;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.9f, 0.88f, 0.72f, 1f);
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(80f, 16f);
            text.rectTransform.anchoredPosition = new Vector2(0f, -28f);
        }

        return cell;
    }

    private static void CreateTextCell(string text, Transform parent, Vector2 size)
    {
        GameObject cell = new GameObject("CollectibleOverflow");
        cell.transform.SetParent(parent, false);

        RectTransform rect = cell.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        TextMeshProUGUI label = cell.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 18;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.95f, 0.9f, 0.7f, 1f);
        label.raycastTarget = false;
    }
}
