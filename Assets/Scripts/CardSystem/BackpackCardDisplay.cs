using UnityEngine;
using TMPro;

public class BackpackCardDisplay : CardDisplayBase
{
    protected override void FixTextSizeForUI()
    {
        Transform textContainer = null;
        if (nameText != null) textContainer = nameText.transform.parent;
        else if (descriptionText != null) textContainer = descriptionText.transform.parent;

        if (textContainer != null)
        {
            textContainer.localScale = new Vector3(20f, 20f, 1f);
        }

        if (nameText != null)
        {
            nameText.transform.localScale = new Vector3(5f, 5f, 1f);
            nameText.rectTransform.anchoredPosition = new Vector2(444f, -163.52f);
        }

        if (descriptionText != null)
        {
            descriptionText.transform.localScale = new Vector3(5f, 5f, 1f);
            descriptionText.rectTransform.anchoredPosition = new Vector2(443.23f, -166.45f);
        }

        if (costText != null)
        {
            costText.transform.localScale = new Vector3(5f, 5f, 1f);
            costText.rectTransform.anchoredPosition = new Vector2(449.3f, -155.6f);
        }
    }
}
