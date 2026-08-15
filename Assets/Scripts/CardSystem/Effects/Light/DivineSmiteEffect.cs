using UnityEngine;
[CreateAssetMenu(menuName = "Card Effects/Light/Divine Smite")]
public class DivineSmiteEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            context.Target.TakeDamage(30, DamageType.Light);
            if (context.Target.hasDealtDamage)
            {
                context.Target.ApplyStatus(StatusType.Stun, 1);
            }
        }
    }
}
