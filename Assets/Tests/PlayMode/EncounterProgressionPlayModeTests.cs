using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EncounterProgressionPlayModeTests
{
    private GameObject arena;

    private CombatController CreateCombat(int depth, bool elite)
    {
        CollectibleManager.ResetForNewRun();
        arena = new GameObject("Encounter progression");
        arena.AddComponent<MapGenerator>().totalDepth = 10;
        GameObject nodeObject = new GameObject("Bound node");
        nodeObject.transform.SetParent(arena.transform);
        Node node = nodeObject.AddComponent<Node>();
        node.depth = depth;
        node.type = elite ? NodeType.EliteBattle : NodeType.Battle;
        GameObject combatObject = new GameObject("Combat");
        combatObject.transform.SetParent(arena.transform);
        CombatController combat = combatObject.AddComponent<CombatController>();
        combat.startCombatOnStart = false;
        combat.autoBuildUI = false;
        combat.completeNodeOnVictory = false;
        combat.isEliteBattle = elite;
        combat.BindNode(node, 1);
        PlayerStats.Instance.ResetForNewRun();
        DeckManager.Instance.ResetForNewRun();
        combat.StartCombat();
        return combat;
    }

    [UnityTest]
    public IEnumerator LateEliteUsesNodeDepthAndAlternatesActualIntent()
    {
        CombatController combat = CreateCombat(9, true);
        yield return null;
        Enemy[] enemies = combat.GetComponentsInChildren<Enemy>();
        Assert.That(enemies.Length, Is.EqualTo(2));
        Assert.That(enemies[0].maxHealth, Is.EqualTo(114));
        Assert.That(enemies[1].maxHealth, Is.EqualTo(126));
        Assert.That(enemies[0].baseAttack, Is.EqualTo(17));
        Assert.That(enemies[1].CurrentIntent, Is.EqualTo(EnemyIntentKind.Guard));
        combat.RequestEndPlayerTurn();
        Assert.That(PlayerStats.Instance.currentHealth, Is.EqualTo(83));
        Assert.That(enemies[1].currentShield, Is.EqualTo(15));
        Assert.That(enemies[0].CurrentIntent, Is.EqualTo(EnemyIntentKind.Guard));
        Assert.That(enemies[1].hpText.text, Does.Contain("重击 29"));
        combat.RequestEndPlayerTurn();
        Assert.That(PlayerStats.Instance.currentHealth, Is.EqualTo(54));
        Assert.That(enemies[0].currentShield, Is.EqualTo(15));
    }

    [UnityTest]
    public IEnumerator NormalVictoryGrantsCurrentDepthGoldExactlyOnce()
    {
        CombatController combat = CreateCombat(8, false);
        yield return null;
        Assert.That(combat.GoldReward, Is.EqualTo(28));
        int before = PlayerStats.Instance.gold;
        foreach (Enemy enemy in combat.GetComponentsInChildren<Enemy>()) enemy.TakeDamage(9999);
        combat.RequestEndPlayerTurn();
        yield return null;
        Assert.That(combat.AcceptsPlayerActions, Is.False);
        Assert.That(PlayerStats.Instance.gold, Is.EqualTo(before + 28));
        Assert.That(combat.GetComponentInChildren<RewardChoiceUI>(), Is.Not.Null);
        combat.RequestEndPlayerTurn();
        yield return null;
        Assert.That(PlayerStats.Instance.gold, Is.EqualTo(before + 28));
        Assert.That(combat.GetComponentsInChildren<RewardChoiceUI>().Length, Is.EqualTo(1));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(arena);
        if (EnemyManager.Instance != null) Object.Destroy(EnemyManager.Instance.gameObject);
        if (DeckManager.Instance != null) Object.Destroy(DeckManager.Instance.gameObject);
        if (CardEffectManager.Instance != null) Object.Destroy(CardEffectManager.Instance.gameObject);
        if (PlayerStats.Instance != null) Object.Destroy(PlayerStats.Instance.gameObject);
        EnemyManager.Instance = null;
        DeckManager.Instance = null;
        CardEffectManager.Instance = null;
        PlayerStats.Instance = null;
        CollectibleManager.ResetForNewRun();
        yield return null;
    }
}
