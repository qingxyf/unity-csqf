using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Fire/Flame Shield")]
public class FlameShieldEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        context.Player.AddShield(10);
        context.Player.hasFlameShield = true;
    }
}
