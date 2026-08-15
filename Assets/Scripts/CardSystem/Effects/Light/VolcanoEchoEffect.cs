using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Light/Volcano Echo")]
public class VolcanoEchoEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Player == null) return;

        if (context.Player.currentHealth < 70)
            context.Player.SetHealth(70);
        context.Player.Cleanse();
    }
}
