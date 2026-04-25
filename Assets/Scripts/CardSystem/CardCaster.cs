using UnityEngine;
using UnityEngine.EventSystems;

public class CardCaster : MonoBehaviour, IPointerClickHandler
{
    private CardDisplay cardDisplay;

    private void Start()
    {
        cardDisplay = GetComponent<CardDisplay>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Cast();
    }

    public void Cast()
    {
        if (cardDisplay == null || cardDisplay.cardData == null)
        {
            Debug.LogWarning("CardCaster: No CardDisplay or CardData found!");
            return;
        }

        // Simple targeting logic for testing:
        // 1. Find an Enemy under the mouse? (Requires PhysicsRaycaster)
        // 2. Or just find the FIRST enemy in the scene.
        Enemy target = FindObjectOfType<Enemy>();

        if (CardEffectManager.Instance != null)
        {
            // Logic for playing a card:
            // 1. Remove from logical hand (so it doesn't get affected by its own effects like "Discard Hand")
            if (DeckManager.Instance != null)
            {
                if (DeckManager.Instance.hand.Contains(cardDisplay.cardData))
                {
                    DeckManager.Instance.hand.Remove(cardDisplay.cardData);
                }
            }

            // 2. Trigger Effect
            CardEffectManager.Instance.PlayCard(cardDisplay.cardData, target != null ? target.gameObject : null);
            
            // 3. Handle Card Lifecycle (Exhaust/Discard)
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.OnCardPlayed(cardDisplay.cardData);
            }
            
            // 4. Destroy visual object
            Destroy(gameObject); 
        }
        else
        {
            Debug.LogError("CardEffectManager instance not found! Please add CardEffectManager to the scene.");
        }
    }
}
