using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class RoguelikeEnemyContentTests
{
    private readonly List<GameObject> cleanup = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject instance in cleanup)
            if (instance != null) Object.DestroyImmediate(instance);
        cleanup.Clear();
        if (EnemyManager.Instance != null) Object.DestroyImmediate(EnemyManager.Instance.gameObject);
        EnemyManager.Instance = null;
    }

    [TestCase("BattleContent", "DuskScavenger", false, false)]
    [TestCase("EliteBattleContent", "CopperplumeDuelist", true, false)]
    [TestCase("BossContent", "EclipseArchivist", false, true)]
    public void EncounterPrefabsBindOriginalSpritePairsAndCorrectBattleRules(
        string encounter, string archetype, bool elite, bool boss)
    {
        GameObject content = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/NodeContent/" + encounter + ".prefab");
        CombatController controller = content.GetComponent<CombatController>();
        Assert.That(controller.cardsPerTurn, Is.EqualTo(2));
        Assert.That(controller.isEliteBattle, Is.EqualTo(elite));
        Assert.That(controller.isBossBattle, Is.EqualTo(boss));
        GameObject prefab = Resources.Load<GameObject>("Enemies/" + archetype);
        Assert.That(controller.enemyPrefab, Is.SameAs(prefab));
        Assert.That(prefab.GetComponent<Enemy>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<Collider2D>(), Is.Not.Null);
        RoguelikeEnemyPresentation presentation = prefab.GetComponent<RoguelikeEnemyPresentation>();
        Assert.That(presentation, Is.Not.Null);
        Assert.That(presentation.body, Is.Not.Null);
        Assert.That(presentation.body.transform, Is.Not.SameAs(prefab.transform));
        Assert.That(presentation.idleSprite, Is.Not.Null);
        Assert.That(presentation.attackSprite, Is.Not.Null);
        Assert.That(presentation.attackSprite, Is.Not.SameAs(presentation.idleSprite));
        Assert.That(presentation.body.sprite, Is.SameAs(presentation.idleSprite));
    }

    [Test]
    public void RiceKeeperEventChallengerHasBothImportedPosesAtCombatScale()
    {
        GameObject prefab = Resources.Load<GameObject>("Enemies/RiceKeeper");
        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<Enemy>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<Collider2D>(), Is.Not.Null);
        RoguelikeEnemyPresentation presentation = prefab.GetComponent<RoguelikeEnemyPresentation>();
        Assert.That(presentation.idleSprite, Is.Not.Null);
        Assert.That(presentation.attackSprite, Is.Not.Null);
        Assert.That(presentation.body.sprite, Is.SameAs(presentation.idleSprite));
        Assert.That(presentation.attackSprite.bounds.size.y / presentation.idleSprite.bounds.size.y,
            Is.InRange(0.85f, 1.05f), "The wider attack source must not shrink the character during its swing.");
    }

    [Test]
    public void DeadEnemyCannotBeRediscoveredByManagerOrActAgain()
    {
        GameObject enemyObject = new GameObject("Dead enemy awaiting visual cleanup");
        cleanup.Add(enemyObject);
        Enemy enemy = enemyObject.AddComponent<Enemy>();
        // Simulate a dead object still in the scene during its exit animation.
        enemy.GetCurrentHealth();
        enemy.currentHealth = 0;
        EnemyManager.EnsureInstance();
        EnemyManager.Instance.ActiveEnemies.Add(enemy);
        EnemyManager.Instance.ClearNulls();
        EnemyManager.Instance.Register(enemy);

        Assert.That(EnemyManager.Instance.ActiveEnemies, Has.No.Member(enemy));
        Assert.That(enemy.CanAct(), Is.False);
        enemy.AttackPlayer();
        Assert.That(enemy.hasDealtDamage, Is.False);
    }

}
