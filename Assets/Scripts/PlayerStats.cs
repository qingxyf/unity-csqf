using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [Header("基础属性")]
    public int baseMaxHealth = 100; // Permanent Max Health
    public int maxHealth = 100;     // Current Battle Max Health
    public int currentHealth;
    public int baseAttack = 10;
    public int maxMana = 100;
    public int currentMana;
    public int currentShield = 0;
    
    // New fields for card effects
    public List<GameObject> damageSourcesThisTurn = new List<GameObject>();
    public bool hasFlameShield = false; 
    public float damageReductionNextHit = 0f; // 0 to 1
    public int flatDamageReductionNextHit = 0; // Flat reduction
    public bool isUntargetable = false;
    public int regenerationTurns = 0; // Simple turn counter for Regeneration logic
    
    // Self Debuffs (Simulated)
    public int burnTurns = 0;
    public int corrosionTurns = 0;
    public int frostTurns = 0;
    public int bleedTurns = 0;

    // New fields for Neutral card effects
    public int extraDrawsNextTurn = 0;
    public int extraManaNextTurn = 0;

    // Delayed healing (光明祈愿)
    public int delayedHealNextTurn = 0;

    // Nature Guard passive (自然守护)
    public bool natureGuardActive = false;

    [Header("单例模式")]
    public static PlayerStats Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializeStats();
    }

    public void InitializeStats()
    {
        maxHealth = baseMaxHealth;
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentShield = 0;
        damageSourcesThisTurn.Clear();
        hasFlameShield = false;
        Debug.Log($"主角属性初始化: HP {currentHealth}, ATK {baseAttack}, Mana {currentMana}");
    }

    public void OnBattleEnd()
    {
        // Reset Max Health to Base Max Health (removing "Life Nourish" buffs)
        if (maxHealth != baseMaxHealth)
        {
            maxHealth = baseMaxHealth;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            Debug.Log("Battle End: Max Health reset to base value.");
        }
        
        currentShield = 0;
        damageSourcesThisTurn.Clear();
        hasFlameShield = false;
        regenerationTurns = 0;
        isUntargetable = false;
        damageReductionNextHit = 0f;
        flatDamageReductionNextHit = 0;
        extraDrawsNextTurn = 0;
        extraManaNextTurn = 0;
        burnTurns = 0;
        corrosionTurns = 0;
        frostTurns = 0;
        bleedTurns = 0;
        delayedHealNextTurn = 0;
        natureGuardActive = false;
    }

    public void StartTurn()
    {
        damageSourcesThisTurn.Clear();
        hasFlameShield = false; // Reset Flame Shield
        isUntargetable = false;
        natureGuardActive = false; // Reset Nature Guard

        // --- Handle Self Debuffs ---
        if (burnTurns > 0)
        {
            TakeDamage(10, null); // Fire damage
            Debug.Log("Burn Damage: 10");
            burnTurns--;
        }
        if (corrosionTurns > 0)
        {
            TakeDamage(10, null); // Shadow damage
            Debug.Log("Corrosion Damage: 10");
            corrosionTurns--;
        }
        if (frostTurns > 0)
        {
            TakeDamage(5, null); // Ice damage
            Debug.Log("Frost Damage: 5");
            frostTurns--;
        }
        if (bleedTurns > 0)
        {
            TakeDamage(10, null); // Shadow damage
            Debug.Log("Bleed Damage: 10");
            bleedTurns--;
        }

        // Delayed Heal (光明祈愿)
        if (delayedHealNextTurn > 0)
        {
            Heal(delayedHealNextTurn);
            Debug.Log($"Delayed Heal: {delayedHealNextTurn} HP");
            delayedHealNextTurn = 0;
        }
        
        // Apply Next Turn Effects
        if (extraDrawsNextTurn > 0)
        {
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.DrawCardInCombat(extraDrawsNextTurn);
                Debug.Log($"Extra Draw applied: {extraDrawsNextTurn} cards.");
            }
            extraDrawsNextTurn = 0;
        }

        if (extraManaNextTurn > 0)
        {
            RestoreMana(extraManaNextTurn);
            Debug.Log($"Extra Mana applied: {extraManaNextTurn}.");
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
        currentHealth += amount; 
        Debug.Log($"Max Health increased by {amount}. New Max: {maxHealth}");
    }

    public void SetHealth(int value)
    {
        currentHealth = value;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        Debug.Log($"Health set to {currentHealth}");
    }

    public void HealFull()
    {
        int healAmount = maxHealth - currentHealth;
        currentHealth = maxHealth;
        currentMana = maxMana; // 假设也回满蓝
        Debug.Log($"<color=green>主角状态全满！恢复了 {healAmount} 点生命。</color>");
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        Debug.Log($"<color=green>主角恢复了 {amount} 点生命。当前HP: {currentHealth}</color>");
    }

    public void AddShield(int amount)
    {
        currentShield += amount;
        Debug.Log($"<color=blue>主角获得了 {amount} 点护盾。当前护盾: {currentShield}</color>");
    }

    public void RestoreMana(int amount)
    {
        currentMana += amount;
        if (currentMana > maxMana) currentMana = maxMana;
        Debug.Log($"<color=blue>主角回复了 {amount} 点法力。当前法力: {currentMana}</color>");
    }
    
    public void TakeDamage(int damage, GameObject source = null)
    {
        if (isUntargetable) 
        {
            Debug.Log("Player is Untargetable! Damage avoided.");
            return;
        }

        // Corrosion Effect: +5 Damage taken
        if (corrosionTurns > 0)
        {
            damage += 5;
            Debug.Log("Corrosion: +5 Damage Taken");
        }

        if (source != null && !damageSourcesThisTurn.Contains(source))
        {
            damageSourcesThisTurn.Add(source);
        }

        // Flame Shield Logic
        if (hasFlameShield && source != null)
        {
             var enemy = source.GetComponent<Enemy>();
             if (enemy != null)
             {
                 // Apply Burn: "令伤害来源获得[烧伤]" -> Default 2 turns
                 enemy.ApplyStatus(StatusType.Burn, 2);
                 Debug.Log("Flame Shield triggered: Burn applied to attacker!");
             }
        }

        // Nature Guard Logic (自然守护: 本回合内敌人对你造成伤害时，其受到10点生机伤害)
        if (natureGuardActive && source != null)
        {
            var enemy = source.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(10, DamageType.Nature);
                Debug.Log("Nature Guard triggered: 10 Nature damage to attacker!");
            }
        }

        if (damageReductionNextHit > 0)
        {
            damage = Mathf.FloorToInt(damage * (1f - damageReductionNextHit));
            damageReductionNextHit = 0f; // Consume
            Debug.Log("Damage Reduced by Barrier!");
        }

        if (flatDamageReductionNextHit > 0)
        {
            damage -= flatDamageReductionNextHit;
            if (damage < 0) damage = 0;
            flatDamageReductionNextHit = 0; // Consume
            Debug.Log("Damage Reduced by Flat Reduction!");
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
        
        currentHealth -= damage;
        Debug.Log($"<color=red>主角受到伤害！剩余HP: {currentHealth}</color>");
    }

    public void Cleanse()
    {
        // Remove negative effects
        burnTurns = 0;
        corrosionTurns = 0;
        frostTurns = 0;
        bleedTurns = 0;

        Debug.Log("Player Cleansed!");
    }

    public int GetAttackDamage()
    {
        int dmg = baseAttack;
        if (burnTurns > 0)
        {
            dmg /= 2;
        }
        return dmg;
    }

    public void DrawCards(int count)
    {
        // 这里预留给手牌系统
        Debug.Log($"<color=cyan>获得 {count} 张手牌！(系统暂未实装)</color>");
    }
}
