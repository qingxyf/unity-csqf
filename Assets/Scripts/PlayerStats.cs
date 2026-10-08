using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("基础属性")]
    public int baseMaxHealth = 100;
    public int maxHealth = 100;
    public int currentHealth;
    public int baseAttack = 10;
    public int initialMana = 6;
    public int baseMaxMana = 10;
    public int maxMana = 10;
    public int manaRegenPerTurn = 2;
    public int currentMana;
    public int currentShield = 0;
    public int startingGold = 80;
    public int gold = 80;

    public List<GameObject> damageSourcesThisTurn = new List<GameObject>();
    public bool hasFlameShield = false;
    public float damageReductionNextHit = 0f;
    public int flatDamageReductionNextHit = 0;
    public bool isUntargetable = false;
    public int regenerationTurns = 0;

    // Status effects managed by the StatusEffect system
    private List<StatusEffect> activeEffects = new List<StatusEffect>();

    // Legacy accessors for card effects that still set these directly
    public int burnTurns
    {
        get { var e = GetEffect(StatusType.Burn); return e != null ? e.Duration : 0; }
        set { SetSimpleEffect(StatusType.Burn, value); }
    }
    public int corrosionTurns
    {
        get { var e = GetEffect(StatusType.Corrosion); return e != null ? e.Duration : 0; }
        set { SetSimpleEffect(StatusType.Corrosion, value); }
    }
    public int frostTurns
    {
        get { var e = GetEffect(StatusType.Frost); return e != null ? e.Duration : 0; }
        set { SetSimpleEffect(StatusType.Frost, value); }
    }
    public int bleedTurns
    {
        get { var e = GetEffect(StatusType.Bleed); return e != null ? e.Duration : 0; }
        set { SetSimpleEffect(StatusType.Bleed, value); }
    }

    private void SetSimpleEffect(StatusType type, int duration)
    {
        activeEffects.RemoveAll(e => e.Type == type);
        if (duration > 0)
        {
            var effect = StatusEffectFactory.Create(type, duration);
            if (effect != null) activeEffects.Add(effect);
        }
    }

    private StatusEffect GetEffect(StatusType type)
    {
        return activeEffects.Find(e => e.Type == type);
    }

    public bool HasStatus(StatusType type)
    {
        return GetEffect(type) != null;
    }

    public void RemoveStatus(StatusType type)
    {
        activeEffects.RemoveAll(e => e.Type == type);
    }

    public IReadOnlyList<StatusEffect> GetActiveEffects()
    {
        return activeEffects;
    }

    public int extraDrawsNextTurn = 0;
    public int extraManaNextTurn = 0;
    public int delayedHealNextTurn = 0;
    public bool natureGuardActive = false;
    public bool HasLostHealthThisTurn => lostHealthThisTurn;

    public float lifeDamageReductionThisTurn = 0f;
    public int thornCounterAttackDamage = 0;
    private bool lostHealthThisTurn = false;
    private int starPrayerTurns = 0;
    private int starPrayerShield = 0;
    private int starPrayerHeal = 0;

    [Header("单例模式")]
    public static PlayerStats Instance;

    private bool initialized;
    private bool isInitialCombatTurn;
    private int combatTurn;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public static void EnsureInstance()
    {
        if (Instance != null) return;

        // EditMode tests can construct the component without Unity invoking
        // Awake. Reuse an existing scene component before creating a duplicate.
        PlayerStats existing = FindObjectOfType<PlayerStats>();
        if (existing != null)
        {
            Instance = existing;
            if (!existing.initialized)
                existing.InitializeStats();
            return;
        }

        GameObject go = new GameObject("PlayerStats");
        PlayerStats stats = go.AddComponent<PlayerStats>();
        stats.InitializeStats();
    }

    void Start()
    {
        if (!initialized)
            InitializeStats();
    }

    public void InitializeStats()
    {
        if (Instance == null)
            Instance = this;

        initialized = true;
        maxHealth = baseMaxHealth;
        currentHealth = maxHealth;
        maxMana = baseMaxMana;
        currentMana = initialMana;
        gold = startingGold;
        currentShield = 0;
        damageSourcesThisTurn.Clear();
        hasFlameShield = false;
        activeEffects.Clear();
        Debug.Log($"主角属性初始化: HP {currentHealth}, ATK {baseAttack}, Mana {currentMana}");
    }

    /// <summary>
    /// Restores all state that belongs to one roguelike run while keeping the
    /// configured starting stats intact for the next run.
    /// </summary>
    public void ResetForNewRun()
    {
        InitializeStats();
        OnBattleEnd();
        isInitialCombatTurn = false;
    }

    public void OnBattleEnd()
    {
        combatTurn = 0;
        isInitialCombatTurn = false;
        currentShield = 0;
        damageSourcesThisTurn.Clear();
        hasFlameShield = false;
        regenerationTurns = 0;
        isUntargetable = false;
        damageReductionNextHit = 0f;
        flatDamageReductionNextHit = 0;
        extraDrawsNextTurn = 0;
        extraManaNextTurn = 0;
        delayedHealNextTurn = 0;
        natureGuardActive = false;
        lifeDamageReductionThisTurn = 0f;
        thornCounterAttackDamage = 0;
        lostHealthThisTurn = false;
        starPrayerTurns = 0;
        starPrayerShield = 0;
        starPrayerHeal = 0;
        activeEffects.Clear();
    }

    public void BeginCombat()
    {
        combatTurn = 0;
        currentMana = Mathf.Clamp(initialMana, 0, maxMana);
        AddShield(CollectibleManager.GetStartShield());
        isInitialCombatTurn = true;
    }

    public void StartTurn()
    {
        combatTurn++;
        if (isInitialCombatTurn)
        {
            isInitialCombatTurn = false;
        }
        else
        {
            int regeneration = manaRegenPerTurn + (combatTurn >= 3 ? 1 : 0) + (combatTurn >= 5 ? 1 : 0);
            RestoreMana(regeneration);
        }

        damageSourcesThisTurn.Clear();
        hasFlameShield = false;
        isUntargetable = false;
        natureGuardActive = false;
        lifeDamageReductionThisTurn = 0f;
        thornCounterAttackDamage = 0;

        if (starPrayerTurns > 0)
        {
            AddShield(starPrayerShield);
            if (!lostHealthThisTurn)
                Heal(starPrayerHeal);
            starPrayerTurns--;
        }

        lostHealthThisTurn = false;

        CollectibleManager.ApplyBattleTurnEffects(this, combatTurn);

        // Process status effects at turn start
        foreach (var effect in new List<StatusEffect>(activeEffects))
        {
            effect.OnTurnStart(this);
        }

        // Tick and remove expired
        foreach (var effect in new List<StatusEffect>(activeEffects))
        {
            effect.TickDuration();
        }
        activeEffects.RemoveAll(e => e.IsExpired);

        // Delayed Heal
        if (delayedHealNextTurn > 0)
        {
            Heal(delayedHealNextTurn);
            Debug.Log($"Delayed Heal: {delayedHealNextTurn} HP");
            delayedHealNextTurn = 0;
        }

        if (extraDrawsNextTurn > 0)
        {
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.DrawCardInCombat(extraDrawsNextTurn);
            }
            extraDrawsNextTurn = 0;
        }

        if (extraManaNextTurn > 0)
        {
            RestoreMana(extraManaNextTurn);
            extraManaNextTurn = 0;
        }

        Debug.Log("Player Turn Start");
    }

    public void EndTurn()
    {
        if (regenerationTurns > 0)
        {
            Heal(Mathf.FloorToInt(maxHealth * 0.2f));
            regenerationTurns--;
        }
    }

    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        if (maxHealth < 1) maxHealth = 1;
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        if (currentHealth < 1) currentHealth = 1;
    }

    public void IncreaseMaxMana(int amount)
    {
        maxMana += amount;
        if (maxMana < 1) maxMana = 1;
        currentMana += amount;
        if (currentMana > maxMana) currentMana = maxMana;
        if (currentMana < 0) currentMana = 0;
    }

    public void SetHealth(int value)
    {
        currentHealth = value;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        if (currentHealth < 0) currentHealth = 0;
    }

    public void HealFull()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
    }

    public bool PayHealth(int amount)
    {
        if (amount <= 0) return true;
        if (currentHealth <= amount) return false;

        currentHealth -= amount;
        if (currentHealth < 1) currentHealth = 1;
        return true;
    }

    public void GainGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;

        gold -= amount;
        return true;
    }

    public void AddShield(int amount)
    {
        currentShield += amount;
    }

    public void ApplyStarPrayer(int duration, int shield, int heal)
    {
        starPrayerTurns = Mathf.Max(starPrayerTurns, duration);
        starPrayerShield = Mathf.Max(starPrayerShield, shield);
        starPrayerHeal = Mathf.Max(starPrayerHeal, heal);
    }

    public void ApplyStatus(StatusType type, int duration, int value = 0)
    {
        SetSimpleEffect(type, duration);
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => currentHealth <= 0;

    public void RestoreMana(int amount)
    {
        currentMana += amount;
        if (currentMana > maxMana) currentMana = maxMana;
    }

    public void TakeDamage(int damage, GameObject source = null)
    {
        if (isUntargetable)
        {
            Debug.Log("Player is Untargetable! Damage avoided.");
            return;
        }

        // Corrosion on-damage bonus
        var corrosion = GetEffect(StatusType.Corrosion);
        if (corrosion != null)
        {
            damage += 5;
        }

        if (source != null && !damageSourcesThisTurn.Contains(source))
        {
            damageSourcesThisTurn.Add(source);
        }

        // Flame Shield
        if (hasFlameShield && source != null)
        {
            var enemy = source.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.ApplyStatus(StatusType.Burn, 2);
            }
        }

        // Nature Guard
        if (natureGuardActive && source != null)
        {
            var enemy = source.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(10, DamageType.Nature);
            }
        }

        if (damageReductionNextHit > 0)
        {
            damage = Mathf.FloorToInt(damage * (1f - damageReductionNextHit));
            damageReductionNextHit = 0f;
        }

        if (flatDamageReductionNextHit > 0)
        {
            damage -= flatDamageReductionNextHit;
            if (damage < 0) damage = 0;
            flatDamageReductionNextHit = 0;
        }

        damage = CollectibleManager.ReduceIncomingDamage(damage);

        if (currentShield > 0)
        {
            if (currentShield >= damage)
            {
                currentShield -= damage;
                damage = 0;
            }
            else
            {
                damage -= currentShield;
                currentShield = 0;
            }
        }

        if (damage > 0 && lifeDamageReductionThisTurn > 0f)
            damage = Mathf.FloorToInt(damage * (1f - lifeDamageReductionThisTurn));

        if (damage > 0)
            lostHealthThisTurn = true;

        if (thornCounterAttackDamage > 0 && source != null)
        {
            Enemy enemy = source.GetComponent<Enemy>();
            if (enemy != null)
                enemy.TakeDamage(thornCounterAttackDamage, DamageType.Nature);
        }

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
    }

    // IDamageable.TakeDamage (used by StatusEffect system)
    void IDamageable.TakeDamage(int damage, DamageType type)
    {
        TakeDamage(damage, null);
    }

    public void Cleanse()
    {
        activeEffects.RemoveAll(e =>
            e.Type == StatusType.Burn ||
            e.Type == StatusType.Corrosion ||
            e.Type == StatusType.Frost ||
            e.Type == StatusType.Freeze ||
            e.Type == StatusType.Poison ||
            e.Type == StatusType.Stun ||
            e.Type == StatusType.Weak ||
            e.Type == StatusType.Vulnerable ||
            e.Type == StatusType.Bleed ||
            e.Type == StatusType.Confused ||
            e.Type == StatusType.Silenced ||
            e.Type == StatusType.Rooted ||
            e.Type == StatusType.Parasite);
        Debug.Log("Player Cleansed!");
    }

    public int GetAttackDamage()
    {
        int dmg = baseAttack;
        if (GetEffect(StatusType.Burn) != null)
        {
            dmg /= 2;
        }
        return dmg;
    }
}

