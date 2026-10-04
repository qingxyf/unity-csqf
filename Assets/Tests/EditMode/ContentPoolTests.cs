using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ContentPoolTests
{
    [TearDown]
    public void TearDown()
    {
        CollectibleManager.ResetForNewRun();
        EventPool.ResetForNewRun();
    }

    [Test]
    public void EventPoolDrawsEveryEventOnceThenExhaustsAndResets()
    {
        List<EventData> events = new List<EventData>();
        for (int i = 0; i < 3; i++)
        {
            EventData evt = ScriptableObject.CreateInstance<EventData>();
            evt.eventName = "Event " + i;
            events.Add(evt);
        }

        HashSet<EventData> drawn = new HashSet<EventData>();
        for (int i = 0; i < events.Count; i++) drawn.Add(EventPool.Draw(events));
        Assert.That(drawn.Count, Is.EqualTo(events.Count));
        Assert.That(EventPool.Draw(events), Is.Null);

        EventPool.ResetForNewRun();
        Assert.That(EventPool.Draw(events), Is.Not.Null);
        foreach (EventData evt in events) Object.DestroyImmediate(evt);
    }

    [Test]
    public void EventPoolDoesNotResetWhenSourcesAreReorderedOrExpanded()
    {
        EventData first = ScriptableObject.CreateInstance<EventData>();
        EventData second = ScriptableObject.CreateInstance<EventData>();
        EventData third = ScriptableObject.CreateInstance<EventData>();
        EventData fourth = ScriptableObject.CreateInstance<EventData>();
        fourth.eventName = "Event 4";
        List<EventData> events = new List<EventData> { first, second, third };

        EventData drawnFirst = EventPool.Draw(events);
        events.Reverse();
        events.Add(fourth);

        HashSet<EventData> drawn = new HashSet<EventData> { drawnFirst };
        for (int i = 0; i < 3; i++)
            drawn.Add(EventPool.Draw(new List<EventData>(events)));

        Assert.That(drawn.Count, Is.EqualTo(4));
        Assert.That(EventPool.Draw(events), Is.Null);
        Object.DestroyImmediate(first);
        Object.DestroyImmediate(second);
        Object.DestroyImmediate(third);
        Object.DestroyImmediate(fourth);
    }

    [Test]
    public void EventPoolTreatsDifferentInstancesWithTheSameIdAsOneEvent()
    {
        EventData first = CreateEvent("shared-id", "First instance");
        EventData replacement = CreateEvent("shared-id", "Rebuilt instance");
        List<EventData> events = new List<EventData> { first, replacement };

        Assert.That(EventPool.Draw(events), Is.Not.Null);
        Assert.That(EventPool.Draw(events), Is.Null);

        Object.DestroyImmediate(first);
        Object.DestroyImmediate(replacement);
    }

    [Test]
    public void LockedEventBecomesEligibleOnceAfterUnlockAndResetClearsTheUnlock()
    {
        EventData ordinary = CreateEvent("ordinary", "Ordinary");
        EventData locked = CreateEvent("locked", "Locked");
        locked.requiresUnlock = true;
        List<EventData> events = new List<EventData> { ordinary, locked };

        Assert.That(EventPool.Draw(events), Is.SameAs(ordinary));
        Assert.That(EventPool.Draw(events), Is.Null);

        EventPool.UnlockEvent("locked");
        Assert.That(EventPool.IsUnlocked("locked"), Is.True);
        Assert.That(EventPool.Draw(events), Is.SameAs(locked));
        Assert.That(EventPool.Draw(events), Is.Null);

        EventPool.ResetForNewRun();
        Assert.That(EventPool.IsUnlocked("locked"), Is.False);
        Assert.That(EventPool.Draw(events), Is.SameAs(ordinary));
        Assert.That(EventPool.Draw(events), Is.Null);

        Object.DestroyImmediate(ordinary);
        Object.DestroyImmediate(locked);
    }

    [Test]
    public void RegisteringTheSameFallbackAgainDoesNotDestroyIt()
    {
        EventData fallback = ScriptableObject.CreateInstance<EventData>();
        List<EventData> events = new List<EventData> { fallback };

        EventPool.RegisterRuntimeFallback(events);
        EventPool.RegisterRuntimeFallback(events);

        Assert.That(fallback, Is.Not.Null);
    }

    [Test]
    public void RuntimeFallbackIsSharedAcrossManagersAndDrawsWithoutRepeats()
    {
        // These independent list wrappers model two EventManager instances.
        List<EventData> firstManagerPool = EventPool.GetOrCreateRuntimeFallback();
        List<EventData> secondManagerPool = EventPool.GetOrCreateRuntimeFallback();

        Assert.That(secondManagerPool.Count, Is.EqualTo(firstManagerPool.Count));
        Assert.That(secondManagerPool[0], Is.SameAs(firstManagerPool[0]));

        int eligibleCount = firstManagerPool.Count(evt => !evt.requiresUnlock);
        HashSet<EventData> drawn = new HashSet<EventData>();
        for (int i = 0; i < eligibleCount; i++)
        {
            List<EventData> source = i % 2 == 0 ? firstManagerPool : secondManagerPool;
            EventData evt = EventPool.Draw(source);
            Assert.That(evt, Is.Not.Null);
            drawn.Add(evt);
        }

        Assert.That(drawn.Count, Is.EqualTo(eligibleCount));
        Assert.That(EventPool.Draw(firstManagerPool), Is.Null);
    }

    [Test]
    public void CollectibleResourcesImportAsDedicatedCollectibleDataAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:CollectibleData", new[] { "Assets/Resources/Collectibles" });
        Assert.That(guids.Length, Is.EqualTo(15));

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CollectibleData collectible = AssetDatabase.LoadAssetAtPath<CollectibleData>(path);
            Assert.That(collectible, Is.Not.Null, path);
            Assert.That(collectible.collectibleId, Is.Not.Empty, path);
        }

        Assert.That(Resources.LoadAll<CollectibleData>("Collectibles").Length, Is.EqualTo(15));
    }

    [Test]
    public void NewEventResourcesImportAsEventDataAssets()
    {
        string[] existingThreeChoicePaths =
        {
            "Assets/Resources/Events/Event_月下商队.asset",
            "Assets/Resources/Events/Event_低语古井.asset",
            "Assets/Resources/Events/Event_灰烬熔炉.asset",
            "Assets/Resources/Events/Event_坠落观星台.asset",
            "Assets/Resources/Events/Event_缚灵锁链.asset",
            "Assets/Resources/Events/Event_沉没圣所.asset"
        };

        foreach (string path in existingThreeChoicePaths)
        {
            EventData evt = AssetDatabase.LoadAssetAtPath<EventData>(path);
            Assert.That(evt, Is.Not.Null, path);
            Assert.That(evt.choices, Has.Count.EqualTo(3), path);
        }

        EventData[] allEvents = Resources.LoadAll<EventData>("Events");
        Assert.That(allEvents.Length, Is.EqualTo(20));
        foreach (EventData evt in allEvents)
        {
            Assert.That(evt.choices, Is.Not.Null, evt.eventName);
            Assert.That(evt.choices.Count, Is.GreaterThanOrEqualTo(2), evt.eventName);
            Assert.That(evt.choices.TrueForAll(choice => choice != null && !string.IsNullOrEmpty(choice.buttonText)), Is.True, evt.eventName);
        }

        EventData wildCamp = AssetDatabase.LoadAssetAtPath<EventData>("Assets/Resources/Events/Event_野生营地.asset");
        Assert.That(wildCamp.eventId, Is.EqualTo("wild-rice-camp"));
        Assert.That(wildCamp.requiresUnlock, Is.False);
        Assert.That(wildCamp.choices, Has.Count.EqualTo(2));
        Assert.That(wildCamp.choices[1].unlockEventId, Is.EqualTo("rice-owner-reckoning"));
        Assert.That(wildCamp.choices[1].StartsCombat, Is.False);

        EventData reckoning = AssetDatabase.LoadAssetAtPath<EventData>("Assets/Resources/Events/Event_大白饭的讨债人.asset");
        Assert.That(reckoning.eventId, Is.EqualTo("rice-owner-reckoning"));
        Assert.That(reckoning.requiresUnlock, Is.True);
        Assert.That(reckoning.choices, Has.Count.EqualTo(2));
        Assert.That(reckoning.choices[0].surrenderAllGold, Is.True);
        Assert.That(reckoning.choices[0].StartsCombat, Is.False);
        Assert.That(reckoning.choices[1].combatEncounter.enemyResourcePath, Is.EqualTo("Enemies/RiceKeeper"));
        Assert.That(reckoning.choices[1].combatEncounter.maxHealth, Is.EqualTo(240));
    }

    [Test]
    public void CollectiblesAreUniqueAndRandomPoolExhausts()
    {
        HashSet<string> ids = new HashSet<string>();
        foreach (string id in CollectibleCatalog.Ids)
        {
            CollectibleData collectible = CollectibleCatalog.CreateFallback(id);
            Assert.That(collectible.collectibleId, Is.EqualTo(id));
            Assert.That(collectible.collectibleName, Is.Not.Empty);
            Assert.That(collectible.description, Is.Not.Empty);
            Assert.That(CollectibleManager.AddCollectible(collectible), Is.True);
            Assert.That(ids.Add(id), Is.True);
        }

        CollectibleData duplicate = CollectibleCatalog.CreateFallback("life_specimen");
        Assert.That(CollectibleManager.AddCollectible(duplicate), Is.False);
        Object.DestroyImmediate(duplicate);
        Assert.That(CollectibleManager.CreateRandomCollectible(), Is.Null);
    }

    [Test]
    public void AddCollectibleRejectsMissingId()
    {
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleName = "Missing ID fixture";

        Assert.That(CollectibleManager.AddCollectible(collectible), Is.False);
        Assert.That(CollectibleManager.OwnedCollectibles, Is.Empty);

        Object.DestroyImmediate(collectible);
    }

    private static EventData CreateEvent(string id, string name)
    {
        EventData evt = ScriptableObject.CreateInstance<EventData>();
        evt.eventId = id;
        evt.eventName = name;
        return evt;
    }

}
