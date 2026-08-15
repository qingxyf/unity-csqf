using System.IO;
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MapInputAlignmentTests
{
    [Test]
    public void ForthSceneUsesCenteredNodeColliderOffset()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/forth.unity", OpenSceneMode.Single);

        MonoBehaviour generator = FindMapGeneratorInOpenScene();

        Assert.That(generator, Is.Not.Null);
        Assert.That(GetNodeColliderOffset(generator), Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void MapGeneratorDefaultUsesCenteredNodeColliderOffset()
    {
        GameObject host = new GameObject("MapGenerator test host");
        try
        {
            Type mapGeneratorType = Type.GetType("MapGenerator, Assembly-CSharp");
            Assert.That(mapGeneratorType, Is.Not.Null);

            Component generator = host.AddComponent(mapGeneratorType);

            Assert.That(GetNodeColliderOffset(generator), Is.EqualTo(Vector2.zero));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void NodeTemplateColliderStartsCenteredAndSmallEnoughForMapSpacing()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/NodeTemplate.prefab");
        CircleCollider2D collider = prefab.GetComponent<CircleCollider2D>();

        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.offset, Is.EqualTo(Vector2.zero));
        Assert.That(collider.radius, Is.LessThanOrEqualTo(0.75f));
    }

    [Test]
    public void NodeClickHandlerGuardsAgainstUiRaycasts()
    {
        string source = File.ReadAllText("Assets/Scripts/NodeSystem/Node.cs");

        Assert.That(source, Does.Contain("UnityEngine.EventSystems"));
        Assert.That(source, Does.Contain("IsPointerOverGameObject"));
    }

    [Test]
    public void BackpackCardsUseDedicatedRaycastSlots()
    {
        string source = File.ReadAllText("Assets/Scripts/UI/BackpackDisplay.cs");

        Assert.That(source, Does.Contain("CreateCardSlot"));
        Assert.That(source, Does.Contain("DisableRaycastTargets"));
    }

    private static MonoBehaviour FindMapGeneratorInOpenScene()
    {
        return UnityEngine.Object.FindObjectsOfType<MonoBehaviour>()
            .FirstOrDefault(component => component.GetType().Name == "MapGenerator");
    }

    private static Vector2 GetNodeColliderOffset(Component generator)
    {
        FieldInfo field = generator.GetType().GetField("nodeColliderOffset");
        Assert.That(field, Is.Not.Null);
        return (Vector2)field.GetValue(generator);
    }
}
