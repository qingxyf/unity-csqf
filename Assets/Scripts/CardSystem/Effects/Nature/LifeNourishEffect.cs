using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Nature/Life Nourish")]
public class LifeNourishEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        context.Player.IncreaseMaxHealth(10);
        context.Player.Heal(35);
    }
}
