using UnityEngine;

public class ParasiteEffect : StatusEffect
{
    public ParasiteEffect(int duration) : base(StatusType.Parasite, duration) { }

    public override void OnTurnEnd(IDamageable target)
    {
        int paraDmg = Mathf.FloorToInt(target.GetMaxHealth() * 0.1f);
        target.TakeDamage(paraDmg, DamageType.Nature);
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.Heal(paraDmg / 2);
        }
    }
}

public class RegenerationEffect : StatusEffect
{
    public RegenerationEffect(int duration) : base(StatusType.Regeneration, duration) { }

    public override void OnTurnEnd(IDamageable target)
    {
        target.Heal(Mathf.FloorToInt(target.GetMaxHealth() * 0.2f));
    }
}
