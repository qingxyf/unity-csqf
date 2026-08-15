using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Neutral/Set Up Camp")]
public class SetUpCampEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Deck != null)
        {
            context.Deck.DrawCardInCombat(1);
        }
        context.Player.Heal(20);
        context.EndPlayerTurnAfterPlay = true;
    }
}
