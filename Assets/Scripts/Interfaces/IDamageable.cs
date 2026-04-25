using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage, DamageType type = DamageType.Physical);
    void Heal(int amount);
    void AddShield(int amount);
    void ApplyStatus(StatusType type, int duration, int value = 0);
    int GetCurrentHealth();
    int GetMaxHealth();
    bool IsDead();
}

public enum DamageType
{
    Physical,
    Fire,
    Ice,
    Nature,
    Light,
    Shadow
}

public enum StatusType
{
    None,
    Burn,       // 烧伤
    Poison,     // 中毒
    Freeze,     // 冰冻
    Frost,      // 冰霜
    Stun,       // 眩晕
    Weak,       // 虚弱
    Vulnerable, // 易伤 (not explicitly in list but good to have)
    Bleed,      // 流血
    Parasite,   // 寄生
    Regeneration, // 复苏
    Confused,   // 混乱
    Silenced,   // 沉默
    Rooted,     // 禁锢
    Corrosion,  // 腐蚀
    Purified    // 净化 (New)
}
