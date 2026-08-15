using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Shadow/Night Raid")]
public class NightRaidEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            int rand = Random.Range(0, 3);
            if (rand == 0) context.Target.ApplyStatus(StatusType.Weak, 2);
            else if (rand == 1) context.Target.ApplyStatus(StatusType.Bleed, 2);
            else context.Target.ApplyStatus(StatusType.Stun, 1);
        }
    }
}
