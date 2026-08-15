using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Nature/Nature Guard")]
public class NatureGuardEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        context.Player.Heal(40);
        context.Player.natureGuardActive = true;
    }
}
