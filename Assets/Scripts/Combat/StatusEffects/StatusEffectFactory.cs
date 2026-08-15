public static class StatusEffectFactory
{
    public static StatusEffect Create(StatusType type, int duration, int value = 0)
    {
        switch (type)
        {
            case StatusType.Burn: return new BurnEffect(duration);
            case StatusType.Corrosion: return new CorrosionEffect(duration);
            case StatusType.Frost: return new FrostEffect(duration);
            case StatusType.Freeze: return new FreezeEffect(duration);
            case StatusType.Poison: return new PoisonEffect(duration);
            case StatusType.Bleed: return new BleedEffect(duration);
            case StatusType.Stun: return new StunEffect(duration);
            case StatusType.Weak: return new WeakEffect(duration);
            case StatusType.Confused: return new ConfusedEffect(duration);
            case StatusType.Silenced: return new SilencedEffect(duration);
            case StatusType.Vulnerable: return new VulnerableEffect(duration, value);
            case StatusType.Purified: return new PurifiedEffect(duration);
            case StatusType.Parasite: return new ParasiteEffect(duration);
            case StatusType.Regeneration: return new RegenerationEffect(duration);
            default: return null;
        }
    }
}
