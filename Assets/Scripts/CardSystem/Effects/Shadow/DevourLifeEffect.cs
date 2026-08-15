using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Shadow/Devour Life")]
public class DevourLifeEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            int dmg = context.Player.GetAttackDamage() + Random.Range(2, 5) * 10;
            context.Target.TakeDamage(dmg, DamageType.Shadow);
            context.Player.Heal(dmg);
        }
    }
}
