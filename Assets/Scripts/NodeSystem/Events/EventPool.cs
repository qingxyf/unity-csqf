using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Run-scoped event pool. An event can be encountered only once per run.</summary>
public static class EventPool
{
    private static readonly List<EventData> runtimeFallbackEvents = new List<EventData>();
    private static readonly HashSet<string> visitedEventKeys = new HashSet<string>();
    private static readonly HashSet<string> unlockedEventIds = new HashSet<string>();

    public static EventData Draw(IList<EventData> source)
    {
        List<EventData> eligible = source == null
            ? new List<EventData>()
            : source.Where(IsEligibleAndUnvisited).ToList();
        if (eligible.Count == 0)
            return null;

        EventData result = eligible[Random.Range(0, eligible.Count)];
        visitedEventKeys.Add(GetEventKey(result));
        return result;
    }

    public static void UnlockEvent(string eventId)
    {
        if (!string.IsNullOrWhiteSpace(eventId))
            unlockedEventIds.Add(eventId.Trim());
    }

    public static bool IsUnlocked(string eventId)
    {
        return !string.IsNullOrWhiteSpace(eventId) && unlockedEventIds.Contains(eventId.Trim());
    }

    public static void RegisterRuntimeFallback(IEnumerable<EventData> events)
    {
        List<EventData> fallback = events == null
            ? new List<EventData>()
            : events.Where(evt => evt != null).Distinct().ToList();

        if (runtimeFallbackEvents.Count == fallback.Count &&
            runtimeFallbackEvents.All(fallback.Contains))
            return;

        DisposeRuntimeFallback();
        runtimeFallbackEvents.AddRange(fallback);
    }

    /// <summary>
    /// Returns a fresh list wrapper around the current run's fallback objects.
    /// Different EventManager instances therefore share one event deck source.
    /// </summary>
    public static List<EventData> GetOrCreateRuntimeFallback()
    {
        runtimeFallbackEvents.RemoveAll(evt => evt == null);
        if (runtimeFallbackEvents.Count == 0)
            runtimeFallbackEvents.AddRange(EventLibrary.CreateDefaultEvents().Where(evt => evt != null));

        return new List<EventData>(runtimeFallbackEvents);
    }

    public static void ResetForNewRun()
    {
        visitedEventKeys.Clear();
        unlockedEventIds.Clear();
        DisposeRuntimeFallback();
    }

    private static bool IsEligibleAndUnvisited(EventData evt)
    {
        if (evt == null || visitedEventKeys.Contains(GetEventKey(evt)))
            return false;

        return !evt.requiresUnlock || IsUnlocked(evt.eventId);
    }

    private static string GetEventKey(EventData evt)
    {
        if (!string.IsNullOrWhiteSpace(evt.eventId))
            return "id:" + evt.eventId.Trim();
        if (!string.IsNullOrWhiteSpace(evt.eventName))
            return "name:" + evt.eventName.Trim();
        return "instance:" + evt.GetInstanceID();
    }

    private static void DisposeRuntimeFallback()
    {
        foreach (EventData evt in runtimeFallbackEvents)
        {
            if (evt == null) continue;
            if (Application.isPlaying) Object.Destroy(evt);
            else Object.DestroyImmediate(evt);
        }

        runtimeFallbackEvents.Clear();
    }
}
