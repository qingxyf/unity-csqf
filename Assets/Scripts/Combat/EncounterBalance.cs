using System;
using UnityEngine;

public enum EnemyTurnPattern { AttackOnly, Skirmisher, Duelist, Leader }
public enum EnemyIntentKind { Attack, Guard, HeavyAttack }

/// <summary>Authoring data only. Each encounter evaluates its own immutable snapshot.</summary>
[CreateAssetMenu(menuName = "Roguelike/Encounter Balance")]
public class EncounterBalance : ScriptableObject
{
    public EncounterTier normal = EncounterTier.Normal();
    public EncounterTier elite = EncounterTier.Elite();
    public EncounterTier boss = EncounterTier.Boss();

    public static EncounterTier GetTier(EncounterBalance balance, bool isElite, bool isBoss)
    {
        if (balance == null) balance = Resources.Load<EncounterBalance>("Balance/EncounterBalance");
        EncounterTier tier = balance == null ? null : (isBoss ? balance.boss : isElite ? balance.elite : balance.normal);
        return tier ?? (isBoss ? EncounterTier.Boss() : isElite ? EncounterTier.Elite() : EncounterTier.Normal());
    }
}

[Serializable]
public class EncounterTier
{
    [Range(1, 3)] public int enemyCount;
    public int earlyHealth, lateHealth, healthPerEnemy;
    public int earlyAttack, lateAttack, attackPerEnemy;
    public int earlyGuard, lateGuard;
    public int earlyGold, lateGold;
    public EnemyTurnPattern pattern;

    public EncounterStats Evaluate(int depth, int finalDepth, int enemyIndex = 0)
    {
        float progress = finalDepth <= 1 ? 0f : Mathf.Clamp01((depth - 1f) / (finalDepth - 1f));
        int index = Mathf.Clamp(enemyIndex, 0, Mathf.Clamp(enemyCount, 1, 3) - 1);
        return new EncounterStats(
            Mathf.Clamp(enemyCount, 1, 3),
            Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(earlyHealth, lateHealth, progress)) + index * healthPerEnemy),
            Mathf.Max(0, Mathf.RoundToInt(Mathf.Lerp(earlyAttack, lateAttack, progress)) + index * attackPerEnemy),
            Mathf.Max(0, Mathf.RoundToInt(Mathf.Lerp(earlyGuard, lateGuard, progress))),
            Mathf.Max(0, Mathf.RoundToInt(Mathf.Lerp(earlyGold, lateGold, progress))), pattern);
    }

    public static EncounterTier Normal() => new EncounterTier
    {
        enemyCount = 2, earlyHealth = 40, lateHealth = 72, healthPerEnemy = 6,
        earlyAttack = 7, lateAttack = 12, attackPerEnemy = 1,
        earlyGuard = 6, lateGuard = 10, earlyGold = 20, lateGold = 30,
        pattern = EnemyTurnPattern.Skirmisher
    };

    public static EncounterTier Elite() => new EncounterTier
    {
        enemyCount = 2, earlyHealth = 70, lateHealth = 120, healthPerEnemy = 12,
        earlyAttack = 10, lateAttack = 18, attackPerEnemy = 2,
        earlyGuard = 10, lateGuard = 16, earlyGold = 40, lateGold = 55,
        pattern = EnemyTurnPattern.Duelist
    };

    public static EncounterTier Boss() => new EncounterTier
    {
        enemyCount = 1, earlyHealth = 280, lateHealth = 280,
        earlyAttack = 22, lateAttack = 22, earlyGuard = 18, lateGuard = 18,
        pattern = EnemyTurnPattern.Leader
    };
}

public readonly struct EncounterStats
{
    public readonly int Count, Health, Attack, Guard, Gold;
    public readonly EnemyTurnPattern Pattern;
    public EncounterStats(int count, int health, int attack, int guard, int gold, EnemyTurnPattern pattern)
    {
        Count = count; Health = health; Attack = attack; Guard = guard; Gold = gold; Pattern = pattern;
    }
}
