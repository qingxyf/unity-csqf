using UnityEngine;
using System.Collections.Generic;
using TMPro; 

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Attributes")]
    public string enemyName = "Enemy";
    public int maxHealth = 100;
    public int currentHealth;
    public int currentShield = 0;
    public int baseAttack = 10;
    public bool hasDealtDamage = false; // Tracks if enemy has dealt damage in this battle

    [Header("UI")]
    public TextMeshProUGUI hpText;
    public GameObject statusIconContainer; 

    // Status Effects Storage: Type -> (Duration, Value)
    private Dictionary<StatusType, StatusEffectData> activeStatuses = new Dictionary<StatusType, StatusEffectData>();

    // Combat State for Elemental Reactions and Card Effects
    public DamageType lastDamageType = DamageType.Physical;
    public bool spreadDamageToNeighbors = false;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(int damage, DamageType type = DamageType.Physical)
    {
        TakeDamageInternal(damage, type, false);
    }

    private void TakeDamageInternal(int damage, DamageType type, bool isSplash)
    {
        // --- Elemental Reactions ---
        if (PlayerStats.Instance != null)
        {
            // [燃烧伤害]: Fire Damage + (Parasite OR Last was Nature) -> +Base Attack
            if (type == DamageType.Fire)
            {
                if (activeStatuses.ContainsKey(StatusType.Parasite) || lastDamageType == DamageType.Nature)
                {
                    int bonus = PlayerStats.Instance.GetAttackDamage();
                    damage += bonus;
                    ShowFloatingText($"Combustion! +{bonus}", Color.red);
                }
            }
            
            // [蒸发效果]: Ice Damage + Last was Fire -> Remove Random Buff
            if (type == DamageType.Ice && lastDamageType == DamageType.Fire)
            {
                RemoveRandomBuff();
                ShowFloatingText("Vaporize!", Color.cyan);
            }

            // [丰饶生长]: Nature Damage + Last was Ice -> Extend Random Debuff
            if (type == DamageType.Nature && lastDamageType == DamageType.Ice)
            {
                ExtendRandomDebuff(1);
                ShowFloatingText("Bloom!", Color.green);
            }

            // [光暗双生]: Light/Shadow + Shadow/Light -> Player Damage Reduction
            if ((type == DamageType.Light && lastDamageType == DamageType.Shadow) ||
                (type == DamageType.Shadow && lastDamageType == DamageType.Light))
            {
                PlayerStats.Instance.flatDamageReductionNextHit = 10;
                ShowFloatingText("Twilight!", Color.grey);
            }
        }

        // Update Last Damage Type
        if (damage > 0)
        {
            lastDamageType = type;
        }

        // Spread Damage Logic (Life Spark)
        if (spreadDamageToNeighbors && !isSplash)
        {
            Enemy[] allEnemies = FindObjectsOfType<Enemy>();
            // Simple neighbor logic: All other enemies
            foreach (var e in allEnemies)
            {
                if (e != this)
                {
                    e.TakeDamageInternal(damage, type, true);
                }
            }
            ShowFloatingText("Splash!", Color.yellow);
        }

        // Vulnerable (Specific to Fire for now based on "Ignite" card)
        if (type == DamageType.Fire && activeStatuses.ContainsKey(StatusType.Vulnerable))
        {
             damage += activeStatuses[StatusType.Vulnerable].value;
             activeStatuses.Remove(StatusType.Vulnerable); // Consume "Next time"
             ShowFloatingText("Vulnerable Hit!", Color.red);
        }

        // Corrosion: Extra 5 damage when taking damage
        if (activeStatuses.ContainsKey(StatusType.Corrosion))
        {
            damage += 5;
            ShowFloatingText("+5 (Corrosion)", Color.magenta);
        }

        // Shield absorption
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
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        ShowFloatingText(damage.ToString(), Color.red);
        UpdateUI();
    }

    public void Heal(int amount)
    {
        if (activeStatuses.ContainsKey(StatusType.Bleed))
        {
            ShowFloatingText("Bleeding!", Color.gray);
            return;
        }

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        
        ShowFloatingText("+" + amount, Color.green);
        UpdateUI();
    }

    public void AddShield(int amount)
    {
        currentShield += amount;
        UpdateUI();
    }

    public void ApplyStatus(StatusType type, int duration, int value = 0)
    {
        // Purified: 无法触发额外效果 (cannot have additional effects applied)
        // Only allow Purified itself to be applied; block all other new statuses
        if (activeStatuses.ContainsKey(StatusType.Purified) && type != StatusType.Purified)
        {
            ShowFloatingText("Purified! Blocked!", Color.white);
            Debug.Log($"{enemyName}: {type} blocked by Purified.");
            return;
        }
        
        if (activeStatuses.ContainsKey(type))
        {
            activeStatuses[type] = new StatusEffectData { duration = duration, value = value };
        }
        else
        {
            activeStatuses.Add(type, new StatusEffectData { duration = duration, value = value });
        }
        ShowFloatingText(type.ToString(), Color.yellow);
        Debug.Log($"{enemyName} applied {type} for {duration} turns.");
    }
    
    // Call this at start of turn
    public void ProcessTurnStart()
    {
        List<StatusType> toRemove = new List<StatusType>();
        
        foreach (var kvp in new Dictionary<StatusType, StatusEffectData>(activeStatuses))
        {
            StatusType type = kvp.Key;
            StatusEffectData data = kvp.Value;

            switch (type)
            {
                case StatusType.Burn: // Start of turn: 10 fire damage
                    TakeDamage(10, DamageType.Fire);
                    break;
                case StatusType.Corrosion: // Start of turn: 10 shadow damage
                    TakeDamage(10, DamageType.Shadow);
                    break;
                case StatusType.Frost: // Start of turn: 5 ice damage
                    TakeDamage(5, DamageType.Ice);
                    break;
                case StatusType.Poison: // Start of turn: Lose 10% current HP (max 30)
                    int poisonDmg = Mathf.Min(30, Mathf.FloorToInt(currentHealth * 0.1f));
                    TakeDamage(poisonDmg, DamageType.Nature);
                    break;
                case StatusType.Freeze: // Start of turn: 5 ice damage + cannot act
                    TakeDamage(5, DamageType.Ice);
                    break;
                case StatusType.Bleed: // Start of turn: 10 shadow damage
                    TakeDamage(10, DamageType.Shadow);
                    break;
            }

            // Decrement duration for start-of-turn effects? 
            // Usually duration ticks down at end of turn. 
            // But if effect happens at start, maybe tick here? 
            // Let's assume standard turn structure: Start -> Action -> End (Tick).
            // So we don't tick here, we tick at ProcessTurnEnd.
        }
        UpdateUI();
    }

    public void ProcessTurnEnd()
    {
        List<StatusType> toRemove = new List<StatusType>();

        foreach (var kvp in new Dictionary<StatusType, StatusEffectData>(activeStatuses))
        {
            StatusType type = kvp.Key;
            StatusEffectData data = kvp.Value;

            switch (type)
            {
                case StatusType.Regeneration: // End of turn: Heal 20% Max HP
                    Heal(Mathf.FloorToInt(maxHealth * 0.2f));
                    break;
                case StatusType.Parasite: // End of turn: Lose 10% Max HP, Healer heals 50%
                    int paraDmg = Mathf.FloorToInt(maxHealth * 0.1f);
                    TakeDamage(paraDmg, DamageType.Nature);
                    if (PlayerStats.Instance != null)
                    {
                        PlayerStats.Instance.Heal(paraDmg / 2);
                    }
                    break;
            }

            data.duration--;
            if (data.duration <= 0)
            {
                toRemove.Add(type);
            }
            else
            {
                activeStatuses[type] = data;
            }
        }

        foreach (var type in toRemove)
        {
            activeStatuses.Remove(type);
        }
        
        // Reset turn-based flags
        spreadDamageToNeighbors = false;
        
        UpdateUI();
    }

    // Called by TurnManager to execute enemy action
    public void AttackPlayer()
    {
        if (!CanAct()) return;

        int dmg = GetAttackDamage();

        // Confused: attack self
        if (activeStatuses.ContainsKey(StatusType.Confused))
        {
            TakeDamage(dmg, DamageType.Physical);
            ShowFloatingText("Confused Hit Self!", Color.yellow);
            activeStatuses.Remove(StatusType.Confused); // 下次攻击时触发，消耗掉
            return;
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.TakeDamage(dmg, gameObject);
            hasDealtDamage = true;
        }
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => currentHealth <= 0;
    public bool HasStatus(StatusType type) => activeStatuses.ContainsKey(type);

    public void ExtendStatus(StatusType type, int turns)
    {
        if (activeStatuses.ContainsKey(type))
        {
            var data = activeStatuses[type];
            data.duration += turns;
            activeStatuses[type] = data;
        }
    }

    public void ExtendAllStatus(int turns)
    {
        List<StatusType> keys = new List<StatusType>(activeStatuses.Keys);
        foreach (var key in keys)
        {
            ExtendStatus(key, turns);
        }
    }

    public bool CanAct()
    {
        if (activeStatuses.ContainsKey(StatusType.Freeze) || 
            activeStatuses.ContainsKey(StatusType.Stun) || 
            activeStatuses.ContainsKey(StatusType.Rooted) ||
            activeStatuses.ContainsKey(StatusType.Silenced)) 
        {
            return false;
        }
        return true;
    }

    public int GetAttackDamage()
    {
        int dmg = baseAttack;
        
        if (activeStatuses.ContainsKey(StatusType.Weak))
        {
            dmg -= 10;
        }

        if (activeStatuses.ContainsKey(StatusType.Burn))
        {
            dmg = Mathf.FloorToInt(dmg * 0.5f);
        }

        if (dmg < 0) dmg = 0;
        return dmg;
    }

    private void Die()
    {
        Debug.Log($"{enemyName} Died!");
        
        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnEnemyKilled();
        }

        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnEnemyDied();
        }

        Destroy(gameObject);
    }

    private void RemoveRandomBuff()
    {
        List<StatusType> buffs = new List<StatusType>();
        foreach (var kvp in activeStatuses)
        {
            // Identify Buffs: Regeneration, Shield(not status), etc.
            // Currently only Regeneration is a clear buff in our list.
            if (kvp.Key == StatusType.Regeneration)
            {
                buffs.Add(kvp.Key);
            }
        }

        if (buffs.Count > 0)
        {
            StatusType toRemove = buffs[Random.Range(0, buffs.Count)];
            activeStatuses.Remove(toRemove);
        }
    }

    private void ExtendRandomDebuff(int turns)
    {
        List<StatusType> debuffs = new List<StatusType>();
        foreach (var kvp in activeStatuses)
        {
            // Identify Debuffs: Everything except Regeneration
            if (kvp.Key != StatusType.Regeneration)
            {
                debuffs.Add(kvp.Key);
            }
        }

        if (debuffs.Count > 0)
        {
            StatusType toExtend = debuffs[Random.Range(0, debuffs.Count)];
            var data = activeStatuses[toExtend];
            data.duration += turns;
            activeStatuses[toExtend] = data;
        }
    }

    private void UpdateUI()
    {
        if (hpText != null)
        {
            hpText.text = $"{currentHealth}/{maxHealth} (Shield: {currentShield})";
        }
    }

    private void ShowFloatingText(string text, Color color)
    {
        Debug.Log($"[{enemyName}] Floating Text: {text}");
    }
}

public struct StatusEffectData
{
    public int duration;
    public int value;
}
