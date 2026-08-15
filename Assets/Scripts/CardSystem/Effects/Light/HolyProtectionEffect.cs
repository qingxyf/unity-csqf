using UnityEngine;
[CreateAssetMenu(menuName = "Card Effects/Light/Holy Protection")]
public class HolyProtectionEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        context.Player.AddShield(20);
    }
}
