using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class CardContentTests
{
    private readonly List<Object> cleanup = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object item in cleanup)
            if (item != null) Object.DestroyImmediate(item);
        cleanup.Clear();
        DeckManager.Instance = null;
        PlayerStats.Instance = null;
        if (EnemyManager.Instance != null)
            Object.DestroyImmediate(EnemyManager.Instance.gameObject);
        EnemyManager.Instance = null;
        CollectibleManager.ResetForNewRun();
    }

    [Test]
    public void EveryCardHasResolvableEffectAndDedicatedBoundPrefab()
    {
        CardData[] cards = Resources.LoadAll<CardData>("Cards");
        Assert.That(cards, Has.Length.EqualTo(61));

        foreach (CardData card in cards)
        {
            Assert.That(CardEffectCatalog.Resolve(card), Is.Not.Null, card.cardName);
            GameObject prefab = CardResourceUtility.LoadCardPrefab(card);
            Assert.That(prefab, Is.Not.Null, card.cardName);

            CardDisplay display = prefab.GetComponentInChildren<CardDisplay>(true);
            Assert.That(display, Is.Not.Null, card.cardName);
            Assert.That(prefab.GetComponentsInChildren<CardDisplay>(true), Has.Length.EqualTo(1), card.cardName);
            Assert.That(display.cardData, Is.SameAs(card), card.cardName);
            Assert.That(card.cardArt, Is.Not.Null, card.cardName);
        }
    }

    [Test]
    public void NewPrefabsSerializeTheirOwnLabelsInsteadOfTemplateLabels()
    {
        string[] ids = { "晨辉壁垒", "焚烬突袭", "荆棘复苏", "霜潮回环", "幽影收割", "远行补给" };
        foreach (string id in ids)
        {
            CardData card = Resources.Load<CardData>("Cards/" + id);
            GameObject prefab = CardResourceUtility.LoadCardPrefab(card);
            CardDisplay display = prefab.GetComponentInChildren<CardDisplay>(true);
            Assert.That(display.nameText.text, Is.EqualTo(card.cardName), id);
            Assert.That(display.descriptionText.text, Is.EqualTo(card.description), id);
            Assert.That(display.costText.text, Is.EqualTo(card.cost.ToString()), id);
        }
    }

    [Test]
    public void NewCardsResolveEffectsAndPreviewsMatchAuthoredText()
    {
        string[] ids = { "晨辉壁垒", "焚烬突袭", "荆棘复苏", "霜潮回环", "幽影收割", "远行补给" };
        foreach (string id in ids)
        {
            CardData card = Resources.Load<CardData>("Cards/" + id);
            Assert.That(card, Is.Not.Null, id);
            Assert.That(card.effectId, Is.EqualTo(id));
            Assert.That(CardEffectCatalog.Resolve(card), Is.Not.Null, id);
            Assert.That(CardDescriptionFormatter.GetDescription(card), Is.EqualTo(card.description), id);
        }
    }

    [Test]
    public void NewCardEffectsUseTheirAdvertisedSynergies()
    {
        PlayerStats player = CreatePlayer();
        Enemy target = CreateEnemy();

        Execute("晨辉壁垒", player, target);
        Assert.That(player.currentShield, Is.EqualTo(10));
        Execute("晨辉壁垒", player, target);
        Assert.That(player.delayedHealNextTurn, Is.EqualTo(10));

        target.ApplyStatus(StatusType.Burn, 2);
        int beforeFire = target.GetCurrentHealth();
        Execute("焚烬突袭", player, target);
        Assert.That(beforeFire - target.GetCurrentHealth(), Is.EqualTo(30));

        player.currentHealth = 50;
        Execute("荆棘复苏", player, target);
        Assert.That(player.currentHealth, Is.EqualTo(62));
        Assert.That(player.thornCounterAttackDamage, Is.EqualTo(6));

        player.currentMana = 2;
        target.ApplyStatus(StatusType.Frost, 2);
        Execute("霜潮回环", player, target);
        Assert.That(player.currentMana, Is.EqualTo(3));

        target.ApplyStatus(StatusType.Corrosion, 2);
        int beforeShadow = target.GetCurrentHealth();
        Execute("幽影收割", player, target);
        Assert.That(beforeShadow - target.GetCurrentHealth(), Is.GreaterThanOrEqualTo(32));

        player.currentMana = 0;
        Execute("远行补给", player, target);
        Assert.That(player.extraDrawsNextTurn, Is.EqualTo(1));
        Assert.That(player.extraManaNextTurn, Is.EqualTo(1));
    }

    [Test]
    public void UpgradeCreatesRunLocalCloneWithoutMutatingResourceCard()
    {
        CardData source = Resources.Load<CardData>("Cards/晨辉壁垒");
        int authoredCost = source.cost;
        string authoredDescription = source.description;
        GameObject deckObject = new GameObject("Deck");
        cleanup.Add(deckObject);
        DeckManager deck = deckObject.AddComponent<DeckManager>();
        deck.backpack.Add(source);

        Assert.That(deck.TryUpgradeRandomBackpackCard(out CardData upgraded), Is.True);
        Assert.That(upgraded, Is.Not.SameAs(source));
        Assert.That(source.cost, Is.EqualTo(authoredCost));
        Assert.That(source.description, Is.EqualTo(authoredDescription));
    }

    [Test]
    public void HandSlotOwnsTheOnlyCardCaster()
    {
        string source = File.ReadAllText("Assets/Scripts/CardSystem/HandView.cs");
        Assert.That(source, Does.Contain("slot.AddComponent<CardCaster>()"));
        Assert.That(source, Does.Not.Contain("instance.AddComponent<CardCaster>()"));
    }

    [Test]
    public void LegacyPrefabAliasStillResolvesAfterDedicatedPrefabMigration()
    {
        Assert.That(CardResourceUtility.LoadCardPrefab("神圣惩戒"), Is.Not.Null);
    }

    private PlayerStats CreatePlayer()
    {
        GameObject gameObject = new GameObject("Player");
        cleanup.Add(gameObject);
        PlayerStats player = gameObject.AddComponent<PlayerStats>();
        PlayerStats.Instance = player;
        player.InitializeStats();
        return player;
    }

    private Enemy CreateEnemy()
    {
        GameObject gameObject = new GameObject("Enemy");
        cleanup.Add(gameObject);
        Enemy enemy = gameObject.AddComponent<Enemy>();
        enemy.maxHealth = 300;
        enemy.currentHealth = 300;
        return enemy;
    }

    private static void Execute(string cardName, PlayerStats player, Enemy target)
    {
        CardData card = Resources.Load<CardData>("Cards/" + cardName);
        CardEffectCatalog.Resolve(card).Execute(new CardEffectContext
        {
            Card = card,
            Player = player,
            Target = target,
            AllEnemies = new List<Enemy> { target }
        });
    }
}
