using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Presents a terminal card-roguelike result. It can use a scene-provided
/// component or create a readable overlay at runtime when UI assets are not
/// configured yet.
/// </summary>
public class RoguelikeResultPanel : MonoBehaviour
{
    private GameObject overlayRoot;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private Button newRunButton;
    private Button returnToMenuButton;

    public bool IsVisible { get; private set; }
    public RoguelikeRunResult DisplayedResult { get; private set; }

    public void Show(RoguelikeRunResult result, UnityAction newRun, UnityAction returnToMenu)
    {
        gameObject.SetActive(true);
        EnsureUi();

        DisplayedResult = result;
        IsVisible = true;
        overlayRoot.SetActive(true);

        bool victory = result == RoguelikeRunResult.Victory;
        titleText.text = victory ? "远征完成" : "远征失败";
        bodyText.text = victory
            ? "你已击败卡牌 Boss。准备开始下一次远征吗？"
            : "本次远征已经结束。调整牌组后再试一次吧。";

        newRunButton.onClick.RemoveAllListeners();
        returnToMenuButton.onClick.RemoveAllListeners();
        if (newRun != null)
            newRunButton.onClick.AddListener(newRun);
        if (returnToMenu != null)
            returnToMenuButton.onClick.AddListener(returnToMenu);
        returnToMenuButton.gameObject.SetActive(Application.platform != RuntimePlatform.WebGLPlayer);
        newRunButton.GetComponent<RectTransform>().anchoredPosition = Application.platform == RuntimePlatform.WebGLPlayer
            ? new Vector2(0f, -135f)
            : new Vector2(-125f, -135f);
    }

    public void Hide()
    {
        IsVisible = false;
        DisplayedResult = RoguelikeRunResult.None;
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    private void EnsureUi()
    {
        if (overlayRoot != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            canvas = CreateFallbackCanvas();

        overlayRoot = new GameObject("ResultOverlay", typeof(RectTransform));
        overlayRoot.transform.SetParent(canvas.transform, false);
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImage = overlayRoot.AddComponent<Image>();
        overlayImage.color = new Color(0.025f, 0.03f, 0.055f, 0.94f);

        GameObject panel = new GameObject("ResultPanel", typeof(RectTransform));
        panel.transform.SetParent(overlayRoot.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720f, 430f);
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.09f, 0.11f, 0.18f, 1f);

        titleText = CreateText("Title", panel.transform, 42, new Vector2(620f, 70f), new Vector2(0f, 125f));
        bodyText = CreateText("Body", panel.transform, 23, new Vector2(600f, 100f), new Vector2(0f, 25f));
        newRunButton = CreateButton("NewRunButton", panel.transform, "新开一局", new Vector2(-125f, -135f));
        returnToMenuButton = CreateButton("ReturnToMenuButton", panel.transform, "返回主菜单", new Vector2(125f, -135f));
    }

    private Canvas CreateFallbackCanvas()
    {
        GameObject canvasObject = new GameObject("RuntimeRoguelikeResultCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, int size, Vector2 bounds, Vector2 position)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.rectTransform.sizeDelta = bounds;
        text.rectTransform.anchoredPosition = position;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(210f, 58f);
        rect.anchoredPosition = position;
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.23f, 0.31f, 0.48f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, 22, rect.sizeDelta, Vector2.zero);
        text.text = label;
        return button;
    }
}
