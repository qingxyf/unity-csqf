using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    public List<Enemy> ActiveEnemies = new List<Enemy>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public static void EnsureInstance()
    {
        if (Instance != null) return;

        EnemyManager existing = FindObjectOfType<EnemyManager>();
        if (existing != null)
        {
            Instance = existing;
            existing.RegisterExistingEnemies();
            return;
        }

        GameObject go = new GameObject("EnemyManager");
        EnemyManager created = go.AddComponent<EnemyManager>();
        if (Instance == null)
            Instance = created;
        created.RegisterExistingEnemies();
    }

    public void RegisterExistingEnemies()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null)
                Register(enemy);
        }
    }

    public void Register(Enemy e)
    {
        if (e == null || e.IsDead() || !e.isActiveAndEnabled) return;
        if (!ActiveEnemies.Contains(e))
            ActiveEnemies.Add(e);
    }

    public void Unregister(Enemy e)
    {
        ActiveEnemies.Remove(e);
    }

    public void ClearNulls()
    {
        ActiveEnemies.RemoveAll(e => e == null || e.IsDead() || !e.isActiveAndEnabled);
        if (ActiveEnemies.Count == 0)
            RegisterExistingEnemies();
    }
}
