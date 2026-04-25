using UnityEngine;
using UnityEngine.UI;

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

        SetupButton(backpackButton, OnBackpackButtonClicked);

        SetupButton(rulesButton, OnRulesButtonClicked);

        SetButtonsActive(false);
    }

    private void SetupButton(GameObject obj, UnityEngine.Events.UnityAction action)
    {
        if (obj != null)
        {
            Button btn = obj.GetComponent<Button>();
            if (btn == null)
            {
                btn = obj.AddComponent<Button>();
            }
            btn.onClick.AddListener(action);
        }
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
        bool shouldShow = timeCondition && mapCondition;
        
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
