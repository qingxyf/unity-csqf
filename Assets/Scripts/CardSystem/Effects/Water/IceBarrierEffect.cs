using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Water/Ice Barrier")]
public class IceBarrierEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        context.Player.AddShield(20);
        context.Player.damageReductionNextHit = 0.2f;
    }
}
