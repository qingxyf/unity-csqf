using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class EncounterBalanceTests
{
    private readonly List<Object> cleanup = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object item in cleanup) if (item != null) Object.DestroyImmediate(item);
        cleanup.Clear();
        if (EnemyManager.Instance != null) Object.DestroyImmediate(EnemyManager.Instance.gameObject);
        EnemyManager.Instance = null;
        PlayerStats.Instance = null;
        CollectibleManager.ResetForNewRun();
    }

    [Test]
    public void EncounterAssetsAndFallbacksShareTheSameCurve()
    {
        EncounterBalance asset = Resources.Load<EncounterBalance>("Balance/EncounterBalance");
        Assert.That(asset, Is.Not.Null);
        EncounterBalance fallback = ScriptableObject.CreateInstance<EncounterBalance>();
        cleanup.Add(fallback);
        foreach (string prefab in new[] { "BattleContent", "EliteBattleContent", "BossContent" })
        {
            CombatController combat = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/NodeContent/" + prefab + ".prefab").GetComponent<CombatController>();
            Assert.That(combat.encounterBalance, Is.SameAs(asset), prefab);
        }
        EncounterTier[] authored = { asset.normal, asset.elite, asset.boss };
        EncounterTier[] defaults = { fallback.normal, fallback.elite, fallback.boss };
        for (int tier = 0; tier < authored.Length; tier++)
            for (int depth = 1; depth <= 10; depth++)
                for (int index = 0; index < authored[tier].enemyCount; index++)
                    Assert.That(authored[tier].Evaluate(depth, 10, index),
                        Is.EqualTo(defaults[tier].Evaluate(depth, 10, index)));
    }

    [Test]
    public void CurveIsBoundedMonotonicAndScalesWithMapLength()
    {
        EncounterTier tier = EncounterTier.Normal();
        Assert.That(tier.Evaluate(-5, 10).Health, Is.EqualTo(40));
        Assert.That(tier.Evaluate(50, 10).Health, Is.EqualTo(72));
        Assert.That(tier.Evaluate(1, 1).Health, Is.EqualTo(40));
        Assert.That(tier.Evaluate(10, 10), Is.EqualTo(tier.Evaluate(20, 20)));
        EncounterStats previous = tier.Evaluate(1, 10);
        for (int depth = 2; depth <= 10; depth++)
        {
            EncounterStats next = tier.Evaluate(depth, 10);
            Assert.That(next.Health, Is.GreaterThanOrEqualTo(previous.Health));
            Assert.That(next.Attack, Is.GreaterThanOrEqualTo(previous.Attack));
            Assert.That(next.Gold, Is.GreaterThanOrEqualTo(previous.Gold));
            previous = next;
        }
        Assert.That(EncounterTier.Elite().Evaluate(3, 10).Count, Is.EqualTo(2));
        Assert.That(EncounterTier.Elite().Evaluate(3, 10).Health, Is.LessThan(100));
    }

    [Test]
    public void AdvertisedGuardAndHeavyAttackMatchDamageAndDebuffs()
    {
        PlayerStats player = MakeObject("Player").AddComponent<PlayerStats>();
        player.InitializeStats();
        Enemy enemy = MakeObject("Elite").AddComponent<Enemy>();
        enemy.baseAttack = 20;
        enemy.ConfigureTurns(EnemyTurnPattern.Duelist, 12, 1);

        Assert.That(enemy.GetIntentDescription(), Does.Contain("防守 +12"));
        enemy.TakeTurn();
        Assert.That(player.currentHealth, Is.EqualTo(100));
        Assert.That(enemy.currentShield, Is.EqualTo(12));
        Assert.That(enemy.hasDealtDamage, Is.False);
        Assert.That(enemy.CurrentIntent, Is.EqualTo(EnemyIntentKind.HeavyAttack));
        enemy.ApplyStatus(StatusType.Weak, 2);
        enemy.ApplyStatus(StatusType.Burn, 2);
        Assert.That(enemy.GetIntentValue(), Is.EqualTo(10), "(20 * 1.5 - 10) / 2");
        Assert.That(enemy.GetIntentDescription(), Does.Contain("重击 10"));
        enemy.TakeTurn();
        Assert.That(player.currentHealth, Is.EqualTo(90));
        Assert.That(enemy.CurrentIntent, Is.EqualTo(EnemyIntentKind.Attack));
    }

    [Test]
    public void ControlConsumesIntentAndDeadEnemyCannotAct()
    {
        PlayerStats player = MakeObject("Player").AddComponent<PlayerStats>();
        player.InitializeStats();
        Enemy enemy = MakeObject("Enemy").AddComponent<Enemy>();
        enemy.baseAttack = 20;
        enemy.ConfigureTurns(EnemyTurnPattern.Duelist, 12, 2);
        enemy.ApplyStatus(StatusType.Freeze, 1);
        Assert.That(enemy.GetIntentDescription(), Is.EqualTo("意图：无法行动"));
        enemy.TakeTurn();
        Assert.That(player.currentHealth, Is.EqualTo(100));
        enemy.ProcessTurnEnd();
        Assert.That(enemy.CurrentIntent, Is.EqualTo(EnemyIntentKind.Attack));
        Assert.That(enemy.GetIntentValue(), Is.EqualTo(20));
        enemy.currentHealth = 0;
        enemy.TakeTurn();
        Assert.That(player.currentHealth, Is.EqualTo(100));
        Assert.That(enemy.hasDealtDamage, Is.False);
    }

    [Test]
    public void OpeningFloorsExcludeElitesAndEveryFinalRouteHasCamp()
    {
        Random.State previous = Random.state;
        try
        {
            Random.InitState(214);
            MapGenerator map = MakeObject("Map").AddComponent<MapGenerator>();
            map.totalDepth = 10;
            map.battleWeight = map.eventWeight = map.shopWeight = map.treasureWeight = map.campWeight = 0;
            map.eliteBattleWeight = 100;
            map.GenerateMap();
            List<List<Node>> layers = map.GetNodesByDepth();
            foreach (Node node in layers[1]) Assert.That(node.type, Is.EqualTo(NodeType.Battle));
            foreach (Node node in layers[2]) Assert.That(node.type, Is.EqualTo(NodeType.Battle));
            Assert.That(layers[3].Exists(node => node.type == NodeType.EliteBattle), Is.True);
            foreach (Node parent in layers[9])
            {
                Assert.That(parent.nextNodes, Is.Not.Empty);
                foreach (Node node in parent.nextNodes) Assert.That(node.type, Is.EqualTo(NodeType.Camp));
            }
            foreach (Node camp in layers[10])
                Assert.That(camp.nextNodes[0].type, Is.EqualTo(NodeType.Boss));
        }
        finally { Random.state = previous; }
    }

    [TestCase("big_rice")]
    [TestCase("special_metal_basin")]
    public void ExclusiveCollectiblesHaveDedicatedIconsInAssetsAndFallbacks(string id)
    {
        CollectibleData asset = Resources.Load<CollectibleData>("Collectibles/" + id);
        CollectibleData fallback = CollectibleCatalog.CreateFallback(id);
        cleanup.Add(fallback);
        Sprite icon = Resources.Load<Sprite>("Icons/Collectibles/" + id);
        Assert.That(icon, Is.Not.Null);
        Assert.That(asset.icon, Is.SameAs(icon));
        Assert.That(fallback.icon, Is.SameAs(icon));
        Assert.That(asset.eventExclusive, Is.True);
    }

    private GameObject MakeObject(string name)
    {
        GameObject instance = new GameObject(name);
        cleanup.Add(instance);
        return instance;
    }
}
