using UnityEngine;
using System.Collections.Generic;

public class CardEffectManager : MonoBehaviour
{
    public static CardEffectManager Instance;

    [Header("Runtime Data")]
    // Tracks usage count for "二重吟唱" (Double Chant)
    public int doubleChantUseCount = 0;
    
    // Tracks if "照耀的荣光" (Shining Glory) reduced damage applies
    public int shiningGloryDamageReduction = 0;

    // Tracks "火山" (Volcano) stacks
    public int volcanoStacks = 0;

    // "未完成之咒" (Unfinished Curse) flag
    public bool nextCardDoubleEffect = false;
    
    // "安营扎寨" flag
    public bool campReturnToDeckActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Call this at the start of player's turn
    public void OnPlayerTurnStart()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.StartTurn();
        }
        
        // Reset Camp Flag
        campReturnToDeckActive = false;

        // Volcano Logic: 3 stacks to trigger
        if (volcanoStacks > 0)
        {
            volcanoStacks++;
            if (volcanoStacks >= 3)
            {
                // Trigger 60 AOE Fire Damage
                DamageAllEnemies(60, DamageType.Fire);
                volcanoStacks = 0; // Reset
                Debug.Log("Volcano Erupted!");
            }
        }
        
        var enemies = FindObjectsOfType<Enemy>();
        foreach (var enemy in enemies)
        {
            enemy.ProcessTurnStart();
        }
    }

    // Call this at the end of player's turn
    public void OnPlayerTurnEnd()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.EndTurn();
        }

        // Enemy turn end processing
        var enemies = FindObjectsOfType<Enemy>();
        foreach (var enemy in enemies)
        {
            enemy.ProcessTurnEnd();
        }
    }

    public void OnEnemyKilled()
    {
        if (campReturnToDeckActive)
        {
            if (DeckManager.Instance != null)
            {
                // Create "安营扎寨" card data and add to deck top
                // Assuming we can find the card or create it.
                // For now, let's just Log it as we might not have a reference to the card asset easily.
                // Or use DeckManager to add by name if supported.
                Debug.Log("安营扎寨: Enemy died, adding card to deck top (Not Implemented fully yet)");
                // DeckManager.Instance.AddCardToTop("安营扎寨"); 
            }
            campReturnToDeckActive = false; // Limit 1
        }
    }

    public void PlayCard(CardData card, GameObject target)
    {
        if (card == null) return;
        if (PlayerStats.Instance == null) return;

        PlayerStats player = PlayerStats.Instance;
        Enemy targetEnemy = target != null ? target.GetComponent<Enemy>() : null;

        // General cost check could go here
        if (player.currentMana < card.cost)
        {
            Debug.Log("Not enough mana!");
            return;
        }
        player.currentMana -= card.cost;

        Debug.Log($"Playing Card: {card.cardName}");

        int repeats = 1;
        if (nextCardDoubleEffect)
        {
            repeats = 2;
            nextCardDoubleEffect = false;
            Debug.Log("Double Effect Triggered!");
        }

        for (int i = 0; i < repeats; i++)
        {
            switch (card.cardName)
            {
                // --- Light (光) ---
                case "圣光庇护":
                    player.AddShield(20);
                    break;

                case "神圣惩击":
                    if (targetEnemy != null)
                    {
                        targetEnemy.TakeDamage(30, DamageType.Light);
                        if (targetEnemy.hasDealtDamage)
                        {
                            targetEnemy.ApplyStatus(StatusType.Stun, 1);
                        }
                    }
                    break;

                case "光明祈愿":
                    foreach (var enemy in FindObjectsOfType<Enemy>())
                    {
                        enemy.ApplyStatus(StatusType.Purified, 1);
                    }
                    player.Heal(25); 
                    Debug.Log("Note: '光明祈愿' healing applied immediately for simplicity.");
                    break;

                case "魔法闪耀":
                    if (targetEnemy != null)
                    {
                        targetEnemy.ApplyStatus(StatusType.Confused, 2);
                    }
                    break;

                case "虔心吟诵":
                    if (targetEnemy != null)
                    {
                        targetEnemy.TakeDamage(40, DamageType.Light);
                        targetEnemy.ExtendAllStatus(1);
                    }
                    break;

                case "照耀的荣光":
                    {
                        int dmg = 60 - shiningGloryDamageReduction;
                        if (dmg < 0) dmg = 0;
                        bool killedAny = false;
                        
                        List<Enemy> allEnemies = new List<Enemy>(FindObjectsOfType<Enemy>());
                        foreach (var enemy in allEnemies)
                        {
                            int hpBefore = enemy.GetCurrentHealth();
                            enemy.TakeDamage(dmg, DamageType.Light);
                            if (enemy.IsDead() || (hpBefore > 0 && enemy.GetCurrentHealth() <= 0))
                            {
                                killedAny = true;
                            }
                        }

                        if (!killedAny)
                        {
                            shiningGloryDamageReduction += 10;
                        }
                    }
                    break;
                
                // --- Fire (火) ---
                case "二重吟唱":
                    if (targetEnemy != null)
                    {
                        int fireDmg = 30;
                        if (doubleChantUseCount > 0)
                        {
                            fireDmg += player.GetAttackDamage();
                        }
                        targetEnemy.TakeDamage(fireDmg, DamageType.Fire);
                    }
                    doubleChantUseCount++;
                    break;

                case "点燃":
                    if (targetEnemy != null)
                    {
                        targetEnemy.TakeDamage(20, DamageType.Fire);
                        
                        Enemy[] enemies = FindObjectsOfType<Enemy>();
                        foreach (var e in enemies)
                        {
                            if (e != targetEnemy)
                            {
                                e.TakeDamage(10, DamageType.Fire);
                            }
                        }

                        targetEnemy.ApplyStatus(StatusType.Vulnerable, 1, 10);
                    }
                    break;

                case "火焰护盾":
                    player.AddShield(10);
                    player.hasFlameShield = true;
                    break;
                
                case "生命火种":
                    if (targetEnemy != null)
                    {
                        targetEnemy.ApplyStatus(StatusType.Regeneration, 1);
                        targetEnemy.spreadDamageToNeighbors = true;
                    }
                    break;

                case "火山":
                    DamageAllEnemies(40, DamageType.Fire);
                    foreach(var enemy in FindObjectsOfType<Enemy>())
                    {
                        enemy.ApplyStatus(StatusType.Burn, 2);
                    }
                    break;

                case "烈焰打击":
                    if (targetEnemy != null)
                    {
                        int baseDmg = player.GetAttackDamage() + 20;
                        int extraDmg = 0;
                        
                        if (player.currentHealth > 30)
                        {
                            int lostHealth = player.currentHealth - 30;
                            player.SetHealth(30);
                            extraDmg = (lostHealth / 10) * 10;
                        }
                        
                        targetEnemy.TakeDamage(baseDmg + extraDmg, DamageType.Fire);
                    }
                    break;

                // --- Grass (草) ---
                case "生命滋养":
                    player.IncreaseMaxHealth(10);
                    player.Heal(35);
                    break;

                case "荆棘缠绕":
                    if (targetEnemy != null)
                    {
                        targetEnemy.TakeDamage(20, DamageType.Nature);
                        targetEnemy.ApplyStatus(StatusType.Poison, 3);
                    }
                    break;

                case "自然守护":
                    player.Heal(40);
                    foreach (var source in player.damageSourcesThisTurn)
                    {
                        if (source != null)
                        {
                            var enemy = source.GetComponent<Enemy>();
                            if (enemy != null)
                            {
                                enemy.TakeDamage(10, DamageType.Nature);
                            }
                        }
                    }
                    break;

                case "寄生种子":
                    if (targetEnemy != null)
                    {
                        targetEnemy.ApplyStatus(StatusType.Parasite, 3);
                    }
                    break;

                case "生命之树":
                    player.regenerationTurns += 2;
                    if (targetEnemy != null && targetEnemy.HasStatus(StatusType.Parasite))
                    {
                        targetEnemy.ExtendStatus(StatusType.Parasite, 2);
                    }
                    break;

                case "棘藤棒":
                    if (targetEnemy != null)
                    {
                        targetEnemy.TakeDamage(50, DamageType.Nature);
                        player.regenerationTurns += 1;
                    }
                    break;
                
                case "无声润物":
                    player.SetHealth(100);
                    player.Cleanse();
                    break;

                // --- Water (水) ---
                case "冰霜箭":
                    if (targetEnemy != null)
                    {
                        targetEnemy.TakeDamage(30, DamageType.Ice);
                        targetEnemy.ApplyStatus(StatusType.Frost, 2); 
                    }
                    break;

                case "寒冰护体":
                    player.AddShield(20);
                    player.damageReductionNextHit = 0.2f;
                    break;

                case "激流冲刷":
                    if (targetEnemy != null)
                    {
                        int hpBefore = targetEnemy.GetCurrentHealth();
                        targetEnemy.TakeDamage(55, DamageType.Ice);
                        if (targetEnemy.IsDead() || (hpBefore > 0 && targetEnemy.GetCurrentHealth() <= 0))
                        {
                            player.RestoreMana(40);
                            player.Heal(20);
                        }
                    }
                    break;
                
                case "蚀骨丰泽":
                    if (targetEnemy != null)
                    {
                        targetEnemy.ApplyStatus(StatusType.Corrosion, 2);
                        Enemy[] all = FindObjectsOfType<Enemy>();
                        foreach(var e in all)
                        {
                            if (e != targetEnemy)
                            {
                                e.ApplyStatus(StatusType.Corrosion, 2);
                            }
                        }
                    }
                    break;

                case "冰封领域":
                    foreach (var enemy in FindObjectsOfType<Enemy>())
                    {
                        enemy.ApplyStatus(StatusType.Freeze, 1);
                    }
                    player.DrawCards(1);
                    break;

                case "霜涛覆岭":
                    foreach (var enemy in FindObjectsOfType<Enemy>())
                    {
                        bool hasFrost = enemy.HasStatus(StatusType.Frost);
                        enemy.ApplyStatus(StatusType.Frost, 2);
                        if (hasFrost)
                        {
                            enemy.ApplyStatus(StatusType.Freeze, 1);
                        }
                        enemy.TakeDamage(30 + player.GetAttackDamage(), DamageType.Ice);
                    }
                    break;

                // --- Shadow (暗影) ---
                case "暗影侵蚀":
                    if (targetEnemy != null)
                    {
                        targetEnemy.TakeDamage(20, DamageType.Shadow);
                        targetEnemy.ApplyStatus(StatusType.Corrosion, 2);
                    }
                    break;

                case "暗夜突袭":
                    if (targetEnemy != null)
                    {
                        int rand = Random.Range(0, 3);
                        if (rand == 0) targetEnemy.ApplyStatus(StatusType.Weak, 2);
                        else if (rand == 1) targetEnemy.ApplyStatus(StatusType.Bleed, 2);
                        else targetEnemy.ApplyStatus(StatusType.Stun, 1);
                    }
                    break;

                case "影月庇护":
                    player.isUntargetable = true;
                    player.TakeDamage(10, player.gameObject); 
                    break;

                case "未完成之咒":
                    nextCardDoubleEffect = true;
                    // Apply Self Debuffs
                    player.burnTurns = 2;
                    player.corrosionTurns = 2;
                    player.frostTurns = 2;
                    Debug.Log("Unfinished Curse: Applied Burn, Corrosion, Frost to Player");
                    break;

                case "吞噬生命":
                    if (targetEnemy != null)
                    {
                        int dmg = player.GetAttackDamage() + Random.Range(2, 5) * 10;
                        targetEnemy.TakeDamage(dmg, DamageType.Shadow);
                        player.Heal(dmg);
                    }
                    break;

                case "绝望深渊":
                    if (targetEnemy != null)
                    {
                        if (targetEnemy.currentHealth > player.currentHealth)
                        {
                            int lostHP = targetEnemy.maxHealth - targetEnemy.currentHealth;
                            int dmg = Mathf.FloorToInt(lostHP * 0.4f);
                            targetEnemy.TakeDamage(dmg, DamageType.Shadow);
                        }
                        else
                        {
                            targetEnemy.TakeDamage(80, DamageType.Shadow);
                        }
                    }
                    break;

                // --- Neutral (无属性) ---
                case "无中生有":
                    if (DeckManager.Instance != null)
                    {
                        DeckManager.Instance.DrawCardInCombat(2);
                    }
                    break;

                case "粮草先行":
                    player.extraDrawsNextTurn += 1;
                    break;

                case "安营扎寨":
                    if (DeckManager.Instance != null)
                    {
                        DeckManager.Instance.DrawCardInCombat(1);
                    }
                    player.Heal(20);
                    campReturnToDeckActive = true;
                    OnPlayerTurnEnd();
                    break;

                case "现行等待":
                    int unused = player.currentMana;
                    player.extraManaNextTurn += (unused + 2);
                    OnPlayerTurnEnd();
                    break;

                case "精打细算":
                    if (DeckManager.Instance != null)
                    {
                        int count = DeckManager.Instance.hand.Count;
                        DeckManager.Instance.DiscardHand();
                        DeckManager.Instance.DrawCardInCombat(count + 1);
                    }
                    break;

                default:
                    Debug.LogWarning($"Card effect not implemented: {card.cardName}");
                    break;
            }
        }

        // Notify DeckManager that card was played
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnCardPlayed(card);
        }
    }

    private void DamageAllEnemies(int amount, DamageType type)
    {
        foreach (var enemy in FindObjectsOfType<Enemy>())
        {
            enemy.TakeDamage(amount, type);
        }
    }
}
