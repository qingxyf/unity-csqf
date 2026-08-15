using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Neutral/Careful Calculation")]
public class CarefulCalculationEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Deck != null)
        {
            int count = context.Deck.hand.Count;
            context.Deck.DiscardHand();
            context.Deck.DrawCardInCombat(count + 1);
        }
    }
}
