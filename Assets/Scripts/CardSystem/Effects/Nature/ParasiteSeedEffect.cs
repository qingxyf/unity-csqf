using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Nature/Parasite Seed")]
public class ParasiteSeedEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            context.Target.ApplyStatus(StatusType.Parasite, 3);
        }
    }
}
