using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Water/Frost Arrow")]
public class FrostArrowEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            context.Target.TakeDamage(20, DamageType.Ice);
            context.Target.ApplyStatus(StatusType.Frost, 2);
        }
    }
}
