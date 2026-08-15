using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Fire/Ignite")]
public class IgniteEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            context.Target.TakeDamage(20, DamageType.Fire);

            foreach (var e in CardEffectHelper.GetAdjacentEnemies(context.AllEnemies, context.Target))
            {
                e.TakeDamage(10, DamageType.Fire);
            }

            context.Target.ApplyStatus(StatusType.Vulnerable, 1, 10);
        }
    }
}
