using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CampManager : MonoBehaviour
{
    [Header("Camp Settings")]
    public int restHealPercent = 40;
    public int cardsToGive = 2;
    public int maxCardCost = 2;
    public bool isInitialCamp;
    public int initialCampCardsToGive = 10;
    public int initialCampMaxCardCost = 3;
    public bool autoBuildUI = true;

    [Header("UI")]
    public Button restButton;
    public Button supplyButton;
    public Button removeButton;
    public TextMeshProUGUI titleText;

    private void Start()
    {
        EnsureManagers();

        if (autoBuildUI)
            EnsureUI();

        BindButtons();
    }

    public void ConfigureForNodeDepth(int nodeDepth)
    {
        isInitialCamp = nodeDepth == 0;
        ApplyCampModeToUI();
    }

    public void OnRestButtonClicked()
    {
        EnsureManagers();

        if (PlayerStats.Instance != null)
        {
            int healAmount = Mathf.CeilToInt(PlayerStats.Instance.maxHealth * (restHealPercent / 100f));
            PlayerStats.Instance.Heal(healAmount);
        }

        CompleteNode();
    }

    public void OnSupplyButtonClicked()
    {
        EnsureManagers();

        if (DeckManager.Instance != null)
            DeckManager.Instance.DrawCardsWithMaxCost(GetCurrentCardRewardCount(), GetCurrentMaxCardCost());

        CompleteNode();
    }

    public void OnRemoveButtonClicked()
    {
        EnsureManagers();

        if (DeckManager.Instance != null && DeckManager.Instance.RemoveRandomBackpackCard(out CardData removedCard))
            Debug.Log($"营地移除卡牌：{removedCard.cardName}");

        CompleteNode();
    }

    private void BindButtons()
    {
        if (restButton != null)
        {
            restButton.onClick.RemoveListener(OnRestButtonClicked);
            restButton.onClick.AddListener(OnRestButtonClicked);
        }

        if (supplyButton != null)
        {
            supplyButton.onClick.RemoveListener(OnSupplyButtonClicked);
            supplyButton.onClick.AddListener(OnSupplyButtonClicked);
        }

        if (removeButton != null)
        {
            removeButton.onClick.RemoveListener(OnRemoveButtonClicked);
            removeButton.onClick.AddListener(OnRemoveButtonClicked);
        }
    }

    private void CompleteNode()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.CompleteCurrentNode();
    }

    private void EnsureManagers()
    {
        DeckManager.EnsureInstance();
        PlayerStats.EnsureInstance();
    }

    private void EnsureUI()
    {
        if (restButton != null && supplyButton != null && removeButton != null)
        {
            ApplyCampModeToUI();
            return;
        }

        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("RuntimeCampCanvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = new GameObject("CampPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(560f, 340f);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.08f, 0.05f, 0.94f);

        titleText = CreateText("Title", panel.transform, "营地", 34, TextAlignmentOptions.Center, new Vector2(500f, 60f), new Vector2(0f, 125f));

        restButton = CreateButton("RestButton", panel.transform, $"休息  +{restHealPercent}% 生命", new Vector2(420f, 58f), new Vector2(0f, 45f));
        supplyButton = CreateButton("SupplyButton", panel.transform, $"补给  获得 {cardsToGive} 张低费卡牌", new Vector2(420f, 58f), new Vector2(0f, -25f));
        removeButton = CreateButton("RemoveButton", panel.transform, "移除 1 张随机卡牌", new Vector2(420f, 58f), new Vector2(0f, -95f));
        ApplyCampModeToUI();
    }

    private int GetCurrentCardRewardCount()
    {
        return isInitialCamp ? initialCampCardsToGive : cardsToGive;
    }

    private int GetCurrentMaxCardCost()
    {
        return isInitialCamp ? initialCampMaxCardCost : maxCardCost;
    }

    private void ApplyCampModeToUI()
    {
        if (titleText != null)
            titleText.text = isInitialCamp ? "起始篝火" : "营地";

        if (restButton != null)
            restButton.gameObject.SetActive(!isInitialCamp);

        if (removeButton != null)
            removeButton.gameObject.SetActive(!isInitialCamp);

        if (supplyButton != null)
        {
            supplyButton.gameObject.SetActive(true);
            RectTransform rect = supplyButton.GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = isInitialCamp ? Vector2.zero : new Vector2(0f, -25f);

            SetButtonLabel(
                supplyButton,
                isInitialCamp
                    ? $"领取 {initialCampCardsToGive} 张初始牌"
                    : $"补给  获得 {cardsToGive} 张低费卡牌");
        }
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null) return;

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
            text.text = label;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 size, Vector2 position)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.sizeDelta = size;
        buttonRect.anchoredPosition = position;
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.16f, 0.35f, 0.18f, 1f);
        Button button = buttonObject.AddComponent<Button>();

        CreateText("Text", buttonObject.transform, label, 20, TextAlignmentOptions.Center, size, Vector2.zero);
        return button;
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