public enum CollectibleEffectType
{
    MaxHealth,
    MaxMana,
    ElementDamageBonus,
    FirstElementCardCostReduction,
    StartShield,
    ShopDiscountPercent,
    EverySecondTurnShield,
    EarlyBattleRecovery,
    ChanceDamageReduction
}

public static class CollectibleManager
{
    private static readonly List<CollectibleData> ownedCollectibles = new List<CollectibleData>();
    private static readonly HashSet<CardElement> discountedElementsThisTurn = new HashSet<CardElement>();
    private static readonly HashSet<CollectibleData> runtimeCollectibles = new HashSet<CollectibleData>();
    public static string PendingShopCollectibleId { get; private set; }

    public static IReadOnlyList<CollectibleData> OwnedCollectibles => ownedCollectibles;

    public static void Clear()
    {
        ownedCollectibles.Clear();
        discountedElementsThisTurn.Clear();
        PendingShopCollectibleId = null;
    }

    /// <summary>
    /// Clears the current run and disposes only collectible instances created
    /// by this manager. Serialized collectible assets are never destroyed.
    /// </summary>
    public static void ResetForNewRun()
    {
        foreach (CollectibleData collectible in runtimeCollectibles)
        {
            if (collectible == null) continue;

            if (Application.isPlaying)
                Object.Destroy(collectible);
            else
                Object.DestroyImmediate(collectible);
        }

        runtimeCollectibles.Clear();
        Clear();
    }

