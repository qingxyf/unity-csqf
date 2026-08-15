using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Shadow/Despair Abyss")]
public class DespairAbyssEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            if (context.Target.currentHealth > context.Player.currentHealth)
            {
                int lostHP = context.Target.maxHealth - context.Target.currentHealth;
                int dmg = Mathf.FloorToInt(lostHP * 0.4f);
                context.Target.TakeDamage(dmg, DamageType.Shadow);
            }
            else
            {
                context.Target.TakeDamage(80, DamageType.Shadow);
            }
        }
    }
}
