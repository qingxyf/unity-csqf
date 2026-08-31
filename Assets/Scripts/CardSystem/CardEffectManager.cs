using UnityEngine;
using System.Collections.Generic;

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;

    [Header("Runtime Data")]
    public int doubleChantUseCount = 0;
    public int shiningGloryDamageReduction = 0;
    public bool nextCardDoubleEffect = false;
    public int fireCardsPlayedThisTurn = 0;

    private int waterCostReductionsAvailable = 0;
    private int coldSpringBonusDamage = 0;
    private CardData pendingSupplyLineReturnCard;
    private CardData pendingDodgeReturnCard;
    private bool supplyLineReturnTriggered;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public static void EnsureInstance()
    {
        if (Instance != null) return;

        CardEffectManager existing = FindObjectOfType<CardEffectManager>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject go = new GameObject("CardEffectManager");
        go.AddComponent<CardEffectManager>();
    }

    /// <summary>
    /// Clears state that is only meaningful within one roguelike run. This
    /// manager persists between combats, so terminal results must reset it
    /// explicitly before a new map is generated.
    /// </summary>
    public void ResetForNewRun()
    {
        if (DeckManager.Instance != null)
            DeckManager.Instance.CardDrawn -= OnSupplyLineWatchedCardDrawn;

        doubleChantUseCount = 0;
        shiningGloryDamageReduction = 0;
        nextCardDoubleEffect = false;
        fireCardsPlayedThisTurn = 0;
        waterCostReductionsAvailable = 0;
        coldSpringBonusDamage = 0;
        pendingSupplyLineReturnCard = null;
        pendingDodgeReturnCard = null;
        supplyLineReturnTriggered = false;
    }

    public void OnPlayerTurnStart()
    {
        ResolvePendingDodgeReturn();
        BeginWatchingSupplyLineDraw();

        CollectibleManager.OnPlayerTurnStart();
        fireCardsPlayedThisTurn = 0;
        waterCostReductionsAvailable = 0;
        coldSpringBonusDamage = 0;

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.StartTurn();
        }

        EndWatchingSupplyLineDraw();

        var enemies = EnemyManager.Instance != null
            ? EnemyManager.Instance.ActiveEnemies
            : new List<Enemy>();

        foreach (var enemy in new List<Enemy>(enemies))
        {
            if (enemy != null)
                enemy.ProcessTurnStart();
        }
    }

    public void OnPlayerTurnEnd()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.EndTurn();
        }
    }

    public void OnEnemyTurnEnd()
    {
        var enemies = EnemyManager.Instance != null
            ? EnemyManager.Instance.ActiveEnemies
            : new List<Enemy>();

        foreach (var enemy in new List<Enemy>(enemies))
        {
            if (enemy != null)
                enemy.ProcessTurnEnd();
        }
    }

    public void OnEnemyKilled()
    {
    }

    public void RequestEndPlayerTurn()
    {
        CombatController controller = FindObjectOfType<CombatController>();
        if (controller != null)
        {
            controller.RequestEndPlayerTurn();
            return;
        }

        OnPlayerTurnEnd();
    }

    public bool CanPlayCard(CardData card)
    {
        if (card == null || PlayerStats.Instance == null) return false;
        if (PlayerStats.Instance.HasStatus(StatusType.Silenced)) return false;
        return PlayerStats.Instance.currentMana >= CardCostUtility.GetEffectiveCardCost(card);
    }

    public bool PlayCard(CardData card, GameObject target)
    {
        if (card == null) return false;
        if (PlayerStats.Instance == null) return false;

        PlayerStats player = PlayerStats.Instance;
        Enemy targetEnemy = target != null ? target.GetComponent<Enemy>() : null;

        if (player.HasStatus(StatusType.Silenced))
        {
            Debug.Log("Player is silenced and cannot play cards.");
            return false;
        }

        int effectiveCost = CardCostUtility.GetEffectiveCardCost(card);
        if (player.currentMana < effectiveCost)
        {
            Debug.Log("Not enough mana!");
            return false;
        }

        Debug.Log($"Playing Card: {card.cardName}");

        CardEffect effect = CardEffectCatalog.Resolve(card);
        if (effect == null)
        {
            Debug.LogWarning($"Card effect not assigned: {card.cardName}");
            return false;
        }

        SpendTemporaryCostReduction(card);
        player.currentMana -= effectiveCost;

        var context = new CardEffectContext
        {
            Card = card,
            Player = player,
            Target = targetEnemy,
            AllEnemies = EnemyManager.Instance != null
                ? new List<Enemy>(EnemyManager.Instance.ActiveEnemies)
                : new List<Enemy>(),
            Deck = DeckManager.Instance,
            EffectManager = this
        };

        int repeats = 1;
        if (nextCardDoubleEffect)
        {
            repeats = 2;
            nextCardDoubleEffect = false;
            Debug.Log("Double Effect Triggered!");
        }

        for (int i = 0; i < repeats; i++)
        {
            if (DeckManager.Instance != null)
                DeckManager.Instance.hand.Remove(card);

            effect.Execute(context);
        }

        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnCardPlayed(card, !context.ShufflePlayedCardIntoDrawPile);
            if (context.ShufflePlayedCardIntoDrawPile)
                DeckManager.Instance.ShuffleCardIntoDrawPile(card);
        }

        CollectibleManager.NotifyCardPlayed(card);
        NotifyCardPlayed(card);

        if (context.EndPlayerTurnAfterPlay)
            RequestEndPlayerTurn();

        return true;
    }

    public int GetTemporaryCostReduction(CardData card)
    {
        if (card == null) return 0;

        int reduction = 0;
        if (card.cardName == "余烬连唱")
            reduction += Mathf.Max(0, fireCardsPlayedThisTurn);

        if (waterCostReductionsAvailable > 0 && card.element == CardElement.Water)
            reduction += 1;

        return Mathf.Min(reduction, Mathf.Max(0, card.cost));
    }

    public void AddWaterCostReduction(int count)
    {
        waterCostReductionsAvailable += Mathf.Max(0, count);
    }

    public void EnableColdSpringBonus(int damage)
    {
        coldSpringBonusDamage = Mathf.Max(coldSpringBonusDamage, damage);
    }

    public int ConsumeColdSpringBonus()
    {
        int damage = coldSpringBonusDamage;
        coldSpringBonusDamage = 0;
        return damage;
    }

    public void ScheduleSupplyLineReturn(CardData card)
    {
        pendingSupplyLineReturnCard = card;
    }

    public void ScheduleDodgeReturn(CardData card)
    {
        pendingDodgeReturnCard = card;
    }

    private void SpendTemporaryCostReduction(CardData card)
    {
        if (card == null) return;
        if (waterCostReductionsAvailable > 0 && card.element == CardElement.Water)
            waterCostReductionsAvailable--;
    }

    private void NotifyCardPlayed(CardData card)
    {
        if (card != null && card.element == CardElement.Fire)
            fireCardsPlayedThisTurn++;
    }

    private void ResolvePendingDodgeReturn()
    {
        if (pendingDodgeReturnCard == null) return;
        if (PlayerStats.Instance != null && !PlayerStats.Instance.HasLostHealthThisTurn && DeckManager.Instance != null)
            DeckManager.Instance.ShuffleCardIntoDrawPile(pendingDodgeReturnCard);

        pendingDodgeReturnCard = null;
    }

    private void BeginWatchingSupplyLineDraw()
    {
        if (pendingSupplyLineReturnCard == null || DeckManager.Instance == null) return;

        supplyLineReturnTriggered = false;
        DeckManager.Instance.CardDrawn -= OnSupplyLineWatchedCardDrawn;
        DeckManager.Instance.CardDrawn += OnSupplyLineWatchedCardDrawn;
    }

    private void EndWatchingSupplyLineDraw()
    {
        if (pendingSupplyLineReturnCard == null || DeckManager.Instance == null) return;

        DeckManager.Instance.CardDrawn -= OnSupplyLineWatchedCardDrawn;
        if (supplyLineReturnTriggered)
            DeckManager.Instance.ShuffleCardIntoDrawPile(pendingSupplyLineReturnCard);

        pendingSupplyLineReturnCard = null;
        supplyLineReturnTriggered = false;
    }

    private void OnSupplyLineWatchedCardDrawn(CardData card)
    {
        if (card != null && card.element == CardElement.Neutral)
            supplyLineReturnTriggered = true;
    }
}
