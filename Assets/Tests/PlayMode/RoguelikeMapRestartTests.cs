using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class RoguelikeMapRestartTests
{
    private GameObject arena;
    private GameObject template;
    private Sprite icon;
    private Texture2D texture;

    [UnityTest]
    public IEnumerator RegeneratingHiddenMapInitializesFreshClickableNodesAndKeepsDecorations()
    {
        arena = new GameObject("Restart Map Arena");
        MapGenerator map = arena.AddComponent<MapGenerator>();
        GameObject container = new GameObject("Map Container");
        container.transform.SetParent(arena.transform, false);
        map.mapContainer = container.transform;
        map.totalDepth = 1;
        map.nodesPerLayer = 1;

        GameObject background = new GameObject("Authored Background");
        background.transform.SetParent(container.transform, false);
        GameObject decoration = new GameObject("Authored Decoration");
        decoration.transform.SetParent(container.transform, false);

        texture = new Texture2D(2, 2);
        icon = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        background.AddComponent<SpriteRenderer>().sprite = icon;
        template = new GameObject("Serialized Node Template", typeof(SpriteRenderer), typeof(Node));
        // The authored prefab has no serialized renderer binding; Awake resolves it.
        template.GetComponent<Node>().iconRenderer = null;
        map.nodeTemplate = template;
        map.nodeIcons = new List<MapGenerator.NodeIconConfig>();
        foreach (NodeType type in System.Enum.GetValues(typeof(NodeType)))
            map.nodeIcons.Add(new MapGenerator.NodeIconConfig { type = type, icon = icon });

        map.GenerateMap();
        yield return null;
        Node oldCamp = map.GetNodesByDepth()[0][0];
        Assert.That(oldCamp.iconRenderer.sprite, Is.SameAs(icon));

        container.SetActive(false);
        map.GenerateMap();
        map.ReopenMap();
        yield return null;

        Assert.That(container.activeInHierarchy, Is.True);
        Assert.That(oldCamp == null, Is.True, "Old nodes must not be retained as backgrounds.");
        Assert.That(background != null && decoration != null, Is.True);
        Assert.That(container.GetComponentsInChildren<Node>(true).Length, Is.EqualTo(3));
        foreach (List<Node> layer in map.GetNodesByDepth())
            foreach (Node node in layer)
                Assert.That(node.iconRenderer.sprite, Is.SameAs(icon), node.name);
        Node newCamp = map.GetNodesByDepth()[0][0];
        Assert.That(newCamp.isActive, Is.True);
        Assert.That(newCamp.GetComponent<Collider2D>().enabled, Is.True);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (arena != null) Object.Destroy(arena);
        if (template != null) Object.Destroy(template);
        if (icon != null) Object.Destroy(icon);
        if (texture != null) Object.Destroy(texture);
        yield return null;
    }
}
