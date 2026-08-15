using UnityEngine;
using UnityEngine.EventSystems;

public class HandCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;
    private Vector2 basePosition;
    private Quaternion baseRotation;
    private Vector3 baseScale;
    private int baseSiblingIndex;
    private float hoverLift;
    private float hoverScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Configure(Vector2 position, Quaternion rotation, Vector3 scale, float lift, float scaleMultiplier)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        basePosition = position;
        baseRotation = rotation;
        baseScale = scale;
        hoverLift = lift;
        hoverScale = scaleMultiplier;
        baseSiblingIndex = transform.GetSiblingIndex();
        ApplyBaseState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rectTransform == null) return;

        baseSiblingIndex = transform.GetSiblingIndex();
        transform.SetAsLastSibling();
        rectTransform.anchoredPosition = basePosition + Vector2.up * hoverLift;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyBaseState();
        int siblingCount = transform.parent != null ? transform.parent.childCount : 0;
        if (siblingCount > 0)
            transform.SetSiblingIndex(Mathf.Clamp(baseSiblingIndex, 0, siblingCount - 1));
    }

    private void ApplyBaseState()
    {
        if (rectTransform == null) return;

        rectTransform.anchoredPosition = basePosition;
        rectTransform.localRotation = baseRotation;
        rectTransform.localScale = baseScale;
    }
}
