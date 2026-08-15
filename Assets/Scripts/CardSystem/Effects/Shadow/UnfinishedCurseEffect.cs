using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Shadow/Unfinished Curse")]
public class UnfinishedCurseEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        context.EffectManager.nextCardDoubleEffect = true;
        context.Player.burnTurns = 2;
        context.Player.corrosionTurns = 2;
        context.Player.frostTurns = 2;
    }
}
