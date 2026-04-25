using UnityEngine;
using UnityEngine.EventSystems;

public class CampCardGiver : MonoBehaviour, IPointerClickHandler
{
    [Header("Settings")]
    public int cardsToGive = 6;

    public void OnPointerClick(PointerEventData eventData)
    {
        TriggerEffect();
    }

    private void TriggerEffect()
    {
        // 1. Give cards
        if (DeckManager.Instance != null)
        {
            // DeckManager.Instance.DrawFromHiddenPool(cardsToGive);
            DeckManager.Instance.DrawCardsWithMaxCost(cardsToGive, 3);
            Debug.Log($"Camp Item: Gave {cardsToGive} cards (Cost <= 3) to player.");
        }
        else
        {
            Debug.LogError("DeckManager instance not found!");
        }

        // 2. Heal to full
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.HealFull();
            Debug.Log("Camp Item: Player healed to full.");
        }
        else
        {
            Debug.LogError("PlayerStats instance not found!");
        }
        
        // Optional: Disable interactivity after use?
        // GetComponent<Collider2D>().enabled = false;
        // or Destroy(this);
    }
}
