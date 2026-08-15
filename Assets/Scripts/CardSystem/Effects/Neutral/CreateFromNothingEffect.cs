using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Neutral/Create From Nothing")]
public class CreateFromNothingEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Deck != null)
        {
            context.Deck.DrawCardInCombat(2);
        }
    }
}
