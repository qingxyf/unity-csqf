using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Shadow/Shadow Erosion")]
public class ShadowErosionEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            context.Target.TakeDamage(20, DamageType.Shadow);
            context.Target.ApplyStatus(StatusType.Corrosion, 2);
        }
    }
}
