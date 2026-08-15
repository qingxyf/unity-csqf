using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Water/Frost Tidal")]
public class FrostTidalEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        foreach (var enemy in context.AllEnemies)
        {
            if (enemy == null) continue;
            bool hasFrost = enemy.HasStatus(StatusType.Frost);
            enemy.ApplyStatus(StatusType.Frost, 2);
            if (hasFrost)
            {
                enemy.ApplyStatus(StatusType.Freeze, 1);
            }
            enemy.TakeDamage(30 + context.Player.GetAttackDamage(), DamageType.Ice);
        }
    }
}
