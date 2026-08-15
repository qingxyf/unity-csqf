using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Nature/Tree of Life")]
public class TreeOfLifeEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        context.Player.regenerationTurns += 2;
        if (context.Target != null && context.Target.HasStatus(StatusType.Parasite))
        {
            context.Target.ExtendStatus(StatusType.Parasite, 2);
        }
    }
}
