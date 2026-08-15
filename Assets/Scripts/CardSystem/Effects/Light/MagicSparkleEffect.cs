using UnityEngine;
[CreateAssetMenu(menuName = "Card Effects/Light/Magic Sparkle")]
public class MagicSparkleEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
        {
            context.Target.ApplyStatus(StatusType.Confused, 2);
        }
    }
}
