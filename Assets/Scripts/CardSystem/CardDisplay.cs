using UnityEngine;
using TMPro;

public class CardDisplay : CardDisplayBase
{
    protected override void FixTextSizeForUI()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        float rootScale = Mathf.Max(Mathf.Abs(transform.localScale.x), 0.01f);
        float targetScale = 1f / rootScale;

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in texts)
        {
            NormalizeCardText(txt);
            txt.transform.localScale = new Vector3(targetScale, targetScale, 1f);
        }
    }

    private static void NormalizeCardText(TextMeshProUGUI txt)
    {
        if (txt == null) return;

        if (TMP_Settings.defaultFontAsset != null)
            txt.font = TMP_Settings.defaultFontAsset;

        float currentSize = Mathf.Max(txt.fontSize, 0.1f);
        txt.enableAutoSizing = true;
        txt.fontSizeMax = currentSize;
        txt.fontSizeMin = Mathf.Max(0.08f, currentSize * 0.55f);
        txt.overflowMode = TextOverflowModes.Overflow;
        txt.enableWordWrapping = true;
        txt.extraPadding = true;
        txt.richText = true;
    }
}
