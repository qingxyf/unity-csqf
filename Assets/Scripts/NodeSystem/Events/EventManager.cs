using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventManager : NodeContentController
{
    [Header("Events")]
    public List<EventData> eventPool = new List<EventData>();

    [Header("UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image illustrationImage;
    public Transform choiceButtonContainer;
    public GameObject choiceButtonPrefab;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button continueButton;

    [Header("Runtime")]
    public bool autoBuildUI = true;
    public bool autoLoadResources = true;

    private EventData currentEvent;
    private string lastGrantedCollectibleName;
    private string lastUpgradedCardName;
    private Button boundContinueButton;

    public string LastGrantedCollectibleName => lastGrantedCollectibleName;
    public string LastUpgradedCardName => lastUpgradedCardName;

    private void Start()
    {
        EnsureManagers();

        if (autoBuildUI)
            EnsureRuntimeUI();

        if (autoLoadResources)
            LoadEventsIfNeeded();

        if (resultPanel != null)
            resultPanel.SetActive(false);

        BindContinueButton();

        ShowRandomEvent();
    }

    private void EnsureManagers()
    {
        DeckManager.EnsureInstance();
        PlayerStats.EnsureInstance();
    }

    public void ShowRandomEvent()
    {
        if (eventPool == null || eventPool.Count == 0)
        {
            Debug.LogWarning("EventManager: event pool is empty.");
            OnContinue();
            return;
        }

        currentEvent = EventPool.Draw(eventPool);
        DisplayEvent(currentEvent);
    }

    private void DisplayEvent(EventData evt)
    {
        if (evt == null)
        {
            Debug.LogWarning("EventManager: attempted to display a null event.");
            OnContinue();
            return;
        }

        EnsureChoiceUI();
        BindContinueButton();

        if (titleText != null)
            titleText.text = evt.eventName;

        if (descriptionText != null)
            descriptionText.text = evt.description;

        if (illustrationImage != null)
        {
            if (evt.illustration != null)
            {
                illustrationImage.sprite = evt.illustration;
                illustrationImage.gameObject.SetActive(true);
            }
            else
            {
                illustrationImage.gameObject.SetActive(false);
            }
        }

        if (choiceButtonContainer != null)
        {
            choiceButtonContainer.gameObject.SetActive(true);
            foreach (Transform child in choiceButtonContainer)
                Destroy(child.gameObject);
        }

        if (choiceButtonContainer == null || choiceButtonPrefab == null)
        {
            Debug.LogWarning("EventManager: choice UI is not configured; event remains non-interactive.");
            return;
        }

        List<EventChoice> choices = evt.choices ?? new List<EventChoice>();
        for (int i = 0; i < choices.Count; i++)
        {
            EventChoice choice = choices[i];
            if (choice == null) continue;

            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            btnObj.SetActive(true);

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                string label = choice.buttonText;
                string hint = GetChoiceHint(choice);
                if (!string.IsNullOrEmpty(hint))
                    label += $"\n<size=70%><color=#aaaaaa>{hint}</color></size>";
                btnText.text = label;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                int index = i;
                btn.onClick.AddListener(() => OnChoiceSelected(index));
            }
        }
    }

    private string GetChoiceHint(EventChoice choice)
    {
        List<string> hints = new List<string>();

        if (choice.healToFull) hints.Add("完全恢复");
        else if (choice.healthChange > 0) hints.Add($"+{choice.healthChange} 生命");
        else if (choice.healthChange < 0) hints.Add($"{choice.healthChange} 生命");

        if (choice.maxHealthChange > 0) hints.Add($"+{choice.maxHealthChange} 最大生命");
        else if (choice.maxHealthChange < 0) hints.Add($"{choice.maxHealthChange} 最大生命");

        if (choice.shieldGain > 0) hints.Add($"+{choice.shieldGain} 护盾");
        if (choice.cardsToDraw > 0) hints.Add($"获得 {choice.cardsToDraw} 张卡牌");
        if (choice.cardsToRemove > 0) hints.Add($"移除 {choice.cardsToRemove} 张卡牌");
        if (choice.grantCollectible) hints.Add("获得藏品");
        if (choice.isGamble) hints.Add($"{choice.gambleSuccessRate * 100:F0}% 成功");

        return string.Join("  ", hints);
    }

    private void OnChoiceSelected(int index)
    {
        if (currentEvent == null || currentEvent.choices == null || index < 0 || index >= currentEvent.choices.Count) return;
        if (CompletionRequested) return;

        lastGrantedCollectibleName = null;
        lastUpgradedCardName = null;

        EventChoice choice = currentEvent.choices[index];
        if (choice == null) return;
        string resultMessage;

        if (choice.isGamble)
        {
            bool success = Random.value <= choice.gambleSuccessRate;
            if (success)
            {
                if (!ApplyChoiceEffects(choice))
                    return;
                resultMessage = choice.resultDescription;
            }
            else
            {
                if (!ApplyHealthChange(choice.gambleFailHealthChange))
                    return;
                resultMessage = choice.gambleFailText;
            }
        }
        else
        {
            if (!ApplyChoiceEffects(choice))
                return;
            resultMessage = choice.resultDescription;
        }

        if (!TryBeginCompletion())
            return;

        if (!string.IsNullOrEmpty(lastGrantedCollectibleName))
            resultMessage = $"{resultMessage}\n\n获得藏品：{lastGrantedCollectibleName}";

        if (!string.IsNullOrEmpty(lastUpgradedCardName))
            resultMessage = $"{resultMessage}\n\n卡牌强化：{lastUpgradedCardName}";

        if (choiceButtonContainer != null)
            choiceButtonContainer.gameObject.SetActive(false);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            if (resultText != null)
                resultText.text = resultMessage;
        }
    }

    private bool ApplyChoiceEffects(EventChoice choice)
    {
        lastGrantedCollectibleName = null;
        lastUpgradedCardName = null;

        PlayerStats player = PlayerStats.Instance;
        if (player == null) return false;

        if (choice.healToFull)
            player.HealFull();
        else if (!ApplyHealthChange(choice.healthChange))
            return false;

        if (choice.maxHealthChange != 0)
            player.IncreaseMaxHealth(choice.maxHealthChange);

        if (choice.shieldGain > 0)
            player.AddShield(choice.shieldGain);

        if (choice.manaChange != 0)
            player.RestoreMana(choice.manaChange);

        if (choice.grantCollectible)
        {
            CollectibleData collectible = CollectibleManager.CreateRandomCollectible();
            if (CollectibleManager.AddCollectible(collectible))
                lastGrantedCollectibleName = collectible.collectibleName;
        }

        DeckManager deck = DeckManager.Instance;
        if (deck == null) return true;

        if (choice.upgradeRandomCard && deck.TryUpgradeRandomBackpackCard(out CardData upgradedCard))
            lastUpgradedCardName = upgradedCard.cardName;

        if (choice.cardsToDraw > 0)
        {
            if (choice.useRewardElement)
                DrawFilteredCards(deck, choice.cardsToDraw, choice.rewardElement, choice.rewardMaxCost);
            else
                deck.DrawFromHiddenPool(choice.cardsToDraw);
        }

        if (choice.cardsToRemove > 0)
        {
            for (int i = 0; i < choice.cardsToRemove; i++)
            {
                if (deck.backpack.Count > 0 && deck.RemoveRandomBackpackCard(out CardData removedCard))
                    Debug.Log($"事件移除卡牌：{removedCard.cardName}");
            }
        }

        return true;
    }

    private bool ApplyHealthChange(int healthChange)
    {
        PlayerStats player = PlayerStats.Instance;
        if (player == null) return false;

        if (healthChange > 0)
            player.Heal(healthChange);
        else if (healthChange < 0)
            return player.PayHealth(-healthChange);

        return true;
    }

    private void DrawFilteredCards(DeckManager deck, int count, CardElement element, int maxCost)
    {
        for (int i = 0; i < count; i++)
        {
            List<CardData> choices = deck.GetRewardChoices(1, maxCost, element);
            if (choices.Count == 0)
            {
                deck.DrawFromHiddenPool(1);
                continue;
            }

            deck.TakeCardFromHiddenPool(choices[0]);
        }
    }

    private void LoadEventsIfNeeded()
    {
        eventPool.RemoveAll(e => e == null);
        if (eventPool.Count > 0) return;

        EventData[] loaded = Resources.LoadAll<EventData>("Events");
        if (loaded != null && loaded.Length > 0)
            eventPool = loaded.ToList();

        if (eventPool.Count == 0)
        {
            eventPool = EventPool.GetOrCreateRuntimeFallback();
        }
    }

    private void EnsureRuntimeUI()
    {
        if (titleText != null && descriptionText != null && choiceButtonContainer != null && choiceButtonPrefab != null && continueButton != null)
            return;

        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("RuntimeEventCanvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = CreatePanel("EventPanel", canvas.transform, new Vector2(760f, 520f), new Color(0.05f, 0.05f, 0.08f, 0.92f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        titleText = CreateText("Title", panel.transform, "事件", 34, TextAlignmentOptions.Center, new Vector2(680f, 60f), new Vector2(0f, 205f));
        descriptionText = CreateText("Description", panel.transform, "", 22, TextAlignmentOptions.TopLeft, new Vector2(660f, 160f), new Vector2(0f, 85f));

        GameObject choiceRoot = new GameObject("Choices", typeof(RectTransform));
        choiceRoot.transform.SetParent(panel.transform, false);
        RectTransform choiceRect = choiceRoot.GetComponent<RectTransform>();
        choiceRect.sizeDelta = new Vector2(660f, 180f);
        choiceRect.anchoredPosition = new Vector2(0f, -115f);
        VerticalLayoutGroup layout = choiceRoot.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        choiceButtonContainer = choiceRoot.transform;

        choiceButtonPrefab = CreateButtonObject("RuntimeChoiceButton", "选择", new Vector2(660f, 48f));
        choiceButtonPrefab.SetActive(false);
        choiceButtonPrefab.transform.SetParent(transform, false);

        resultPanel = CreatePanel("ResultPanel", panel.transform, new Vector2(680f, 230f), new Color(0.08f, 0.08f, 0.12f, 0.96f));
        resultPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -65f);
        resultText = CreateText("ResultText", resultPanel.transform, "", 22, TextAlignmentOptions.TopLeft, new Vector2(620f, 140f), new Vector2(0f, 35f));
        continueButton = CreateButton("ContinueButton", resultPanel.transform, "继续", new Vector2(180f, 48f), new Vector2(0f, -75f));
        BindContinueButton();
        resultPanel.SetActive(false);
    }

    // EventContent deliberately has no serialized UI references. DisplayEvent invokes this
    // before assigning event text, so fallback construction cannot overwrite the event view.
    private void EnsureChoiceUI()
    {
        if (choiceButtonContainer != null && choiceButtonPrefab != null)
            return;

        EnsureRuntimeUI();
    }

    private void BindContinueButton()
    {
        if (boundContinueButton == continueButton)
            return;

        if (boundContinueButton != null)
            boundContinueButton.onClick.RemoveListener(OnContinue);

        boundContinueButton = continueButton;
        if (boundContinueButton != null)
            boundContinueButton.onClick.AddListener(OnContinue);
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment, Vector2 rectSize, Vector2 position)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        RectTransform rect = tmp.rectTransform;
        rect.sizeDelta = rectSize;
        rect.anchoredPosition = position;
        return tmp;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 size, Vector2 position)
    {
        GameObject buttonObject = CreateButtonObject(name, label, size);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        return buttonObject.GetComponent<Button>();
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
        image.color = new Color(0.2f, 0.22f, 0.32f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.32f, 0.35f, 0.48f, 1f);
        colors.pressedColor = new Color(0.12f, 0.14f, 0.22f, 1f);
        button.colors = colors;

        CreateText("Text", buttonObject.transform, label, 20, TextAlignmentOptions.Center, size, Vector2.zero);
        return buttonObject;
    }

    private void OnContinue()
    {
        TryCompleteNode();
    }
}
