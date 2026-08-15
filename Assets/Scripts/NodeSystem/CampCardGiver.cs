using UnityEngine;
using UnityEngine.EventSystems;

// CampCardGiver 已合并到 CampManager。
// 这个脚本保留作为 UI 点击适配器，转发到 CampManager。
public class CampCardGiver : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        CampManager camp = FindObjectOfType<CampManager>();
        if (camp != null)
            camp.OnRestButtonClicked();
    }
}
