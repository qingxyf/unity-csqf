using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Neutral/Wait And See")]
public class WaitAndSeeEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        int unused = context.Player.currentMana;
        context.Player.extraManaNextTurn += (unused + 2);
        context.EndPlayerTurnAfterPlay = true;
    }
}
