using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Nature/Thorn Entangle")]
public class ThornEntangleEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            context.Target.TakeDamage(20, DamageType.Nature);
            context.Target.ApplyStatus(StatusType.Poison, 3);
        }
    }
}
