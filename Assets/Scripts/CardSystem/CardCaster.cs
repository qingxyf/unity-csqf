using UnityEngine;
using UnityEngine.EventSystems;

public class CardCaster : MonoBehaviour, IPointerClickHandler
{
    private CardDisplay cardDisplay;

    private void Start()
    {
        ResolveCardDisplay();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Cast();
    }

    public void Cast()
    {
        ResolveCardDisplay();

        if (cardDisplay == null || cardDisplay.cardData == null)
        {
            Debug.LogWarning("CardCaster: No CardDisplay or CardData found!");
            return;
        }

        if (CardEffectManager.Instance == null)
        {
            Debug.LogError("CardEffectManager instance not found!");
            return;
        }

        // Find target enemy
        Enemy target = null;
        if (EnemyManager.Instance != null && EnemyManager.Instance.ActiveEnemies.Count > 0)
        {
            target = EnemyManager.Instance.ActiveEnemies[0];
        }

        bool played = CardEffectManager.Instance.PlayCard(cardDisplay.cardData, target != null ? target.gameObject : null);
        if (played)
            Destroy(gameObject);
    }

    private void ResolveCardDisplay()
    {
        if (cardDisplay != null) return;

        cardDisplay = GetComponent<CardDisplay>();
        if (cardDisplay == null)
            cardDisplay = GetComponentInChildren<CardDisplay>(true);
        if (cardDisplay == null)
            cardDisplay = GetComponentInParent<CardDisplay>();
    }
}
