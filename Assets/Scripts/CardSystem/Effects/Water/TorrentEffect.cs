using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Water/Torrent")]
public class TorrentEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            int hpBefore = context.Target.GetCurrentHealth();
            context.Target.TakeDamage(50, DamageType.Ice);
            if (context.Target.IsDead() || (hpBefore > 0 && context.Target.GetCurrentHealth() <= 0))
            {
                context.Player.RestoreMana(3);
                context.Player.Heal(20);
            }
        }
    }
}
