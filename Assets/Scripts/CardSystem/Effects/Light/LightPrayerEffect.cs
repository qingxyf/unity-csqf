using UnityEngine;
[CreateAssetMenu(menuName = "Card Effects/Light/Light Prayer")]
public class LightPrayerEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        CardEffectHelper.ApplyStatusToAll(context.AllEnemies, StatusType.Purified, 1);
        context.Player.delayedHealNextTurn += 25;
    }
}