    public static bool AddCollectible(CollectibleData collectible)
    {
        // Authored collectibles use collectibleId as their run-unique ownership key.
        // Empty IDs are invalid instead of silently allowing unbounded duplicates.
        if (collectible == null || string.IsNullOrEmpty(collectible.collectibleId)) return false;
        if (ownedCollectibles.Exists(existing => existing != null && existing.collectibleId == collectible.collectibleId)) return false;
        ownedCollectibles.Add(collectible);

        if (collectible.effectType == CollectibleEffectType.MaxHealth && PlayerStats.Instance != null)
            PlayerStats.Instance.IncreaseMaxHealth(collectible.amount);

        if (collectible.effectType == CollectibleEffectType.MaxMana && PlayerStats.Instance != null)
            PlayerStats.Instance.IncreaseMaxMana(collectible.amount);

        return true;
    }

    public static void OnPlayerTurnStart()
    {
        discountedElementsThisTurn.Clear();
    }

    public static void ApplyBattleTurnEffects(PlayerStats player, int turn)
    {
        if (player == null || turn < 1) return;
        foreach (CollectibleData collectible in ownedCollectibles)
        {
            if (collectible == null) continue;
            if (turn == 2)
                player.RestoreMana(collectible.secondTurnManaBonus);
            if (collectible.effectType == CollectibleEffectType.EverySecondTurnShield && turn % 2 == 0)
                player.AddShield(collectible.amount);
            if (collectible.effectType == CollectibleEffectType.EarlyBattleRecovery && turn <= collectible.durationTurns)
            {
                player.Heal(collectible.amount);
                player.RestoreMana(collectible.turnManaBonus);
            }
        }
    }

