using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Water/Frozen Domain")]
public class FrozenDomainEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        CardEffectHelper.ApplyStatusToAll(context.AllEnemies, StatusType.Freeze, 1);
    }
}
