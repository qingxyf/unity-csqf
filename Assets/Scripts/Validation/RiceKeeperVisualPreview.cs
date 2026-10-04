#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Development-build-only, deterministic route through the real rice-camp
/// event chain.  It is deliberately separate from the normal event pool so
/// a screenshot build can reach the challenger without altering a run.
/// </summary>
public class RiceKeeperVisualPreview : MonoBehaviour
{
    public enum PreviewStep
    {
        Booting,
        WildRiceCamp,
        WaitingForWildCampCompletion,
        RiceOwnerReckoning,
        Combat,
        WaitingForReckoningCompletion,
        Complete,
        Defeated
    }

    public PreviewStep CurrentStep { get; private set; } = PreviewStep.Booting;
    public EventManager CurrentEventManager { get; private set; }
    public CombatController ActiveCombat => CurrentEventManager != null ? CurrentEventManager.ActiveEventCombat : null;

    private GameManager gameManager;
    private EventData wildRiceCamp;
    private EventData riceOwnerReckoning;
    private readonly List<GameObject> previewNodes = new List<GameObject>();
    private GameObject previewOverlay;
    private TextMeshProUGUI stepText;
    private Button slowMotionButton;
    private float originalTimeScale = 1f;
    private bool slowMotion;

    private void Awake()
    {
        originalTimeScale = Time.timeScale;
    }

    private IEnumerator Start()
    {
        // Let the authored scene create its normal managers first, then reset
        // through the normal run lifecycle before entering the preview route.
        yield return null;
        BeginPreview();
    }

    private void Update()
    {
        if (gameManager == null)
            return;

        if (gameManager.RunController != null && gameManager.RunController.IsTerminal)
        {
            if (CurrentStep != PreviewStep.Defeated)
            {
                CurrentStep = PreviewStep.Defeated;
                HideMap();
                RefreshOverlay();
            }
            return;
        }

        if (CurrentStep == PreviewStep.WildRiceCamp && CurrentEventManager != null && CurrentEventManager.CompletionRequested)
        {
            CurrentStep = PreviewStep.WaitingForWildCampCompletion;
            RefreshOverlay();
        }

        // Continue can be invoked before this Update sees the selected choice.
        // The bound node remains valid after its EventManager is destroyed.
        if ((CurrentStep == PreviewStep.WildRiceCamp || CurrentStep == PreviewStep.WaitingForWildCampCompletion)
            && gameManager.CurrentNodeCompleted)
        {
            if (EventPool.IsUnlocked("rice-owner-reckoning"))
                EnterRiceOwnerReckoning();
            else
                FinishPreview();
        }

        if (CurrentStep == PreviewStep.RiceOwnerReckoning && CurrentEventManager != null)
        {
            if (CurrentEventManager.ActiveEventCombat != null)
            {
                CurrentStep = PreviewStep.Combat;
                RefreshOverlay();
            }
            else if (CurrentEventManager.CompletionRequested)
            {
                CurrentStep = PreviewStep.WaitingForReckoningCompletion;
                RefreshOverlay();
            }
        }

        if ((CurrentStep == PreviewStep.RiceOwnerReckoning || CurrentStep == PreviewStep.WaitingForReckoningCompletion
            || CurrentStep == PreviewStep.Combat) && gameManager.CurrentNodeCompleted)
            FinishPreview();
    }

    /// <summary>Restarts only this deterministic validation route.</summary>
    public void BeginPreview()
    {
        RestoreTimeScale();
        ResolveDependencies();
        if (gameManager == null || wildRiceCamp == null || riceOwnerReckoning == null)
        {
            Debug.LogError("RiceKeeperVisualPreview: required game managers or event resources are missing.");
            return;
        }

        if (gameManager.RunController == null || !gameManager.RunController.StartNewRun())
        {
            Debug.LogError("RiceKeeperVisualPreview: could not start a fresh roguelike run.");
            return;
        }

        ClearPreviewNodes();
        // This is the same low-cost starter-deck method used by the camp flow,
        // rather than adding test-only damage or cards.
        DeckManager.Instance.DrawCardsWithMaxCost(10, 3);
        BuildOverlay();
        EnterEvent(wildRiceCamp, PreviewStep.WildRiceCamp);
    }

    private void ResolveDependencies()
    {
        gameManager = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();
        wildRiceCamp = Resources.Load<EventData>("Events/Event_野生营地");
        riceOwnerReckoning = Resources.Load<EventData>("Events/Event_大白饭的讨债人");
    }

    private void EnterRiceOwnerReckoning()
    {
        if (!EventPool.IsUnlocked("rice-owner-reckoning"))
            return;
        EnterEvent(riceOwnerReckoning, PreviewStep.RiceOwnerReckoning);
    }

    private void FinishPreview()
    {
        CurrentStep = PreviewStep.Complete;
        HideMap();
        RefreshOverlay();
    }

