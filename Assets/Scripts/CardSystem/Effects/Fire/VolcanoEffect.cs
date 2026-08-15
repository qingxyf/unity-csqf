using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Fire/Volcano")]
public class VolcanoEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        CardEffectHelper.DamageAllEnemies(context.AllEnemies, 40, DamageType.Fire);
        CardEffectHelper.ApplyStatusToAll(context.AllEnemies, StatusType.Burn, 2);
    }
}
