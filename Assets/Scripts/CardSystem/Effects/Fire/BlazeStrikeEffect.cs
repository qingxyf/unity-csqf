using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Fire/Blaze Strike")]
public class BlazeStrikeEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            int baseDmg = context.Player.GetAttackDamage() + 20;
            int extraDmg = 0;

            if (context.Player.currentHealth > 30)
            {
                int lostHealth = context.Player.currentHealth - 30;
                context.Player.SetHealth(30);
                extraDmg = (lostHealth / 10) * 10;
            }

            context.Target.TakeDamage(baseDmg + extraDmg, DamageType.Fire);
        }
    }
}
