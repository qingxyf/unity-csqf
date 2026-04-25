using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    [Header("Configuration")]
    public string cardResourcePath = "Cards"; // Path inside Resources folder

    [Header("State - Out of Combat")]
    public List<CardData> hiddenCardPool = new List<CardData>(); // The hidden pool
    public List<CardData> backpack = new List<CardData>();       // Player's inventory

    [Header("State - In Combat")]
    public List<CardData> drawPile = new List<CardData>();
    public List<CardData> hand = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();
    public List<CardData> exhaustPile = new List<CardData>(); // Removed from game for this battle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeHiddenPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Ensure Instance is accessible or warn
    public static void EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("DeckManager");
            Instance = go.AddComponent<DeckManager>();
            // Initialize will be called by Awake
        }
    }

    /// <summary>
    /// Initializes or resets the hidden card pool based on rules.
    /// </summary>
    public void InitializeHiddenPool()
    {
        hiddenCardPool.Clear();
        CardData[] allCards = Resources.LoadAll<CardData>(cardResourcePath);

        if (allCards.Length == 0)
        {
            Debug.LogError($"No cards found in Resources/{cardResourcePath}");
            return;
        }

        foreach (var card in allCards)
        {
            int count = 0;
            switch (card.cost)
            {
                case 1:
                    count = 4;
                    break;
                case 2:
                    count = 3;
                    break;
                case 3:
                    count = 2;
                    break;
                case 4:
                case 5:
                    count = 1;
                    break;
                default:
                    // Fallback for 0 or >5 cost if any
                    count = 1;
                    break;
            }

            for (int i = 0; i < count; i++)
            {
                hiddenCardPool.Add(card);
            }
        }

        Shuffle(hiddenCardPool);
        Debug.Log($"Hidden Pool Initialized with {hiddenCardPool.Count} cards.");
    }

    /// <summary>
    /// Draws cards from the hidden pool into the player's backpack (Out of Combat).
    /// </summary>
    public void DrawFromHiddenPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (hiddenCardPool.Count == 0)
            {
                Debug.Log("Hidden Pool empty! Reshuffling...");
                InitializeHiddenPool();
            }

            if (hiddenCardPool.Count > 0)
            {
                CardData drawnCard = hiddenCardPool[0];
                hiddenCardPool.RemoveAt(0);
                backpack.Add(drawnCard);
                Debug.Log($"Obtained card: {drawnCard.cardName}");
            }
        }
    }

    /// <summary>
    /// Draws cards with a specific max cost from the hidden pool.
    /// Used for the initial Camp node.
    /// </summary>
    public void DrawCardsWithMaxCost(int count, int maxCost)
    {
        for (int i = 0; i < count; i++)
        {
            // Find a candidate
            var candidate = hiddenCardPool.FirstOrDefault(c => c.cost <= maxCost);
            
            if (candidate == null)
            {
                // If no card meets criteria, reshuffle or just pick any?
                // Let's try reshuffling once.
                Debug.Log("No valid card found in pool! Reshuffling...");
                InitializeHiddenPool();
                candidate = hiddenCardPool.FirstOrDefault(c => c.cost <= maxCost);
            }

            if (candidate != null)
            {
                hiddenCardPool.Remove(candidate);
                backpack.Add(candidate);
                Debug.Log($"Obtained Low Cost Card: {candidate.cardName} (Cost {candidate.cost})");
            }
            else
            {
                Debug.LogWarning($"Could not find any card with cost <= {maxCost} even after reshuffle.");
            }
        }
    }

    // --- Combat Methods ---

    public void StartCombat()
    {
        // Move all cards from backpack to draw pile
        drawPile.Clear();
        drawPile.AddRange(backpack);
        
        discardPile.Clear();
        hand.Clear();
        exhaustPile.Clear();
        
        Shuffle(drawPile);
        Debug.Log("Combat Started! Deck shuffled.");
        
        // Initial Draw? Usually turn start handles this.
    }

    public void EndCombat()
    {
        // Return all cards to backpack
        backpack.Clear();
        backpack.AddRange(drawPile);
        backpack.AddRange(hand);
        backpack.AddRange(discardPile);
        backpack.AddRange(exhaustPile);
        
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        exhaustPile.Clear();
        
        // Reset Player Battle Stats
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnBattleEnd();
        }

        Debug.Log("Combat Ended! All cards returned to backpack.");
    }

    public void DrawCardInCombat(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                {
                    Debug.Log("No cards left to draw!");
                    break;
                }
                
                // Reshuffle discard into draw
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle(drawPile);
                Debug.Log("Discard pile reshuffled into Draw pile.");
            }

            CardData card = drawPile[0];
            drawPile.RemoveAt(0);
            hand.Add(card);
            
            // Notify UI or Card Spawner to create visual representation
            // For now, we just log it. In a real implementation, we'd invoke an event.
            Debug.Log($"Drew card: {card.cardName}");
            
            // Assuming there's a HandManager or CardSpawner listening to this
            // or we manually spawn it here if we had reference to the UI/Canvas.
            // For this task, we focus on logic.
        }
    }

    /// <summary>
    /// Called when a card is played. Handles discard logic.
    /// </summary>
    public void OnCardPlayed(CardData card, bool shouldDiscard = true)
    {
        if (hand.Contains(card))
        {
            hand.Remove(card);
        }

        // Special handling for "Double Chant" (二重吟唱)
        // Description: "回收卡牌第二次及以后使用时..."
        // "Recycle" implies returning to hand? Or putting back in deck?
        // Usually "Recycle" means return to hand.
        // However, the prompt says: "used cards enter discard pile (except special effects like Double Chant)"
        // If Double Chant returns to hand, we add it back.
        if (card.cardName == "二重吟唱")
        {
            // Logic for Double Chant: It stays in hand or returns to hand.
            // Let's assume it returns to hand.
            hand.Add(card);
            Debug.Log("Double Chant triggered: Card returns to hand!");
            return;
        }

        if (shouldDiscard)
        {
            // Check Neutral Rule: Unless specified, remove from game
            if (card.element == CardElement.Neutral)
            {
                // Exceptions that go to discard pile
                if (card.cardName == "粮草先行")
                {
                     discardPile.Add(card);
                     Debug.Log($"Neutral Card {card.cardName} went to Discard Pile.");
                }
                else
                {
                    exhaustPile.Add(card);
                    Debug.Log($"Neutral Card {card.cardName} Exhausted (Removed from game).");
                }
            }
            else
            {
                discardPile.Add(card);
            }
        }
    }

    public void DiscardHand()
    {
        foreach (var card in hand)
        {
            discardPile.Add(card);
        }
        hand.Clear();
        Debug.Log("Hand discarded.");
    }

    public void AddCardToTop(CardData card)
    {
        drawPile.Insert(0, card);
        Debug.Log($"Card {card.cardName} added to top of draw pile.");
    }

    public void OnEnemyDied()
    {
        // Logic for "安营扎寨" (Set Up Camp): 一名敌人死亡时加入牌堆顶（限一次）
        var campCard = exhaustPile.Find(c => c.cardName == "安营扎寨");
        if (campCard != null)
        {
            exhaustPile.Remove(campCard);
            drawPile.Insert(0, campCard); // 加入牌堆顶
            Debug.Log("安营扎寨: Enemy died, card added to top of draw pile.");
        }
    }

    // --- Helpers ---

    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
