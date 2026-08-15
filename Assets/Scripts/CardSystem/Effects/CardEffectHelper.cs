using UnityEngine;
using System.Collections.Generic;

public static class CardEffectHelper
{
    public static void DamageAllEnemies(List<Enemy> enemies, int amount, DamageType type)
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.TakeDamage(amount, type);
        }
    }

    public static void ApplyStatusToAll(List<Enemy> enemies, StatusType status, int duration, int value = 0)
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemy.ApplyStatus(status, duration, value);
        }
    }

    public static List<Enemy> GetAdjacentEnemies(List<Enemy> enemies, Enemy target)
    {
        List<Enemy> adjacent = new List<Enemy>();
        if (enemies == null || target == null) return adjacent;

        List<Enemy> aliveEnemies = enemies.FindAll(e => e != null && !e.IsDead());
        int index = aliveEnemies.IndexOf(target);
        if (index < 0) return adjacent;

        if (index > 0) adjacent.Add(aliveEnemies[index - 1]);
        if (index < aliveEnemies.Count - 1) adjacent.Add(aliveEnemies[index + 1]);
        return adjacent;
    }
}
