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

        GameObject go = new GameObject("EnemyManager");
        go.AddComponent<EnemyManager>();
    }

    public void Register(Enemy e)
    {
        if (!ActiveEnemies.Contains(e))
            ActiveEnemies.Add(e);
    }

    public void Unregister(Enemy e)
    {
        ActiveEnemies.Remove(e);
    }

    public void ClearNulls()
    {
        ActiveEnemies.RemoveAll(e => e == null);
    }
}
