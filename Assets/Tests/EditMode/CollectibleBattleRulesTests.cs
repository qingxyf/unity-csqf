using NUnit.Framework;
using UnityEngine;

public class CollectibleBattleRulesTests
{
    private GameObject playerObject;
    private GameObject shopObject;
    private Random.State randomState;

    [SetUp]
    public void SetUp()
    {
        randomState = Random.state;
        CollectibleManager.ResetForNewRun();
        playerObject = new GameObject("Collectible battle rules player");
        PlayerStats.Instance = playerObject.AddComponent<PlayerStats>();
        PlayerStats.Instance.InitializeStats();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(shopObject);
        Object.DestroyImmediate(playerObject);
        PlayerStats.Instance = null;
        CollectibleManager.ResetForNewRun();
        Random.state = randomState;
    }

    [Test]
    public void RiceHealsAndAddsEnergyOnlyInFirstThreeTurnsOfEveryBattle()
    {
        PlayerStats player = PlayerStats.Instance;
        CollectibleManager.AddCollectible(Load("big_rice"));
        for (int battle = 0; battle < 2; battle++)
        {
            player.currentHealth = 40;
            player.BeginCombat();
            player.StartTurn();
            Assert.That(player.currentHealth, Is.EqualTo(50));
            Assert.That(player.currentMana, Is.EqualTo(7));
            player.currentMana = 0;
            player.StartTurn();
            Assert.That(player.currentHealth, Is.EqualTo(60));
            Assert.That(player.currentMana, Is.EqualTo(3));
            player.currentMana = 0;
            player.StartTurn();
            Assert.That(player.currentHealth, Is.EqualTo(70));
            Assert.That(player.currentMana, Is.EqualTo(4));
            player.currentMana = 0;
            player.StartTurn();
            Assert.That(player.currentHealth, Is.EqualTo(70));
            Assert.That(player.currentMana, Is.EqualTo(3));
            player.OnBattleEnd();
        }
        player.currentHealth = 98;
        player.BeginCombat();
        player.currentMana = 10;
        player.StartTurn();
        Assert.That(player.currentHealth, Is.EqualTo(100));
        Assert.That(player.currentMana, Is.EqualTo(10));
    }

    [TestCase(true, 10, 95)]
    [TestCase(false, 10, 90)]
    [TestCase(true, 3, 100)]
    public void BasinRollsPerHitAndCannotCauseNegativeDamage(bool proc, int damage, int health)
    {
        CollectibleManager.AddCollectible(Load("special_metal_basin"));
        Random.state = FindDamageRoll(proc);
        PlayerStats.Instance.TakeDamage(damage);
        Assert.That(PlayerStats.Instance.currentHealth, Is.EqualTo(health));
        Random.state = FindDamageRoll(!proc);
        PlayerStats.Instance.TakeDamage(10);
        Assert.That(PlayerStats.Instance.currentHealth, Is.EqualTo(health - (proc ? 10 : 5)));
    }

    [Test]
    public void BasinReducesBeforeShieldsAndDoesNotReduceHealthPayments()
    {
        CollectibleManager.AddCollectible(Load("special_metal_basin"));
        PlayerStats player = PlayerStats.Instance;
        player.currentShield = 8;
        Random.state = FindDamageRoll(true);
        player.TakeDamage(10);
        Assert.That(player.currentShield, Is.EqualTo(3));
        Assert.That(player.currentHealth, Is.EqualTo(100));
        Random.state = FindDamageRoll(true);
        ((IDamageable)player).TakeDamage(10, DamageType.Physical);
        Assert.That(player.currentShield, Is.Zero);
        Assert.That(player.currentHealth, Is.EqualTo(98));
        player.PayHealth(10);
        Assert.That(player.currentHealth, Is.EqualTo(88));
    }

    [TestCase(29, true)]
    [TestCase(30, false)]
    public void NormalBattleDropUsesThirtyPercentBoundary(int roll, bool expectReward)
    {
        Random.state = FindIntegerRoll(100, roll);
        CollectibleData reward = CollectibleManager.GrantBattleCollectible(false, EventCombatReward.Standard);
        Assert.That(reward != null, Is.EqualTo(expectReward));
        Assert.That(CollectibleManager.OwnedCollectibles.Count, Is.EqualTo(expectReward ? 1 : 0));
    }