    public static int ReduceIncomingDamage(int damage)
    {
        damage = Mathf.Max(0, damage);
        foreach (CollectibleData collectible in ownedCollectibles)
        {
            if (damage == 0) break;
            if (collectible == null || collectible.effectType != CollectibleEffectType.ChanceDamageReduction) continue;
            if (Random.value < collectible.procChance)
                damage = Mathf.Max(0, damage - collectible.amount);
        }
        return damage;
    }

    public static CollectibleData GrantBattleCollectible(bool isElite, EventCombatReward eventReward)
    {
        CollectibleData reward;
        if (eventReward == EventCombatReward.RiceKeepsakes)
        {
            string id = Random.Range(0, 2) == 0 ? "special_metal_basin" : "big_rice";
            if (IsOwned(id)) id = id == "big_rice" ? "special_metal_basin" : "big_rice";
            if (IsOwned(id)) return null;
            reward = CreateCollectible(id);
            if (!AddCollectible(reward)) return null;
            string otherId = id == "big_rice" ? "special_metal_basin" : "big_rice";
            PendingShopCollectibleId = IsOwned(otherId) ? null : otherId;
            return reward;
        }
        // Integer rolls make the normal drop chance exactly 30 out of 100.
        if (!isElite && Random.Range(0, 100) >= 30) return null;
        reward = CreateRandomCollectible();
        return AddCollectible(reward) ? reward : null;
    }

    public static CollectibleData ClaimNextShopCollectible()
    {
        string id = PendingShopCollectibleId;
        PendingShopCollectibleId = null;
        return string.IsNullOrEmpty(id) || IsOwned(id) ? null : CreateCollectible(id);
    }

    private static CollectibleData CreateCollectible(string id)
    {
        CollectibleData asset = Resources.Load<CollectibleData>("Collectibles/" + id);
        CollectibleData collectible = asset != null ? Object.Instantiate(asset) : CollectibleCatalog.CreateFallback(id);
        CollectibleCatalog.EnsureIcon(collectible);
        return RegisterRuntimeCollectible(collectible);
    }

    public static int GetEffectiveCardCost(CardData card)
    {
        if (card == null) return 0;

        int cost = card.cost;
        foreach (CollectibleData collectible in ownedCollectibles)
        {
            if (collectible == null) continue;
            if (collectible.effectType != CollectibleEffectType.FirstElementCardCostReduction) continue;
            if (collectible.element != card.element) continue;
            if (discountedElementsThisTurn.Contains(card.element)) continue;

            cost -= collectible.amount;
        }

        return Mathf.Max(0, cost);
    }

