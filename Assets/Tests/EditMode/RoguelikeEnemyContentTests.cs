using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

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

    [UnityTest]
    public IEnumerator BossExitFinishesBeforeCombatCompletes()
    {
        yield return new EnterPlayMode();
        GameObject arena = new GameObject("Boss exit lifecycle");
        CombatController combat = arena.AddComponent<CombatController>();
        combat.startCombatOnStart = false;
        combat.autoBuildUI = false;
        combat.completeNodeOnVictory = false;
        combat.isBossBattle = true;
        combat.StartCombat();
        yield return null;

        Enemy boss = arena.GetComponentInChildren<Enemy>();
        RoguelikeEnemyPresentation visual = boss.GetComponent<RoguelikeEnemyPresentation>();
        Assert.That(visual.body.GetComponent<Animation>().GetClip("Attack"), Is.Not.Null);
        boss.AttackPlayer();
        Assert.That(visual.CurrentState, Is.EqualTo(RoguelikeEnemyPresentation.MotionState.Attack));
        PlayerStats.Instance.currentHealth = 1;
        PlayerStats.Instance.currentShield = 0;
        PlayerStats.Instance.ApplyStatus(StatusType.Bleed, 2);
        boss.currentHealth = 1;
        Assert.That(combat.UseBasicAttack(boss), Is.True);
        Assert.That(PlayerStats.Instance.currentHealth, Is.EqualTo(1),
            "The lethal basic attack must not advance to a damaging turn.");
        Assert.That(combat.AcceptsPlayerActions, Is.False,
            "Victory must lock input before the next frame.");
        EnemyManager.Instance.ClearNulls();
        Assert.That(EnemyManager.Instance.ActiveEnemies, Has.No.Member(boss));
        Assert.That(visual.CurrentState, Is.EqualTo(RoguelikeEnemyPresentation.MotionState.Death));

        FieldInfo active = typeof(CombatController).GetField("combatActive", BindingFlags.Instance | BindingFlags.NonPublic);
        yield return new WaitForSeconds(0.06f);
        Assert.That(boss != null, Is.True, "The final boss must remain visible during its exit.");
        Assert.That((bool)active.GetValue(combat), Is.True);
        Assert.That(combat.AcceptsPlayerActions, Is.False);
        PlayerStats.Instance.currentMana = 0;
        int healthAfterVictory = PlayerStats.Instance.currentHealth;
        combat.RequestEndPlayerTurn();
        Assert.That(PlayerStats.Instance.currentMana, Is.EqualTo(0));
        Assert.That(PlayerStats.Instance.currentHealth, Is.EqualTo(healthAfterVictory));
        CardData blockedCard = ScriptableObject.CreateInstance<CardData>();
        blockedCard.cardName = "晨辉壁垒";
        blockedCard.cost = 0;
        Assert.That(CardEffectManager.Instance.CanPlayCard(blockedCard), Is.False);
        Assert.That(CardEffectManager.Instance.PlayCard(blockedCard, null), Is.False);
        Object.Destroy(blockedCard);
        yield return new WaitForSeconds(0.5f);
        Assert.That(boss == null, Is.True);
        Assert.That((bool)active.GetValue(combat), Is.False);
        Object.Destroy(arena);
        yield return new ExitPlayMode();
    }
}
