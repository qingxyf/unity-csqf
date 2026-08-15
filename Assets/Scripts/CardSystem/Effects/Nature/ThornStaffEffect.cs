using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Nature/Thorn Staff")]
public class ThornStaffEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            context.Target.TakeDamage(50, DamageType.Nature);
        }

        context.Player.regenerationTurns = Mathf.Max(context.Player.regenerationTurns, 1);
    }
}
