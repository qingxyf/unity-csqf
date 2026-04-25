using UnityEngine;
using UnityEngine.UI; // 如果需要操作UI

public class CampManager : MonoBehaviour
{
    // 提供一个公开方法，供按钮点击事件调用
    [Header("Settings")]
    public int cardsToGive = 6;
    public void OnRestButtonClicked()
    {
        Debug.Log("玩家在营地选择了休息...");

        if (PlayerStats.Instance != null)
        {
            
            PlayerStats.Instance.HealFull();
            Debug.Log("Camp Item: Player healed to full.");
            
            // Ensure DeckManager exists
            DeckManager.EnsureInstance();
            if (DeckManager.Instance != null)
            {
                // DeckManager.Instance.DrawFromHiddenPool(cardsToGive);
                // Use new logic: Only cards with cost <= 3
                DeckManager.Instance.DrawCardsWithMaxCost(cardsToGive, 3);
                Debug.Log($"Camp Item: Gave {cardsToGive} cards (Cost <= 3) to player.");
            }
        }
        else
        {
            Debug.LogError("未找到 PlayerStats 实例！请确保场景中有挂载 PlayerStats 的对象。");
        }

        FindObjectOfType<GameManager>().CompleteCurrentNode();
    }

    // 如果是3D/2D物体点击，可以使用这个
    private void OnMouseDown()
    {
        OnRestButtonClicked();
    }
}