    public static void NotifyCardPlayed(CardData card)
    {
        if (card == null) return;

        foreach (CollectibleData collectible in ownedCollectibles)
        {
            if (collectible == null) continue;
            if (collectible.effectType != CollectibleEffectType.FirstElementCardCostReduction) continue;
            if (collectible.element != card.element) continue;

            discountedElementsThisTurn.Add(card.element);
        }
    }

    public static int GetDamageBonus(DamageType damageType)
    {
        int bonus = 0;
        foreach (CollectibleData collectible in ownedCollectibles)
        {
            if (collectible == null) continue;
            if (collectible.effectType == CollectibleEffectType.ElementDamageBonus && collectible.damageType == damageType)
                bonus += collectible.amount;
        }

        return bonus;
    }

    public static int GetStartShield()
    {
        int shield = 0;
        foreach (CollectibleData collectible in ownedCollectibles)
        {
            if (collectible == null) continue;
            if (collectible.effectType == CollectibleEffectType.StartShield)
                shield += collectible.amount;
        }

        return Mathf.Max(0, shield);
    }

    public static int GetShopDiscountPercent()
    {
        int discount = 0;
        foreach (CollectibleData collectible in ownedCollectibles)
        {
            if (collectible == null) continue;
            if (collectible.effectType == CollectibleEffectType.ShopDiscountPercent)
                discount += collectible.amount;
        }

        return Mathf.Clamp(discount, 0, 80);
    }

    public static string GetOwnedCollectibleSummary()
    {
        if (ownedCollectibles.Count == 0)
            return "藏品 0";

        List<string> names = new List<string>();
        foreach (CollectibleData collectible in ownedCollectibles)
        {
            if (collectible == null) continue;
            if (!string.IsNullOrEmpty(collectible.collectibleName))
                names.Add(collectible.collectibleName);
        }

        if (names.Count == 0)
            return $"藏品 {ownedCollectibles.Count}";

        return $"藏品 {ownedCollectibles.Count}: {string.Join("，", names)}";
    }

    public static CollectibleData CreateRandomCollectible()
    {
        List<CollectibleData> authored = Resources.LoadAll<CollectibleData>("Collectibles").ToList();
        List<CollectibleData> candidates = authored.Count > 0
            ? authored.FindAll(data => data != null && !data.eventExclusive && !IsOwned(data.collectibleId))
            : CollectibleCatalog.Ids.Where(id => !IsOwned(id)).Select(CollectibleCatalog.CreateFallback)
                .Select(RegisterRuntimeCollectible).Where(data => data != null && !data.eventExclusive).ToList();
        if (candidates.Count == 0) return null;

        CollectibleData selected = candidates[Random.Range(0, candidates.Count)];
        if (authored.Count > 0)
            selected = Object.Instantiate(selected);
        CollectibleCatalog.EnsureIcon(selected);
        return RegisterRuntimeCollectible(selected);
    }

    private static bool IsOwned(string id)
    {
        return !string.IsNullOrEmpty(id) && ownedCollectibles.Exists(data => data != null && data.collectibleId == id);
    }

    public static CollectibleData CreateMaxHealthCollectible()
    {
        return RegisterRuntimeCollectible(CollectibleCatalog.CreateFallback("life_specimen"));
    }

    public static CollectibleData CreateGreaterMaxHealthCollectible()
    {
        return RegisterRuntimeCollectible(CollectibleCatalog.CreateFallback("vital_core"));
    }

    public static CollectibleData CreateMaxManaCollectible()
    {
        return RegisterRuntimeCollectible(CollectibleCatalog.CreateFallback("energy_core"));
    }

    public static CollectibleData CreateStartShieldCollectible()
    {
        return RegisterRuntimeCollectible(CollectibleCatalog.CreateFallback("aegis_shard"));
    }

    public static CollectibleData CreateFireDamageCollectible()
    {
        return RegisterRuntimeCollectible(CollectibleCatalog.CreateFallback("fire_badge"));
    }

    public static CollectibleData CreateWaterDiscountCollectible()
    {
        return RegisterRuntimeCollectible(CollectibleCatalog.CreateFallback("cold_tide_pendant"));
    }

    public static CollectibleData CreateShopDiscountCollectible()
    {
        return RegisterRuntimeCollectible(CollectibleCatalog.CreateFallback("old_wallet"));
    }

    private static CollectibleData RegisterRuntimeCollectible(CollectibleData collectible)
    {
        if (collectible != null)
            runtimeCollectibles.Add(collectible);
        return collectible;
    }
}
