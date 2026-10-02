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
    public IEnumerator EveryRouteLayerThroughTheBossStaysVisibleAndClickable()
    {
        arena = new GameObject("Full Route Arena");
        MapGenerator map = arena.AddComponent<MapGenerator>();
        map.totalDepth = 10;
        map.nodesPerLayer = 3;
        map.verticalSpacing = 1.5f;
        map.GenerateMap();
        yield return null;

        GameObject cameraObject = new GameObject("Fixed Route Camera", typeof(Camera));
        cameraObject.transform.SetParent(arena.transform, false);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.aspect = 16f / 10f;

        Node current = map.GetNodesByDepth()[0][0];
        map.MoveToNode(current);
        while (current.nextNodes.Count > 0)
        {
            Node next = current.nextNodes[0];
            Assert.That(next.depth, Is.EqualTo(current.depth + 1));
            Assert.That(next.isActive, Is.True);
            Assert.That(next.position.y, Is.EqualTo(next.depth * map.verticalSpacing));
            Vector3 viewport = camera.WorldToViewportPoint(next.transform.position);
            Assert.That(viewport.x, Is.InRange(0.1f, 0.9f));
            Assert.That(viewport.y, Is.InRange(0.1f, 0.9f), next.name);
            Assert.That(next.GetComponent<Collider2D>().enabled, Is.True);
            Physics2D.SyncTransforms();
            Assert.That(Physics2D.OverlapPoint(next.transform.position).gameObject, Is.SameAs(next.gameObject));

            map.MoveToNode(next);
            Assert.That(next.transform.position.y, Is.EqualTo(0f).Within(0.01f));
            current = next;
        }

        Assert.That(current.type, Is.EqualTo(NodeType.Boss));
        Assert.That(current.depth, Is.EqualTo(11));
        Assert.That(camera.transform.position, Is.EqualTo(new Vector3(0f, 0f, -10f)));
    }

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
