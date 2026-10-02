using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Run-scoped event deck. Every eligible event is drawn once before reshuffling.</summary>
public static class EventPool
{
    private static readonly List<EventData> drawPile = new List<EventData>();
    private static readonly List<EventData> runtimeFallbackEvents = new List<EventData>();
    private static string activeSourceKey;

    public static EventData Draw(IList<EventData> source)
    {
        List<EventData> eligible = source == null ? new List<EventData>() : source.Where(evt => evt != null).Distinct().ToList();
        if (eligible.Count == 0)
            return null;

        string sourceKey = string.Join("|", eligible
            .Select(evt => evt.GetInstanceID().ToString())
            .OrderBy(id => id));
        if (sourceKey != activeSourceKey || drawPile.Count == 0)
        {
            drawPile.Clear();
            drawPile.AddRange(eligible);
            activeSourceKey = sourceKey;
        }

        int index = Random.Range(0, drawPile.Count);
        EventData result = drawPile[index];
        drawPile.RemoveAt(index);
        return result;
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
        drawPile.Clear();
        activeSourceKey = null;
        DisposeRuntimeFallback();
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
