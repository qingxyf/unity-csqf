using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Shadow/Shadow Moon")]
public class ShadowMoonEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        context.Player.isUntargetable = true;
        context.Player.bleedTurns = 2;
    }
}
