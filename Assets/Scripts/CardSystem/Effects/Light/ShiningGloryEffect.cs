using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(menuName = "Card Effects/Light/Shining Glory")]
public class ShiningGloryEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        int dmg = 60 - context.EffectManager.shiningGloryDamageReduction;
        if (dmg < 0) dmg = 0;
        bool killedAny = false;

        foreach (var enemy in new List<Enemy>(context.AllEnemies))
        {
            if (enemy == null) continue;
            int hpBefore = enemy.GetCurrentHealth();
            enemy.TakeDamage(dmg, DamageType.Light);
            if (enemy.IsDead() || (hpBefore > 0 && enemy.GetCurrentHealth() <= 0))
            {
                killedAny = true;
            }
        }

        if (!killedAny)
        {
            context.EffectManager.shiningGloryDamageReduction += 10;
        }
    }
}
