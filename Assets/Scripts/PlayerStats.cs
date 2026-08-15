using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("基础属性")]
    public int baseMaxHealth = 100;
    public int maxHealth = 100;
    public int currentHealth;
    public int baseAttack = 10;
    public int initialMana = 6;
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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public static void EnsureInstance()
    {
        if (Instance != null) return;

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
        initialized = true;
        maxHealth = baseMaxHealth;
        currentHealth = maxHealth;
        currentMana = initialMana;
        gold = startingGold;
        currentShield = 0;
        damageSourcesThisTurn.Clear();
        hasFlameShield = false;
        activeEffects.Clear();
        Debug.Log($"主角属性初始化: HP {currentHealth}, ATK {baseAttack}, Mana {currentMana}");
    }

    public void OnBattleEnd()
    {
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
        currentMana = Mathf.Clamp(initialMana, 0, maxMana);
        AddShield(CollectibleManager.GetStartShield());
        isInitialCombatTurn = true;
    }

    public void StartTurn()
    {
        if (isInitialCombatTurn)
        {
            isInitialCombatTurn = false;
        }
        else
        {
            currentMana = Mathf.Min(maxMana, currentMana + manaRegenPerTurn);
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
    ShopDiscountPercent
}

[CreateAssetMenu(fileName = "NewCollectible", menuName = "Roguelike/Collectible")]
public class CollectibleData : ScriptableObject
{
    public string collectibleId;
    public string collectibleName;
    public Sprite icon;
    [TextArea(2, 4)]
    public string description;
    public CollectibleEffectType effectType;
    public int amount;
    public CardElement element = CardElement.Neutral;
    public DamageType damageType = DamageType.Physical;
    public int shopPrice = 100;
}

public static class CollectibleManager
{
    private static readonly List<CollectibleData> ownedCollectibles = new List<CollectibleData>();
    private static readonly HashSet<CardElement> discountedElementsThisTurn = new HashSet<CardElement>();

    public static IReadOnlyList<CollectibleData> OwnedCollectibles => ownedCollectibles;

    public static void Clear()
    {
        ownedCollectibles.Clear();
        discountedElementsThisTurn.Clear();
    }

    public static void AddCollectible(CollectibleData collectible)
    {
        if (collectible == null) return;
        ownedCollectibles.Add(collectible);

        if (collectible.effectType == CollectibleEffectType.MaxHealth && PlayerStats.Instance != null)
            PlayerStats.Instance.IncreaseMaxHealth(collectible.amount);

        if (collectible.effectType == CollectibleEffectType.MaxMana && PlayerStats.Instance != null)
            PlayerStats.Instance.IncreaseMaxMana(collectible.amount);
    }

    public static void OnPlayerTurnStart()
    {
        discountedElementsThisTurn.Clear();
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
        CollectibleData[] pool =
        {
            CreateMaxHealthCollectible(),
            CreateGreaterMaxHealthCollectible(),
            CreateMaxManaCollectible(),
            CreateStartShieldCollectible(),
            CreateFireDamageCollectible(),
            CreateWaterDiscountCollectible(),
            CreateShopDiscountCollectible()
        };

        return pool[Random.Range(0, pool.Length)];
    }

    public static CollectibleData CreateMaxHealthCollectible()
    {
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleId = "life_specimen";
        collectible.collectibleName = "生命标本";
        collectible.icon = LoadCollectibleIcon("life_specimen");
        collectible.description = "最大生命值 +15。";
        collectible.effectType = CollectibleEffectType.MaxHealth;
        collectible.amount = 15;
        collectible.shopPrice = 110;
        return collectible;
    }

    public static CollectibleData CreateGreaterMaxHealthCollectible()
    {
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleId = "vital_core";
        collectible.collectibleName = "活力核心";
        collectible.icon = LoadCollectibleIcon("vital_core");
        collectible.description = "最大生命值 +25。";
        collectible.effectType = CollectibleEffectType.MaxHealth;
        collectible.amount = 25;
        collectible.shopPrice = 160;
        return collectible;
    }

    public static CollectibleData CreateMaxManaCollectible()
    {
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleId = "energy_core";
        collectible.collectibleName = "能量核心";
        collectible.icon = LoadCollectibleIcon("energy_core");
        collectible.description = "能量上限 +1，并获得 1 点当前能量。";
        collectible.effectType = CollectibleEffectType.MaxMana;
        collectible.amount = 1;
        collectible.shopPrice = 150;
        return collectible;
    }

    public static CollectibleData CreateStartShieldCollectible()
    {
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleId = "aegis_shard";
        collectible.collectibleName = "圣盾碎片";
        collectible.icon = LoadCollectibleIcon("aegis_shard");
        collectible.description = "每场战斗开始时获得 8 点护盾。";
        collectible.effectType = CollectibleEffectType.StartShield;
        collectible.amount = 8;
        collectible.shopPrice = 90;
        return collectible;
    }

    public static CollectibleData CreateFireDamageCollectible()
    {
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleId = "fire_badge";
        collectible.collectibleName = "火焰徽章";
        collectible.icon = LoadCollectibleIcon("fire_badge");
        collectible.description = "火焰伤害 +10。";
        collectible.effectType = CollectibleEffectType.ElementDamageBonus;
        collectible.damageType = DamageType.Fire;
        collectible.amount = 10;
        collectible.shopPrice = 120;
        return collectible;
    }

    public static CollectibleData CreateWaterDiscountCollectible()
    {
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleId = "cold_tide_pendant";
        collectible.collectibleName = "寒潮坠饰";
        collectible.icon = LoadCollectibleIcon("cold_tide_pendant");
        collectible.description = "每回合第一张水系卡牌费用 -1。";
        collectible.effectType = CollectibleEffectType.FirstElementCardCostReduction;
        collectible.element = CardElement.Water;
        collectible.amount = 1;
        collectible.shopPrice = 130;
        return collectible;
    }

    public static CollectibleData CreateShopDiscountCollectible()
    {
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleId = "old_wallet";
        collectible.collectibleName = "旧钱包";
        collectible.icon = LoadCollectibleIcon("old_wallet");
        collectible.description = "商店价格 -15%。";
        collectible.effectType = CollectibleEffectType.ShopDiscountPercent;
        collectible.amount = 15;
        collectible.shopPrice = 100;
        return collectible;
    }

    private static Sprite LoadCollectibleIcon(string iconName)
    {
        return Resources.Load<Sprite>($"Icons/Collectibles/{iconName}");
    }
}