    [Test]
    public void EliteBattleStillGuaranteesOneCollectible()
    {
        Random.state = FindIntegerRoll(100, 99);
        Assert.That(CollectibleManager.GrantBattleCollectible(true, EventCombatReward.Standard), Is.Not.Null);
        Assert.That(CollectibleManager.OwnedCollectibles.Count, Is.EqualTo(1));
    }

    [TestCase(0, "special_metal_basin", "big_rice")]
    [TestCase(1, "big_rice", "special_metal_basin")]
    public void RiceVictoryGrantsOneKeepsakeAndReservesOtherForNextShop(int roll, string rewardId, string shopId)
    {
        Random.state = FindIntegerRoll(2, roll);
        CollectibleData reward = CollectibleManager.GrantBattleCollectible(true, EventCombatReward.RiceKeepsakes);
        Assert.That(reward.collectibleId, Is.EqualTo(rewardId));
        Assert.That(CollectibleManager.OwnedCollectibles.Count, Is.EqualTo(1));
        Assert.That(CollectibleManager.PendingShopCollectibleId, Is.EqualTo(shopId));
        Assert.That(CollectibleManager.ClaimNextShopCollectible().collectibleId, Is.EqualTo(shopId));
        Assert.That(CollectibleManager.ClaimNextShopCollectible(), Is.Null);
    }

    [Test]
    public void NewRunClearsPendingShopGuaranteeAndOrdinaryPoolNeverGivesEventKeepsakes()
    {
        CollectibleManager.GrantBattleCollectible(true, EventCombatReward.RiceKeepsakes);
        CollectibleManager.ResetForNewRun();
        Assert.That(CollectibleManager.PendingShopCollectibleId, Is.Null);
        Assert.That(CollectibleManager.ClaimNextShopCollectible(), Is.Null);
        for (int i = 0; i < 15; i++)
        {
            CollectibleData reward = CollectibleManager.GrantBattleCollectible(true, EventCombatReward.Standard);
            Assert.That(reward, Is.Not.Null);
            Assert.That(reward.eventExclusive, Is.False);
        }
        Assert.That(CollectibleManager.CreateRandomCollectible(), Is.Null);
    }

    [Test]
    public void WalletDiscountsCardsAndEveryShopServiceByTwentyPercent()
    {
        CollectibleManager.AddCollectible(Load("old_wallet"));
        shopObject = new GameObject("Discount test shop");
        ShopManager shop = shopObject.AddComponent<ShopManager>();
        CardData card = ScriptableObject.CreateInstance<CardData>();
        try
        {
            card.cost = 1;
            Assert.That(shop.GetCardPrice(card), Is.EqualTo(32));
            Assert.That(shop.GetRemoveServiceCost(), Is.EqualTo(40));
            Assert.That(shop.GetHealServiceCost(), Is.EqualTo(24));
            Assert.That(shop.GetRefreshServiceCost(), Is.EqualTo(12));
        }
        finally { Object.DestroyImmediate(card); }
    }

    private static CollectibleData Load(string id)
    {
        CollectibleData data = Resources.Load<CollectibleData>("Collectibles/" + id);
        Assert.That(data, Is.Not.Null, id);
        return data;
    }

    private static Random.State FindIntegerRoll(int max, int expected)
    {
        for (int seed = 0; seed < 10000; seed++)
        {
            Random.InitState(seed);
            Random.State state = Random.state;
            if (Random.Range(0, max) == expected) return state;
        }
        throw new System.InvalidOperationException("Unable to find integer random fixture.");
    }

    private static Random.State FindDamageRoll(bool proc)
    {
        for (int seed = 0; seed < 10000; seed++)
        {
            Random.InitState(seed);
            Random.State state = Random.state;
            if ((Random.value < 0.3f) == proc) return state;
        }
        throw new System.InvalidOperationException("Unable to find damage random fixture.");
    }
}
