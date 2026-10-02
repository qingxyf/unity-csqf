using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapUIController : MonoBehaviour
{
    [Header("Dependencies")]
    public GameObject backpackButton; // The button object in the scene
    public GameObject rulesButton;    // The button object in the scene
    public MapGenerator mapGenerator; // To check map visibility

    [Header("Display Scripts")]
    public BackpackDisplay backpackDisplay;
    public RulesDisplay rulesDisplay;

    [Header("Settings")]
    public float delayBeforeShow = 5.0f;

    private void Start()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindObjectOfType<MapGenerator>();
        }

        if (backpackDisplay == null) backpackDisplay = FindObjectOfType<BackpackDisplay>(true);
        if (rulesDisplay == null) rulesDisplay = FindObjectOfType<RulesDisplay>(true);

        if (backpackButton != null) backpackButton.SetActive(false);
        if (rulesButton != null) rulesButton.SetActive(false);
        CreateUtilityButtons();

        SetButtonsActive(false);
    }

    private void CreateUtilityButtons()
    {
        GameObject canvasObject = new GameObject("MapUtilityCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        backpackButton = CreateTextButton("背包", canvasObject.transform, new Vector2(94f, -32f), new Vector2(160f, 44f), OnBackpackButtonClicked);
        rulesButton = CreateTextButton("规则", canvasObject.transform, new Vector2(230f, -32f), new Vector2(100f, 44f), OnRulesButtonClicked);
    }

    private static GameObject CreateTextButton(string label, Transform parent, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(label + "按钮", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        buttonObject.GetComponent<Image>().color = new Color(0.08f, 0.11f, 0.18f, 0.94f);
        buttonObject.GetComponent<Button>().onClick.AddListener(action);

        GameObject textObject = new GameObject("文字", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
        text.text = label;
        text.fontSize = 21f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return buttonObject;
    }

    private void OnBackpackButtonClicked()
    {
        if (backpackDisplay == null)
        {
            backpackDisplay = FindObjectOfType<BackpackDisplay>(true);
        }
        if (backpackDisplay != null)
        {
            backpackDisplay.ToggleBackpack();
        }
        else
        {
            Debug.LogError("BackpackDisplay not found in scene. Please assign it on MapUIController.");
        }
    }

    private void OnRulesButtonClicked()
    {
        if (rulesDisplay == null)
        {
            rulesDisplay = FindObjectOfType<RulesDisplay>(true);
        }
        if (rulesDisplay != null)
        {
            rulesDisplay.ToggleRules();
        }
        else
        {
            Debug.LogError("RulesDisplay not found in scene. Please assign it on MapUIController.");
        }
    }

    private void Update()
    {
        // Condition 1: Time since scene load > 5 seconds
        bool timeCondition = Time.timeSinceLevelLoad > delayBeforeShow;

        // Condition 2: In Map Viewing Phase
        // We assume we are in map phase if the map container is active
        bool mapCondition = false;
        if (mapGenerator != null && mapGenerator.mapContainer != null)
        {
            mapCondition = mapGenerator.mapContainer.gameObject.activeSelf;
        }

        // Final Visibility
        bool utilityOpen = (backpackDisplay != null && backpackDisplay.IsOpen)
            || (rulesDisplay != null && rulesDisplay.IsOpen);
        bool shouldShow = timeCondition && mapCondition && !utilityOpen;
        
        SetButtonsActive(shouldShow);
    }

    private void SetButtonsActive(bool active)
    {
        if (backpackButton != null && backpackButton.activeSelf != active)
        {
            backpackButton.SetActive(active);
        }

        if (rulesButton != null && rulesButton.activeSelf != active)
        {
            rulesButton.SetActive(active);
        }
    }
}
