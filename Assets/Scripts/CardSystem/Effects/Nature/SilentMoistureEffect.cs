using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Nature/Silent Moisture")]
public class SilentMoistureEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Player.currentHealth < 100)
            context.Player.SetHealth(100);
        context.Player.Cleanse();
    }
}
