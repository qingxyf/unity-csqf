using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackpackDisplay : MonoBehaviour
{
    public Transform contentContainer;
    public GameObject itemPrefab;
    public GameObject backpackPanel;
    public Transform stagePreviewParent;

    private GameObject overlay;
    public bool IsOpen => overlay != null && overlay.activeSelf;

    public void ToggleBackpack()
    {
        if (IsOpen)
        {
            CloseBackpack();
            return;
        }
        ShowBackpack();
    }

    public void ShowBackpack()
    {
        if (backpackPanel != null) backpackPanel.SetActive(false);
        if (overlay == null)
            overlay = CreateOverlay("背包", CloseBackpack, out RectTransform body);
        overlay.SetActive(true);
        RefreshDisplay();
    }

    public void CloseBackpack()
    {
        if (overlay != null) overlay.SetActive(false);
        if (backpackPanel != null) backpackPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay);
    }

    public void RefreshDisplay()
    {
        if (overlay == null) return;
        Transform content = overlay.transform.Find("Body/Viewport/Content");
        if (content == null) return;
        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        List<CardData> cards = DeckManager.Instance != null ? DeckManager.Instance.backpack : new List<CardData>();
        TextMeshProUGUI title = overlay.transform.Find("Body/标题")?.GetComponent<TextMeshProUGUI>();
        if (title != null) title.text = $"背包（{cards.Count} 张）";
        RectTransform contentRect = content as RectTransform;
        const float rowHeight = 104f;
        for (int i = 0; i < cards.Count; i++) CreateCardRow(content, cards[i], i, rowHeight);
        contentRect.sizeDelta = new Vector2(0f, Mathf.Max(450f, cards.Count * rowHeight + 16f));
        if (cards.Count == 0)
            Text("空背包提示", content, "背包为空，继续探索以获得卡牌。", 22f, new Vector2(760f, 60f), new Vector2(0f, -42f), TextAlignmentOptions.Center);
    }

    public static GameObject CreateOverlay(string title, UnityEngine.Events.UnityAction close, out RectTransform body)
    {
        GameObject root = new GameObject(title + "覆盖层", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        Stretch(root.GetComponent<RectTransform>());
        Image blocker = root.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.72f);
        blocker.raycastTarget = true;
        GameObject panel = UiObject("Body", root.transform, new Vector2(900f, 590f), Vector2.zero, new Color(0.06f, 0.08f, 0.12f, 0.98f));
        body = panel.GetComponent<RectTransform>();
        Text("标题", panel.transform, title, 30f, new Vector2(660f, 48f), new Vector2(-80f, 250f), TextAlignmentOptions.Left);
        Button button = UiObject("关闭", panel.transform, new Vector2(110f, 44f), new Vector2(360f, 250f), new Color(0.3f, 0.12f, 0.14f, 1f)).AddComponent<Button>();
        Text("文字", button.transform, "关闭", 20f, new Vector2(100f, 38f), Vector2.zero, TextAlignmentOptions.Center);
        button.onClick.AddListener(close);
        GameObject viewport = UiObject("Viewport", panel.transform, new Vector2(820f, 450f), new Vector2(0f, -28f), new Color(0f, 0f, 0f, 0.25f));
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 450f);
        ScrollRect scroll = panel.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35f;
        return root;
    }

    private static void CreateCardRow(Transform parent, CardData card, int index, float rowHeight)
    {
        GameObject row = UiObject("Card_" + index, parent, new Vector2(0f, rowHeight - 8f),
            new Vector2(0f, -index * rowHeight - rowHeight * 0.5f - 8f),
            new Color(0.12f, 0.15f, 0.22f, 0.98f));
        RectTransform rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(0f, rowHeight - 8f);
        string element = card != null ? GetElementLabel(card.element) : "未知";
        string name = card != null ? card.cardName : "空卡";
        string cost = card != null ? card.cost.ToString() : "-";
        string effect = card != null ? CardDescriptionFormatter.GetDescription(card) : "";
        Text("信息", row.transform, $"{name}   [{element}]   费用 {cost}\n{effect}",
            20f, new Vector2(760f, 82f), Vector2.zero, TextAlignmentOptions.TopLeft);
    }

    private static string GetElementLabel(CardElement element)
    {
        switch (element)
        {
            case CardElement.Light: return "光";
            case CardElement.Fire: return "火";
            case CardElement.Nature: return "草";
            case CardElement.Water: return "水";
            case CardElement.Shadow: return "暗影";
            default: return "无属性";
        }
    }

    private static GameObject UiObject(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        go.GetComponent<Image>().color = color;
        return go;
    }
    private static TextMeshProUGUI Text(string name,Transform parent,string value,float size,Vector2 dimensions,Vector2 position,TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = size;
        text.enableWordWrapping = true;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.95f, 0.98f, 1f);
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = position;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
