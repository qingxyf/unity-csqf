using System.Collections.Generic;
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
    public void EventPoolDrawsEveryEventBeforeRepeatingAndResets()
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

        EventPool.ResetForNewRun();
        Assert.That(EventPool.Draw(events), Is.Not.Null);
        foreach (EventData evt in events) Object.DestroyImmediate(evt);
    }

    [Test]
    public void EventPoolDoesNotResetWhenTheSameSourceIsReordered()
    {
        EventData first = ScriptableObject.CreateInstance<EventData>();
        EventData second = ScriptableObject.CreateInstance<EventData>();
        EventData third = ScriptableObject.CreateInstance<EventData>();
        List<EventData> events = new List<EventData> { first, second, third };

        EventData drawnFirst = EventPool.Draw(events);
        events.Reverse();
        EventData drawnSecond = EventPool.Draw(events);
        EventData drawnThird = EventPool.Draw(events);

        Assert.That(new HashSet<EventData> { drawnFirst, drawnSecond, drawnThird }.Count, Is.EqualTo(3));
        Object.DestroyImmediate(first);
        Object.DestroyImmediate(second);
        Object.DestroyImmediate(third);
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

        HashSet<EventData> drawn = new HashSet<EventData>();
        for (int i = 0; i < firstManagerPool.Count; i++)
        {
            List<EventData> source = i % 2 == 0 ? firstManagerPool : secondManagerPool;
            drawn.Add(EventPool.Draw(source));
        }

        Assert.That(drawn.Count, Is.EqualTo(firstManagerPool.Count));
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
        string[] paths =
        {
            "Assets/Resources/Events/Event_月下商队.asset",
            "Assets/Resources/Events/Event_低语古井.asset",
            "Assets/Resources/Events/Event_灰烬熔炉.asset",
            "Assets/Resources/Events/Event_坠落观星台.asset",
            "Assets/Resources/Events/Event_缚灵锁链.asset",
            "Assets/Resources/Events/Event_沉没圣所.asset"
        };

        foreach (string path in paths)
        {
            EventData evt = AssetDatabase.LoadAssetAtPath<EventData>(path);
            Assert.That(evt, Is.Not.Null, path);
            Assert.That(evt.choices, Has.Count.EqualTo(3), path);
        }
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
}
