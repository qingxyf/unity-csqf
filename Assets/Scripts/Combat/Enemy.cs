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
    public bool hasDealtDamage = false;

    [Header("UI")]
    public TextMeshProUGUI hpText;
    public GameObject statusIconContainer;

    private List<StatusEffect> activeEffects = new List<StatusEffect>();
    private bool healthInitialized;
    private bool dying;
    private RoguelikeEnemyPresentation presentation;

    private void Awake()
    {
        presentation = GetComponent<RoguelikeEnemyPresentation>();
    }

    public DamageType lastDamageType = DamageType.Physical;
    public bool spreadDamageToNeighbors = false;

    private void OnEnable()
    {
        if (EnemyManager.Instance == null)
            EnemyManager.EnsureInstance();

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.Unregister(this);
    }

    private void Start()
    {
        EnsureHealthInitialized();
        UpdateUI();
    }

    private void EnsureHealthInitialized()
    {
        if (healthInitialized) return;
        if (currentHealth <= 0)
            currentHealth = maxHealth;
        healthInitialized = true;
    }

    public void TakeDamage(int damage, DamageType type = DamageType.Physical)
    {
        EnsureHealthInitialized();
        TakeDamageInternal(damage, type, false);
    }

    private void TakeDamageInternal(int damage, DamageType type, bool isSplash)
    {
        if (dying) return;
        if (PlayerStats.Instance != null && !HasStatus(StatusType.Purified))
        {
            if (type == DamageType.Fire)
            {
                if (HasStatus(StatusType.Parasite) || lastDamageType == DamageType.Nature)
                {
                    int bonus = PlayerStats.Instance.GetAttackDamage();
                    damage += bonus;
                    ShowFloatingText($"Combustion! +{bonus}", Color.red);
                }
            }

            if (type == DamageType.Ice && lastDamageType == DamageType.Fire)
            {
                RemoveRandomBuff();
                if (CardEffectManager.Instance != null)
                {
                    int coldSpringBonus = CardEffectManager.Instance.ConsumeColdSpringBonus();
                    if (coldSpringBonus > 0)
                    {
                        damage += coldSpringBonus;
                        ShowFloatingText($"Cold Spring! +{coldSpringBonus}", Color.cyan);
                    }
                }
                ShowFloatingText("Vaporize!", Color.cyan);
            }

            if (type == DamageType.Nature && lastDamageType == DamageType.Ice)
            {
                ExtendRandomDebuff(1);
                ShowFloatingText("Bloom!", Color.green);
            }

            if ((type == DamageType.Light && lastDamageType == DamageType.Shadow) ||
                (type == DamageType.Shadow && lastDamageType == DamageType.Light))
            {
                PlayerStats.Instance.flatDamageReductionNextHit = 10;
                ShowFloatingText("Twilight!", Color.grey);
            }
        }

        if (damage > 0)
        {
            damage += CollectibleManager.GetDamageBonus(type);
            lastDamageType = type;
        }

        if (spreadDamageToNeighbors && !isSplash && EnemyManager.Instance != null)
        {
            foreach (var e in CardEffectHelper.GetAdjacentEnemies(EnemyManager.Instance.ActiveEnemies, this))
            {
                e.TakeDamageInternal(damage, type, true);
            }
            ShowFloatingText("Splash!", Color.yellow);
        }

        // Vulnerable
        var vulnerable = GetEffect(StatusType.Vulnerable);
        if (type == DamageType.Fire && vulnerable != null)
        {
            damage += vulnerable.Value;
            RemoveEffect(StatusType.Vulnerable);
            ShowFloatingText("Vulnerable Hit!", Color.red);
        }

        // Corrosion on-damage bonus
        var corrosion = GetEffect(StatusType.Corrosion);
        if (corrosion != null)
        {
            corrosion.OnDamageTaken(this, ref damage);
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
        ShowFloatingText(damage.ToString(), Color.red);
        UpdateUI();
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            UpdateUI();
            Die();
            return;
        }
        if (damage > 0 && presentation != null) presentation.PlayHit();
    }

    public void Heal(int amount)
    {
        if (dying) return;
        if (HasStatus(StatusType.Bleed))
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
        if (dying) return;
        currentShield += amount;
        UpdateUI();
    }

    public void ApplyStatus(StatusType type, int duration, int value = 0)
    {
        if (dying) return;
        RemoveEffect(type);

        var effect = StatusEffectFactory.Create(type, duration, value);
        if (effect != null)
        {
            activeEffects.Add(effect);
        }

        ShowFloatingText(type.ToString(), Color.yellow);
        Debug.Log($"{enemyName} applied {type} for {duration} turns.");
    }

    public void ProcessTurnStart()
    {
        if (dying) return;
        foreach (var effect in new List<StatusEffect>(activeEffects))
        {
            if (dying) break;
            effect.OnTurnStart(this);
        }
        UpdateUI();
    }

    public void ProcessTurnEnd()
    {
        if (dying) return;
        foreach (var effect in new List<StatusEffect>(activeEffects))
        {
            if (dying) break;
            effect.OnTurnEnd(this);
            effect.TickDuration();
        }

        activeEffects.RemoveAll(e => e.IsExpired);
        spreadDamageToNeighbors = false;
        UpdateUI();
    }

    public void AttackPlayer()
    {
        if (!CanAct()) return;

        int dmg = GetAttackDamage();

        if (HasStatus(StatusType.Confused))
        {
            TakeDamage(dmg, DamageType.Physical);
            ShowFloatingText("Confused Hit Self!", Color.yellow);
            RemoveEffect(StatusType.Confused);
            return;
        }

        if (PlayerStats.Instance != null)
        {
            if (presentation != null) presentation.PlayAttack();
            PlayerStats.Instance.TakeDamage(dmg, gameObject);
            hasDealtDamage = true;
        }
    }

    public int GetCurrentHealth()
    {
        EnsureHealthInitialized();
        return currentHealth;
    }
    public int GetMaxHealth() => maxHealth;
    public bool IsDead()
    {
        if (dying) return true;
        EnsureHealthInitialized();
        return currentHealth <= 0;
    }

    public bool HasStatus(StatusType type)
    {
        return activeEffects.Exists(e => e.Type == type);
    }

    public bool HasAnyDebuff()
    {
        return CountDebuffs() > 0;
    }

    public int CountDebuffs()
    {
        int count = 0;
        foreach (StatusEffect effect in activeEffects)
        {
            if (effect == null) continue;
            if (effect.Type != StatusType.Regeneration)
                count++;
        }
        return count;
    }

    public StatusEffect GetEffect(StatusType type)
    {
        return activeEffects.Find(e => e.Type == type);
    }

    private void RemoveEffect(StatusType type)
    {
        activeEffects.RemoveAll(e => e.Type == type);
    }

    public void RemoveStatus(StatusType type)
    {
        RemoveEffect(type);
    }

    public void ExtendStatus(StatusType type, int turns)
    {
        var effect = GetEffect(type);
        if (effect != null)
        {
            effect.ExtendDuration(turns);
        }
    }

    public void ExtendAllStatus(int turns)
    {
        foreach (var effect in activeEffects)
        {
            effect.ExtendDuration(turns);
        }
    }

    public bool CanAct()
    {
        return !IsDead() && !HasStatus(StatusType.Freeze) &&
               !HasStatus(StatusType.Stun) &&
               !HasStatus(StatusType.Rooted) &&
               !HasStatus(StatusType.Silenced);
    }

    public int GetAttackDamage()
    {
        int dmg = baseAttack;

        if (HasStatus(StatusType.Weak))
        {
            dmg -= 10;
        }

        if (HasStatus(StatusType.Burn))
        {
            dmg = Mathf.FloorToInt(dmg * 0.5f);
        }

        if (dmg < 0) dmg = 0;
        return dmg;
    }

    private void Die()
    {
        if (dying) return;
        dying = true;
        if (EnemyManager.Instance != null) EnemyManager.Instance.Unregister(this);
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        Debug.Log($"{enemyName} Died!");

        if (CardEffectManager.Instance != null)
        {
            CardEffectManager.Instance.OnEnemyKilled();
        }

        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.OnEnemyDied();
        }

        if (Application.isPlaying)
        {
            if (presentation != null) presentation.PlayDeath();
            Destroy(gameObject, presentation != null ? presentation.DeathDuration : 0f);
        }
        else
            DestroyImmediate(gameObject);
    }

    private void RemoveRandomBuff()
    {
        List<StatusEffect> buffs = activeEffects.FindAll(e => e.Type == StatusType.Regeneration);

        if (buffs.Count > 0)
        {
            var toRemove = buffs[Random.Range(0, buffs.Count)];
            activeEffects.Remove(toRemove);
        }
    }

    private void ExtendRandomDebuff(int turns)
    {
        List<StatusEffect> debuffs = activeEffects.FindAll(e => e.Type != StatusType.Regeneration);

        if (debuffs.Count > 0)
        {
            var toExtend = debuffs[Random.Range(0, debuffs.Count)];
            toExtend.ExtendDuration(turns);
        }
    }

    private void UpdateUI()
    {
        if (hpText != null)
        {
            hpText.text = $"{enemyName}\n生命 {currentHealth}/{maxHealth}  护盾 {currentShield}\n" +
                (CanAct() ? $"意图：攻击 {GetAttackDamage()}" : "意图：无法行动");
        }
    }

    private void ShowFloatingText(string text, Color color)
    {
        Debug.Log($"[{enemyName}] Floating Text: {text}");
    }
}
