using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Fire/Double Chant")]
public class DoubleChantEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            int fireDmg = 30;
            if (context.EffectManager.doubleChantUseCount > 0)
            {
                fireDmg += context.Player.GetAttackDamage();
            }
            context.Target.TakeDamage(fireDmg, DamageType.Fire);
        }
        context.EffectManager.doubleChantUseCount++;
    }
}
