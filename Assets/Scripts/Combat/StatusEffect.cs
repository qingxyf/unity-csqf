using UnityEngine;

public abstract class StatusEffect
{
    public StatusType Type;
    public int Duration;
    public int Value;

    public StatusEffect(StatusType type, int duration, int value = 0)
    {
        Type = type;
        Duration = duration;
        Value = value;
    }

    public virtual void OnTurnStart(IDamageable target) { }
    public virtual void OnTurnEnd(IDamageable target) { }
    public virtual void OnDamageTaken(IDamageable target, ref int damage) { }

    public bool IsExpired => Duration <= 0;

    public void TickDuration()
    {
        Duration--;
    }

    public void ExtendDuration(int turns)
    {
        Duration += turns;
    }
}
