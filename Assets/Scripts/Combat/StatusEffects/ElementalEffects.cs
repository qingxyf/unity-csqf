using UnityEngine;

public class BurnEffect : StatusEffect
{
    public BurnEffect(int duration) : base(StatusType.Burn, duration) { }

    public override void OnTurnStart(IDamageable target)
    {
        target.TakeDamage(10, DamageType.Fire);
    }
}

public class CorrosionEffect : StatusEffect
{
    public CorrosionEffect(int duration) : base(StatusType.Corrosion, duration) { }

    public override void OnTurnStart(IDamageable target)
    {
        target.TakeDamage(10, DamageType.Shadow);
    }

    public override void OnDamageTaken(IDamageable target, ref int damage)
    {
        damage += 5;
    }
}

public class FrostEffect : StatusEffect
{
    public FrostEffect(int duration) : base(StatusType.Frost, duration) { }

    public override void OnTurnStart(IDamageable target)
    {
        target.TakeDamage(5, DamageType.Ice);
    }
}

public class FreezeEffect : StatusEffect
{
    public FreezeEffect(int duration) : base(StatusType.Freeze, duration) { }

    public override void OnTurnStart(IDamageable target)
    {
        target.TakeDamage(5, DamageType.Ice);
    }
}
