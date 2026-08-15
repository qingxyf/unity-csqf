using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Fire/Life Spark")]
public class LifeSparkEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            context.Target.ApplyStatus(StatusType.Regeneration, 1);
            context.Target.spreadDamageToNeighbors = true;
        }
    }
}
