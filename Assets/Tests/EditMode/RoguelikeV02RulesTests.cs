using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RoguelikeV02RulesTests
{
    private GameObject playerObject;
    private GameObject enemyManagerObject;
    private GameObject effectManagerObject;
    private GameObject combatObject;
    private GameObject enemyObject;
    private GameObject shopObject;
    private GameObject eventObject;
    private GameObject deckObject;

    [TearDown]
    public void TearDown()
    {
        Destroy(playerObject);
        Destroy(enemyManagerObject);
        Destroy(effectManagerObject);
        Destroy(combatObject);
        Destroy(enemyObject);
        Destroy(shopObject);
        Destroy(eventObject);
        Destroy(deckObject);

        PlayerStats.Instance = null;
        EnemyManager.Instance = null;
        CardEffectManager.Instance = null;
        DeckManager.Instance = null;
        CollectibleManager.Clear();
    }

    [Test]
    public void PlayerStartsWithEnergyPoolAndGoldEconomy()
    {
        PlayerStats stats = CreatePlayerStats();

        Assert.That(stats.initialMana, Is.EqualTo(6));
        Assert.That(stats.maxMana, Is.EqualTo(10));
        Assert.That(stats.manaRegenPerTurn, Is.EqualTo(2));
        Assert.That(stats.currentMana, Is.EqualTo(6));

        FieldInfo goldField = typeof(PlayerStats).GetField("gold");
        Assert.That(goldField, Is.Not.Null);
        Assert.That((int)goldField.GetValue(stats), Is.GreaterThanOrEqualTo(60));

        MethodInfo gainGold = typeof(PlayerStats).GetMethod("GainGold", new[] { typeof(int) });
        MethodInfo spendGold = typeof(PlayerStats).GetMethod("SpendGold", new[] { typeof(int) });
        Assert.That(gainGold, Is.Not.Null);
        Assert.That(spendGold, Is.Not.Null);

        int before = (int)goldField.GetValue(stats);
        gainGold.Invoke(stats, new object[] { 25 });
        Assert.That((int)goldField.GetValue(stats), Is.EqualTo(before + 25));

        bool spent = (bool)spendGold.Invoke(stats, new object[] { 10 });
        Assert.That(spent, Is.True);
        Assert.That((int)goldField.GetValue(stats), Is.EqualTo(before + 15));
    }

    [Test]
    public void PlayerRegenerationScalesAtTurnsThreeAndFiveAndResetsEachBattle()
    {
        PlayerStats stats = CreatePlayerStats();

        stats.BeginCombat();
        stats.StartTurn();
        Assert.That(stats.currentMana, Is.EqualTo(6));

        foreach (int expected in new[] { 2, 3, 3, 4, 4 })
        {
            stats.currentMana = 0;
            stats.StartTurn();
            Assert.That(stats.currentMana, Is.EqualTo(expected));
        }
        stats.currentMana = 9;
        stats.StartTurn();
        Assert.That(stats.currentMana, Is.EqualTo(10));
        stats.OnBattleEnd();
        stats.BeginCombat();
        stats.StartTurn();
        Assert.That(stats.currentMana, Is.EqualTo(6));
        stats.currentMana = 0;
        stats.StartTurn();
        Assert.That(stats.currentMana, Is.EqualTo(2));
    }

    [Test]
    public void PayHealthBypassesShieldAndCannotKillVoluntaryCosts()
    {
        PlayerStats stats = CreatePlayerStats();
        stats.currentHealth = 20;
        stats.currentShield = 999;
        stats.damageReductionNextHit = 0.75f;
        stats.flatDamageReductionNextHit = 10;

        MethodInfo payHealth = typeof(PlayerStats).GetMethod("PayHealth", new[] { typeof(int) });
        Assert.That(payHealth, Is.Not.Null);

        bool paid = (bool)payHealth.Invoke(stats, new object[] { 5 });

        Assert.That(paid, Is.True);
        Assert.That(stats.currentHealth, Is.EqualTo(15));
        Assert.That(stats.currentShield, Is.EqualTo(999));
        Assert.That(stats.damageReductionNextHit, Is.EqualTo(0.75f));
        Assert.That(stats.flatDamageReductionNextHit, Is.EqualTo(10));

        bool denied = (bool)payHealth.Invoke(stats, new object[] { 99 });

        Assert.That(denied, Is.False);
        Assert.That(stats.currentHealth, Is.EqualTo(15));
    }

    [Test]
    public void BasicAttackCostsManaDealsDamageAndEndsThePlayerTurn()
    {
        PlayerStats stats = CreatePlayerStats();
        stats.baseAttack = 10;
        CreateEnemyManager();
        CreateEffectManager();

        Enemy enemy = CreateEnemy(100, 7);

        combatObject = new GameObject("CombatController");
        CombatController controller = combatObject.AddComponent<CombatController>();
        controller.autoBuildUI = false;
        controller.autoCreateManagers = false;
        controller.startCombatOnStart = false;
        controller.StartCombat();

        MethodInfo basicAttack = typeof(CombatController).GetMethod("UseBasicAttack", new[] { typeof(Enemy) });
        Assert.That(basicAttack, Is.Not.Null);

        bool used = (bool)basicAttack.Invoke(controller, new object[] { enemy });

        Assert.That(used, Is.True);
        Assert.That(enemy.currentHealth, Is.EqualTo(90));
        Assert.That(stats.currentHealth, Is.EqualTo(93));
        Assert.That(stats.currentMana, Is.EqualTo(7));
        Assert.That(enemy.hasDealtDamage, Is.True);
    }

    [Test]
    public void DrawCardInCombatRespectsHandLimit()
    {
        DeckManager deck = CreateDeckManager();
        deck.maxHandSize = 10;

        for (int i = 0; i < 9; i++)
            deck.hand.Add(CreateCard($"Hand {i}", 1));

        for (int i = 0; i < 5; i++)
            deck.drawPile.Add(CreateCard($"Draw {i}", 1));

        deck.DrawCardInCombat(5);

        Assert.That(deck.hand.Count, Is.EqualTo(10));
        Assert.That(deck.drawPile.Count, Is.EqualTo(4));
    }

    [Test]
    public void EndTurnKeepsHandAndDrawsTwoCards()
    {
        PlayerStats stats = CreatePlayerStats();
        CreateEnemyManager();
        CreateEffectManager();
        DeckManager deck = CreateDeckManager();
        deck.maxHandSize = 10;

        for (int i = 0; i < 10; i++)
            deck.backpack.Add(CreateCard($"Starter {i}", 1));

        combatObject = new GameObject("CombatController");
        CombatController controller = combatObject.AddComponent<CombatController>();
        controller.autoBuildUI = false;
        controller.autoCreateManagers = false;
        controller.startCombatOnStart = false;
        controller.cardsPerTurn = 2;
        controller.StartCombat();

        Assert.That(deck.hand.Count, Is.EqualTo(5));
        Assert.That(stats.currentMana, Is.EqualTo(6));

        controller.RequestEndPlayerTurn();

        Assert.That(deck.discardPile.Count, Is.EqualTo(0));
        Assert.That(deck.hand.Count, Is.EqualTo(7));
        Assert.That(stats.currentMana, Is.EqualTo(8));
    }

    [Test]
    public void ShopPricesScaleWithCardCostAndRemovalUseCount()
    {
        shopObject = new GameObject("ShopManager");
        ShopManager shop = shopObject.AddComponent<ShopManager>();

        MethodInfo getCardPrice = typeof(ShopManager).GetMethod("GetCardPrice", new[] { typeof(CardData) });
        MethodInfo getRemoveCost = typeof(ShopManager).GetMethod("GetRemoveServiceCost", System.Type.EmptyTypes);
        Assert.That(getCardPrice, Is.Not.Null);
        Assert.That(getRemoveCost, Is.Not.Null);

        CardData lowCostCard = CreateCard("Low", 1);
        CardData highCostCard = CreateCard("High", 5);

        try
        {
            int lowPrice = (int)getCardPrice.Invoke(shop, new object[] { lowCostCard });
            int highPrice = (int)getCardPrice.Invoke(shop, new object[] { highCostCard });

            Assert.That(lowPrice, Is.EqualTo(40));
            Assert.That(highPrice, Is.EqualTo(90));
            Assert.That(highPrice, Is.GreaterThan(lowPrice));
            Assert.That((int)getRemoveCost.Invoke(shop, null), Is.EqualTo(50));
        }
        finally
        {
            Object.DestroyImmediate(lowCostCard);
            Object.DestroyImmediate(highCostCard);
        }
    }

    [Test]
    public void EventHealthCostsUseDirectPaymentInsteadOfDamage()
    {
        PlayerStats stats = CreatePlayerStats();
        stats.currentHealth = 20;
        stats.currentShield = 999;

        eventObject = new GameObject("EventManager");
        EventManager eventManager = eventObject.AddComponent<EventManager>();

        EventChoice choice = new EventChoice
        {
            buttonText = "Pay",
            healthChange = -5
        };

        MethodInfo applyChoiceEffects = typeof(EventManager).GetMethod(
            "ApplyChoiceEffects",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(applyChoiceEffects, Is.Not.Null);

        applyChoiceEffects.Invoke(eventManager, new object[] { choice });

        Assert.That(stats.currentHealth, Is.EqualTo(15));
        Assert.That(stats.currentShield, Is.EqualTo(999));
    }

    [Test]
    public void TorrentKillRestoresThreeEnergy()
    {
        PlayerStats stats = CreatePlayerStats();
        stats.currentMana = 5;
        stats.maxMana = 10;
        Enemy enemy = CreateEnemy(1, 0);

        TorrentEffect torrent = ScriptableObject.CreateInstance<TorrentEffect>();
        try
        {
            torrent.Execute(new CardEffectContext { Player = stats, Target = enemy });

            Assert.That(stats.currentMana, Is.EqualTo(8));
        }
        finally
        {
            Object.DestroyImmediate(torrent);
        }
    }

    [Test]
    public void MaxHealthCollectibleIncreasesPlayerMaximumHealth()
    {
        PlayerStats stats = CreatePlayerStats();
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleId = "test_max_health";
        collectible.collectibleName = "生命标本";
        collectible.effectType = CollectibleEffectType.MaxHealth;
        collectible.amount = 15;

        try
        {
            CollectibleManager.AddCollectible(collectible);

            Assert.That(stats.maxHealth, Is.EqualTo(115));
            Assert.That(stats.currentHealth, Is.EqualTo(115));
        }
        finally
        {
            Object.DestroyImmediate(collectible);
        }
    }

    [Test]
    public void MaxEnergyCollectibleIncreasesPlayerEnergyCap()
    {
        PlayerStats stats = CreatePlayerStats();
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleId = "test_max_energy";
        collectible.collectibleName = "Energy Core";
        collectible.effectType = CollectibleEffectType.MaxMana;
        collectible.amount = 1;

        try
        {
            CollectibleManager.AddCollectible(collectible);

            Assert.That(stats.maxMana, Is.EqualTo(11));
            Assert.That(stats.currentMana, Is.EqualTo(7));
        }
        finally
        {
            Object.DestroyImmediate(collectible);
        }
    }

    [Test]
    public void ShieldCollectiblesGrantShieldOnlyOnEvenTurnsOfEachBattle()
    {
        PlayerStats stats = CreatePlayerStats();
        CollectibleManager.AddCollectible(Resources.Load<CollectibleData>("Collectibles/aegis_shard"));
        CollectibleManager.AddCollectible(Resources.Load<CollectibleData>("Collectibles/warding_coin"));
        for (int battle = 0; battle < 2; battle++)
        {
            stats.BeginCombat();
            Assert.That(stats.currentShield, Is.Zero);
            foreach (int expected in new[] { 0, 13, 0, 13, 0, 13 })
            {
                stats.currentShield = 0;
                stats.StartTurn();
                Assert.That(stats.currentShield, Is.EqualTo(expected));
            }
            stats.OnBattleEnd();
        }
    }

    [TestCase("energy_core", 11, 3)]
    [TestCase("mana_prism", 12, 4)]
    public void EnergyCollectiblesKeepTheirCapAndAddEnergyOnTurnTwo(string id, int cap, int secondTurnEnergy)
    {
        PlayerStats stats = CreatePlayerStats();
        CollectibleManager.AddCollectible(Resources.Load<CollectibleData>("Collectibles/" + id));
        Assert.That(stats.maxMana, Is.EqualTo(cap));
        for (int battle = 0; battle < 2; battle++)
        {
            stats.BeginCombat();
            stats.StartTurn();
            Assert.That(stats.currentMana, Is.EqualTo(6));
            stats.currentMana = 0;
            stats.StartTurn();
            Assert.That(stats.currentMana, Is.EqualTo(secondTurnEnergy));
            stats.currentMana = 0;
            stats.StartTurn();
            Assert.That(stats.currentMana, Is.EqualTo(3));
            stats.OnBattleEnd();
        }
    }

    [Test]
    public void EventChoiceCanGrantCollectible()
    {
        CreatePlayerStats();
        CreateDeckManager();
        eventObject = new GameObject("EventManager");
        EventManager eventManager = eventObject.AddComponent<EventManager>();

        EventChoice choice = new EventChoice
        {
            buttonText = "Take relic",
            grantCollectible = true
        };

        MethodInfo applyChoiceEffects = typeof(EventManager).GetMethod(
            "ApplyChoiceEffects",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(applyChoiceEffects, Is.Not.Null);

        applyChoiceEffects.Invoke(eventManager, new object[] { choice });

        Assert.That(CollectibleManager.OwnedCollectibles.Count, Is.EqualTo(1));

        PropertyInfo lastGranted = typeof(EventManager).GetProperty("LastGrantedCollectibleName");
        Assert.That(lastGranted, Is.Not.Null);
        Assert.That((string)lastGranted.GetValue(eventManager), Is.Not.Empty);
    }

    [Test]
    public void CollectibleSummaryListsOwnedCollectibleNames()
    {
        CollectibleData first = ScriptableObject.CreateInstance<CollectibleData>();
        first.collectibleId = "test_summary_life";
        first.collectibleName = "Life Specimen";
        CollectibleData second = ScriptableObject.CreateInstance<CollectibleData>();
        second.collectibleId = "test_summary_aegis";
        second.collectibleName = "Aegis Shard";

        try
        {
            CollectibleManager.AddCollectible(first);
            CollectibleManager.AddCollectible(second);

            string summary = CollectibleManager.GetOwnedCollectibleSummary();

            Assert.That(summary, Does.Contain("Life Specimen"));
            Assert.That(summary, Does.Contain("Aegis Shard"));
            Assert.That(summary, Does.Contain("2"));
        }
        finally
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }
    }

    [Test]
    public void ElementCostCollectibleDiscountsOnlyFirstMatchingCardEachTurn()
    {
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleId = "test_water_discount";
        collectible.collectibleName = "寒潮坠饰";
        collectible.effectType = CollectibleEffectType.FirstElementCardCostReduction;
        collectible.element = CardElement.Water;
        collectible.amount = 1;
        CardData waterCard = CreateCard("Water", 2);
        waterCard.element = CardElement.Water;

        try
        {
            CollectibleManager.AddCollectible(collectible);
            CollectibleManager.OnPlayerTurnStart();

            Assert.That(CollectibleManager.GetEffectiveCardCost(waterCard), Is.EqualTo(1));

            CollectibleManager.NotifyCardPlayed(waterCard);

            Assert.That(CollectibleManager.GetEffectiveCardCost(waterCard), Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(collectible);
            Object.DestroyImmediate(waterCard);
        }
    }

    [Test]
    public void ElementDamageCollectibleAddsDamage()
    {
        CreateEnemyManager();
        Enemy enemy = CreateEnemy(100, 0);
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleId = "test_fire_damage";
        collectible.collectibleName = "火焰徽章";
        collectible.effectType = CollectibleEffectType.ElementDamageBonus;
        collectible.damageType = DamageType.Fire;
        collectible.amount = 10;

        try
        {
            CollectibleManager.AddCollectible(collectible);

            enemy.TakeDamage(5, DamageType.Fire);

            Assert.That(enemy.currentHealth, Is.EqualTo(85));
        }
        finally
        {
            Object.DestroyImmediate(collectible);
        }
    }

    [Test]
    public void PurifiedDoesNotProtectEnemiesFromLaterDebuffs()
    {
        CreateEnemyManager();
        Enemy enemy = CreateEnemy(100, 10);

        enemy.ApplyStatus(StatusType.Purified, 1);
        enemy.ApplyStatus(StatusType.Weak, 1);

        Assert.That(enemy.HasStatus(StatusType.Purified), Is.True);
        Assert.That(enemy.HasStatus(StatusType.Weak), Is.True);
    }

    [Test]
    public void FixedTargetHealingEffectsDoNotLowerCurrentHealth()
    {
        PlayerStats stats = CreatePlayerStats();
        stats.maxHealth = 150;
        stats.currentHealth = 130;

        SilentMoistureEffect silentMoisture = ScriptableObject.CreateInstance<SilentMoistureEffect>();
        VolcanoEchoEffect volcanoEcho = ScriptableObject.CreateInstance<VolcanoEchoEffect>();
        CardEffectContext context = new CardEffectContext { Player = stats };

        try
        {
            silentMoisture.Execute(context);
            Assert.That(stats.currentHealth, Is.EqualTo(130));

            volcanoEcho.Execute(context);
            Assert.That(stats.currentHealth, Is.EqualTo(130));
        }
        finally
        {
            Object.DestroyImmediate(silentMoisture);
            Object.DestroyImmediate(volcanoEcho);
        }
    }

    [Test]
    public void AdjacentEnemyHelperReturnsOnlyImmediateNeighbors()
    {
        GameObject aObject = new GameObject("A");
        GameObject bObject = new GameObject("B");
        GameObject cObject = new GameObject("C");
        GameObject dObject = new GameObject("D");

        try
        {
            Enemy a = aObject.AddComponent<Enemy>();
            Enemy b = bObject.AddComponent<Enemy>();
            Enemy c = cObject.AddComponent<Enemy>();
            Enemy d = dObject.AddComponent<Enemy>();

            var enemies = new System.Collections.Generic.List<Enemy> { a, b, c, d };

            var middleNeighbors = CardEffectHelper.GetAdjacentEnemies(enemies, b);
            Assert.That(middleNeighbors, Is.EquivalentTo(new[] { a, c }));

            var edgeNeighbors = CardEffectHelper.GetAdjacentEnemies(enemies, a);
            Assert.That(edgeNeighbors, Is.EquivalentTo(new[] { b }));
        }
        finally
        {
            Object.DestroyImmediate(aObject);
            Object.DestroyImmediate(bObject);
            Object.DestroyImmediate(cObject);
            Object.DestroyImmediate(dObject);
        }
    }

    private PlayerStats CreatePlayerStats()
    {
        playerObject = new GameObject("PlayerStats");
        PlayerStats stats = playerObject.AddComponent<PlayerStats>();
        stats.InitializeStats();
        return stats;
    }

    private void CreateEnemyManager()
    {
        enemyManagerObject = new GameObject("EnemyManager");
        enemyManagerObject.AddComponent<EnemyManager>();
    }

    private void CreateEffectManager()
    {
        effectManagerObject = new GameObject("CardEffectManager");
        effectManagerObject.AddComponent<CardEffectManager>();
    }

    private DeckManager CreateDeckManager()
    {
        deckObject = new GameObject("DeckManager");
        DeckManager deck = deckObject.AddComponent<DeckManager>();
        return deck;
    }

    private Enemy CreateEnemy(int health, int attack)
    {
        enemyObject = new GameObject("Enemy");
        Enemy enemy = enemyObject.AddComponent<Enemy>();
        enemy.maxHealth = health;
        enemy.currentHealth = health;
        enemy.baseAttack = attack;
        return enemy;
    }

    private static CardData CreateCard(string name, int cost)
    {
        CardData card = ScriptableObject.CreateInstance<CardData>();
        card.cardName = name;
        card.cost = cost;
        return card;
    }

    private static void Destroy(GameObject gameObject)
    {
        if (gameObject != null)
            Object.DestroyImmediate(gameObject);
    }
}
