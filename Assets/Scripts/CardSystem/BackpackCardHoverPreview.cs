using UnityEngine;
using UnityEngine.EventSystems;

public class BackpackCardHoverPreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CardData cardData;
    public Transform stagePreviewParent;

    GameObject previewInstance;

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowPreview();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HidePreview();
    }

    void ShowPreview()
    {
        if (cardData == null) return;
        if (previewInstance != null) return;

        string prefabName = cardData.cardName;
        if (string.IsNullOrEmpty(prefabName)) return;

        GameObject prefab = CardResourceUtility.LoadCardPrefab(cardData);
        if (prefab == null) return;

        Transform parent = stagePreviewParent;
        if (parent != null)
        {
            previewInstance = Instantiate(prefab, parent);
            previewInstance.transform.localPosition = new Vector3(4.01f, -0.93f, 0f);
            previewInstance.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }
        else
        {
            previewInstance = Instantiate(prefab);
            previewInstance.transform.position = new Vector3(4.01f, -0.93f, 0f);
            previewInstance.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }

        InitializePreviewDisplay();
    }

    void InitializePreviewDisplay()
    {
        if (previewInstance == null) return;

        CardDisplayBase display = previewInstance.GetComponent<CardDisplayBase>();
        if (display != null) display.Setup(cardData);
    }

    void HidePreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
    }

    void OnDisable()
    {
        HidePreview();
    }
}
