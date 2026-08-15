using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardChoiceUI : MonoBehaviour
{
    [Header("Reward Settings")]
    public int choiceCount = 3;
    public int maxCost = 99;
    public CardElement rewardElement = CardElement.Neutral;
    public bool filterByElement;
    public string title = "选择卡牌";
    public bool completeNodeAfterPick = true;
    public int skipHealAmount = 0;
    public int maxPicks = 1;

    [Header("UI")]
    public TextMeshProUGUI titleText;
    public Transform choiceContainer;
    public GameObject choiceButtonPrefab;
    public Button skipButton;

    private readonly List<CardData> selectedCards = new List<CardData>();

    private void Start()
    {
        EnsureManagers();
        EnsureUI();
        PopulateChoices();
    }

    private void EnsureManagers()
    {
        DeckManager.EnsureInstance();
        PlayerStats.EnsureInstance();
    }

    private void PopulateChoices()
    {
        if (titleText != null) titleText.text = title;
        selectedCards.Clear();

        foreach (Transform child in choiceContainer)
            Destroy(child.gameObject);

        List<CardData> choices = filterByElement
            ? DeckManager.Instance.GetRewardChoices(choiceCount, maxCost, rewardElement)
            : DeckManager.Instance.GetRewardChoices(choiceCount, maxCost);

        if (choices.Count == 0)
        {
            DeckManager.Instance.DrawFromHiddenPool(1);
            CompleteNode();
            return;
        }

        foreach (CardData card in choices)
        {
            GameObject buttonObject = Instantiate(choiceButtonPrefab, choiceContainer);
            buttonObject.SetActive(true);
            TextMeshProUGUI text = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = $"{card.cardName}  费用:{card.cost}\n<size=75%>{card.description}</size>";

            Button button = buttonObject.GetComponent<Button>();
            if (button != null)
            {
                CardData selected = card;
                if (maxPicks <= 1)
                    button.onClick.AddListener(() => PickSingleCard(selected));
                else
                    button.onClick.AddListener(() => ToggleCardSelection(selected, buttonObject));
            }
        }

        if (skipButton != null)
        {
            TextMeshProUGUI skipText = skipButton.GetComponentInChildren<TextMeshProUGUI>();
            if (skipText != null)
                skipText.text = maxPicks > 1 ? $"确认 0/{maxPicks}" : (skipHealAmount > 0 ? $"跳过（+{skipHealAmount} 生命）" : "跳过");
        }
    }

    private void PickSingleCard(CardData card)
    {
        if (!DeckManager.Instance.TakeCardFromHiddenPool(card))
            DeckManager.Instance.AddCardToBackpack(card);

        CompleteNode();
    }

    private void ToggleCardSelection(CardData card, GameObject buttonObject)
    {
        if (card == null || buttonObject == null) return;

        Image image = buttonObject.GetComponent<Image>();
        TextMeshProUGUI text = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
        bool isSelected = selectedCards.Contains(card);

        if (isSelected)
        {
            selectedCards.Remove(card);
            if (image != null) image.color = new Color(0.2f, 0.22f, 0.32f, 1f);
        }
        else
        {
            if (selectedCards.Count >= maxPicks) return;
            selectedCards.Add(card);
            if (image != null) image.color = new Color(0.28f, 0.42f, 0.32f, 1f);
        }

        if (text != null)
        {
            string prefix = selectedCards.Contains(card) ? "[已选择] " : "";
            text.text = $"{prefix}{card.cardName}  费用:{card.cost}\n<size=75%>{card.description}</size>";
        }

        UpdateConfirmText();
    }

    private void SkipReward()
    {
        if (maxPicks > 1)
        {
            ConfirmSelectedCards();
            return;
        }

        if (skipHealAmount > 0 && PlayerStats.Instance != null)
            PlayerStats.Instance.Heal(skipHealAmount);

        CompleteNode();
    }

    private void ConfirmSelectedCards()
    {
        if (selectedCards.Count == 0 && skipHealAmount > 0 && PlayerStats.Instance != null)
            PlayerStats.Instance.Heal(skipHealAmount);

        foreach (CardData card in selectedCards)
        {
            if (!DeckManager.Instance.TakeCardFromHiddenPool(card))
                DeckManager.Instance.AddCardToBackpack(card);
        }

        CompleteNode();
    }

    private void UpdateConfirmText()
    {
        if (skipButton == null) return;
        TextMeshProUGUI skipText = skipButton.GetComponentInChildren<TextMeshProUGUI>();
        if (skipText != null)
            skipText.text = $"确认 {selectedCards.Count}/{maxPicks}";
    }

    private void CompleteNode()
    {
        if (completeNodeAfterPick && GameManager.Instance != null)
            GameManager.Instance.CompleteCurrentNode();
    }

    private void EnsureUI()
    {
        if (titleText != null && choiceContainer != null && choiceButtonPrefab != null && skipButton != null)
            return;

        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("RuntimeRewardCanvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = new GameObject("RewardPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(760f, 500f);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.06f, 0.08f, 0.94f);

        titleText = CreateText("Title", panel.transform, title, 34, TextAlignmentOptions.Center, new Vector2(680f, 60f), new Vector2(0f, 200f));

        GameObject choices = new GameObject("Choices");
        choices.transform.SetParent(panel.transform, false);
        choiceContainer = choices.transform;
        RectTransform choicesRect = choices.AddComponent<RectTransform>();
        choicesRect.sizeDelta = new Vector2(680f, 300f);
        choicesRect.anchoredPosition = new Vector2(0f, 20f);
        VerticalLayoutGroup layout = choices.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        choiceButtonPrefab = CreateButtonObject("RewardButtonPrefab", "卡牌", new Vector2(680f, 78f));
        choiceButtonPrefab.SetActive(false);
        choiceButtonPrefab.transform.SetParent(transform, false);

        GameObject skipObject = CreateButtonObject("SkipButton", "跳过", new Vector2(190f, 48f));
        skipObject.transform.SetParent(panel.transform, false);
        skipObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -205f);
        skipButton = skipObject.GetComponent<Button>();
        skipButton.onClick.AddListener(SkipReward);
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
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.22f, 0.32f, 1f);
        Button button = buttonObject.AddComponent<Button>();

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.32f, 0.35f, 0.48f, 1f);
        colors.pressedColor = new Color(0.12f, 0.14f, 0.22f, 1f);
        button.colors = colors;

        CreateText("Text", buttonObject.transform, label, 20, TextAlignmentOptions.Center, size, Vector2.zero);
        return buttonObject;
    }
}