    private void EnterEvent(EventData eventData, PreviewStep step)
    {
        if (eventData == null || gameManager == null)
            return;

        GameObject nodeObject = new GameObject("RiceKeeperPreview_" + eventData.eventId);
        previewNodes.Add(nodeObject);
        Node node = nodeObject.AddComponent<Node>();
        node.type = NodeType.Event;
        node.depth = 0;
        node.isActive = true;
        node.UpdateVisuals();

        gameManager.SelectNode(node);
        CurrentEventManager = null;
        foreach (EventManager candidate in FindObjectsOfType<EventManager>())
        {
            if (candidate.BoundNode == node && candidate.BoundSessionToken == gameManager.CurrentContentSession)
            {
                CurrentEventManager = candidate;
                break;
            }
        }
        if (CurrentEventManager == null)
        {
            Debug.LogError("RiceKeeperVisualPreview: EventManager did not load for preview node.");
            return;
        }

        // Configure before EventManager.Start. These are the only two source
        // assets used by this preview: the follow-up remains unavailable until
        // the real full-meal choice unlocks it. The global run pool is untouched.
        CurrentEventManager.autoLoadResources = false;
        CurrentEventManager.eventPool = new List<EventData> { wildRiceCamp, riceOwnerReckoning };
        CurrentStep = step;
        HideMap();
        RefreshOverlay();
    }

    private void BuildOverlay()
    {
        if (previewOverlay != null)
            return;

        previewOverlay = new GameObject("RiceKeeperPreviewOverlay");
        Canvas canvas = previewOverlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = previewOverlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        previewOverlay.AddComponent<GraphicRaycaster>();

        GameObject panel = CreatePanel(previewOverlay.transform, new Vector2(118f, 166f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(6f, -14f);

        stepText = CreateText(panel.transform, "独立战斗验收", 12, new Vector2(108f, 56f), new Vector2(0f, -34f));
        stepText.rectTransform.anchorMin = stepText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        Button restart = CreateButton(panel.transform, "重新验收", new Vector2(106f, 34f), new Vector2(0f, -88f));
        restart.onClick.AddListener(BeginPreview);
        slowMotionButton = CreateButton(panel.transform, "慢速播放：关", new Vector2(106f, 34f), new Vector2(0f, -132f));
        slowMotionButton.onClick.AddListener(ToggleSlowMotion);
    }

    private void ToggleSlowMotion()
    {
        slowMotion = !slowMotion;
        Time.timeScale = slowMotion ? 0.2f : originalTimeScale;
        RefreshOverlay();
    }

    private void RefreshOverlay()
    {
        if (stepText != null)
            stepText.text = "独立战斗验收\n" + GetStepLabel(CurrentStep);
        if (slowMotionButton != null)
        {
            TextMeshProUGUI text = slowMotionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = slowMotion ? "慢速播放：开" : "慢速播放：关";
        }
    }

    private static string GetStepLabel(PreviewStep step)
    {
        switch (step)
        {
            case PreviewStep.WildRiceCamp: return "吃光大米饭";
            case PreviewStep.WaitingForWildCampCompletion: return "继续前往讨债人";
            case PreviewStep.RiceOwnerReckoning: return "选择艰难战斗";
            case PreviewStep.Combat: return "与蓝色大肥鱼战斗";
            case PreviewStep.WaitingForReckoningCompletion: return "讨债已结清";
            case PreviewStep.Complete: return "验收完成，可重新开始";
            case PreviewStep.Defeated: return "本次战败，可重新开始";
            default: return "准备中";
        }
    }

    private void HideMap()
    {
        if (gameManager != null && gameManager.mapGenerator != null && gameManager.mapGenerator.mapContainer != null)
            gameManager.mapGenerator.mapContainer.gameObject.SetActive(false);
    }

    private void ClearPreviewNodes()
    {
        foreach (GameObject node in previewNodes)
        {
            if (node != null)
                Destroy(node);
        }
        previewNodes.Clear();
    }

    private static GameObject CreatePanel(Transform parent, Vector2 size)
    {
        GameObject panel = new GameObject("PreviewPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<RectTransform>().sizeDelta = size;
        panel.GetComponent<Image>().color = new Color(0.035f, 0.06f, 0.12f, 0.92f);
        return panel;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string text, int size, Vector2 area, Vector2 position)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.rectTransform.sizeDelta = area;
        label.rectTransform.anchoredPosition = position;
        return label;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 size, Vector2 position)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.29f, 0.48f, 1f);
        CreateText(buttonObject.transform, label, 14, size, Vector2.zero);
        return buttonObject.GetComponent<Button>();
    }

    private void OnDestroy()
    {
        RestoreTimeScale();
        ClearPreviewNodes();
        if (previewOverlay != null) Destroy(previewOverlay);
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = originalTimeScale;
        slowMotion = false;
    }
}
#endif
