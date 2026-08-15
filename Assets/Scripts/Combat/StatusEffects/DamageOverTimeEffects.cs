using UnityEngine;

public class PoisonEffect : StatusEffect
{
    public PoisonEffect(int duration) : base(StatusType.Poison, duration) { }

    public override void OnTurnStart(IDamageable target)
    {
        int poisonDmg = Mathf.Min(30, Mathf.FloorToInt(target.GetCurrentHealth() * 0.1f));
        target.TakeDamage(poisonDmg, DamageType.Nature);
    }
}

public class BleedEffect : StatusEffect
{
    public BleedEffect(int duration) : base(StatusType.Bleed, duration) { }

    public override void OnTurnStart(IDamageable target)
    {
        target.TakeDamage(10, DamageType.Shadow);
    }
}

public class StunEffect : StatusEffect
{
    public StunEffect(int duration) : base(StatusType.Stun, duration) { }
}
