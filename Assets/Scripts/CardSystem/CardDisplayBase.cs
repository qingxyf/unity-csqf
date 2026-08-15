using UnityEngine;
using TMPro;
using UnityEngine.UI;

public abstract class CardDisplayBase : MonoBehaviour
{
    private const string UiProxyPrefix = "[UI_Proxy]_";

    [Header("UI Components")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;

    [Header("Data")]
    public CardData cardData;

    public void RefreshDisplay()
    {
        if (cardData == null) return;

        if (nameText != null)
        {
            nameText.text = cardData.cardName;
        }

        if (descriptionText != null)
        {
            string processedText = CardDescriptionFormatter.GetDescription(cardData);
            descriptionText.text = InsertLineBreaks(processedText, 9);
        }

        if (costText != null)
        {
            int effectiveCost = CardCostUtility.GetEffectiveCardCost(cardData);
            costText.text = CardCostUtility.GetCostLabel(cardData);
        }

        EnsureArtForUI();
        FixTextSizeForUI();
    }

    protected abstract void FixTextSizeForUI();

    protected void EnsureArtForUI()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        System.Array.Sort(renderers, (a, b) => a.sortingOrder.CompareTo(b.sortingOrder));

        int insertIndex = 0;
        Vector3 rootScale = transform.localScale;

        foreach (var sr in renderers)
        {
            if (sr.sprite == null) continue;

            string proxyName = $"{UiProxyPrefix}{sr.gameObject.name}";
            Transform existingProxy = transform.Find(proxyName);

            GameObject uiObj;
            if (existingProxy != null)
            {
                uiObj = existingProxy.gameObject;
            }
            else
            {
                uiObj = new GameObject(proxyName, typeof(RectTransform));
                uiObj.transform.SetParent(this.transform, false);
            }

            uiObj.transform.SetSiblingIndex(insertIndex);
            insertIndex++;

            Image img = uiObj.GetComponent<Image>();
            if (img == null) img = uiObj.AddComponent<Image>();

            img.sprite = sr.sprite;
            img.color = sr.color;
            img.raycastTarget = false;

            ConfigureSpriteProxy(sr, img.rectTransform, rootScale);

            sr.enabled = false;
        }
    }

    private void ConfigureSpriteProxy(SpriteRenderer sr, RectTransform proxy, Vector3 rootScale)
    {
        Sprite sprite = sr.sprite;
        float pixelsPerUnit = sprite != null ? sprite.pixelsPerUnit : 100f;
        Vector3 localPosition = transform.InverseTransformPoint(sr.transform.position);
        Vector3 relativeScale = GetRelativeScale(sr.transform);
        Vector2 normalizedPivot = GetNormalizedPivot(sprite);

        proxy.anchorMin = new Vector2(0.5f, 0.5f);
        proxy.anchorMax = new Vector2(0.5f, 0.5f);
        proxy.pivot = normalizedPivot;
        proxy.anchoredPosition = new Vector2(
            localPosition.x * pixelsPerUnit * rootScale.x,
            localPosition.y * pixelsPerUnit * rootScale.y);
        proxy.localScale = new Vector3(
            relativeScale.x * rootScale.x,
            relativeScale.y * rootScale.y,
            1f);
        proxy.localRotation = Quaternion.Inverse(transform.rotation) * sr.transform.rotation;
        proxy.sizeDelta = sprite != null ? sprite.rect.size : Vector2.zero;
    }

    private Vector3 GetRelativeScale(Transform target)
    {
        if (target == transform) return Vector3.one;

        Vector3 rootLossyScale = transform.lossyScale;
        Vector3 targetLossyScale = target.lossyScale;

        return new Vector3(
            SafeDivide(targetLossyScale.x, rootLossyScale.x),
            SafeDivide(targetLossyScale.y, rootLossyScale.y),
            1f);
    }

    private static float SafeDivide(float value, float divisor)
    {
        if (Mathf.Approximately(divisor, 0f)) return 0f;
        return value / divisor;
    }

    private static Vector2 GetNormalizedPivot(Sprite sprite)
    {
        if (sprite == null || sprite.rect.width <= 0f || sprite.rect.height <= 0f)
        {
            return new Vector2(0.5f, 0.5f);
        }

        return new Vector2(
            sprite.pivot.x / sprite.rect.width,
            sprite.pivot.y / sprite.rect.height);
    }

    protected string InsertLineBreaks(string text, int lineLength)
    {
        if (string.IsNullOrEmpty(text)) return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int visibleCount = 0;
        bool inTag = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            sb.Append(c);

            if (c == '<')
            {
                inTag = true;
            }
            else if (c == '>')
            {
                inTag = false;
            }
            else if (!inTag)
            {
                if (c == '\n')
                {
                    visibleCount = 0;
                }
                else
                {
                    visibleCount++;

                    if (visibleCount >= lineLength)
                    {
                        if (i < text.Length - 1)
                        {
                            sb.Append('\n');
                            visibleCount = 0;
                        }
                    }
                }
            }
        }

        return sb.ToString();
    }

    public void Setup(CardData data)
    {
        this.cardData = data;
        RefreshDisplay();
    }

    private void OnValidate()
    {
        RefreshDisplay();
    }
}
