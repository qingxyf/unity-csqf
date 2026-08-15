using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Water/Corrosive Tide")]
public class CorrosiveTideEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            context.Target.ApplyStatus(StatusType.Corrosion, 2);
            foreach (var e in CardEffectHelper.GetAdjacentEnemies(context.AllEnemies, context.Target))
            {
                e.ApplyStatus(StatusType.Corrosion, 2);
            }
        }
    }
}
