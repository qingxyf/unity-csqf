using UnityEngine;
[CreateAssetMenu(menuName = "Card Effects/Light/Devout Chant")]
public class DevoutChantEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            context.Target.TakeDamage(40, DamageType.Light);
            context.Target.ExtendAllStatus(1);
        }
    }
}
