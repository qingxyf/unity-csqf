using UnityEngine;

public class WeakEffect : StatusEffect
{
    public WeakEffect(int duration) : base(StatusType.Weak, duration) { }
}

public class ConfusedEffect : StatusEffect
{
    public ConfusedEffect(int duration) : base(StatusType.Confused, duration) { }
}

public class SilencedEffect : StatusEffect
{
    public SilencedEffect(int duration) : base(StatusType.Silenced, duration) { }
}

public class VulnerableEffect : StatusEffect
{
    public VulnerableEffect(int duration, int value) : base(StatusType.Vulnerable, duration, value) { }
}

public class PurifiedEffect : StatusEffect
{
    public PurifiedEffect(int duration) : base(StatusType.Purified, duration) { }
}
