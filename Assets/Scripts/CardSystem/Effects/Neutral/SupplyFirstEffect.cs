using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Neutral/Supply First")]
public class SupplyFirstEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        context.Player.extraDrawsNextTurn += 1;
    }
}
