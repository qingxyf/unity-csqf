using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeVisualizer : MonoBehaviour
{
    public LineRenderer lineRendererPrefab;
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private List<Material> runtimeMaterials = new List<Material>();
    
    public void VisualizeConnections(List<List<Node>> nodesByDepth)
    {
        ClearLines();

        if (nodesByDepth == null)
            return;
        
        // 为每个节点创建到其下一个节点的连线
        foreach (List<Node> depthNodes in nodesByDepth)
        {
            foreach (Node node in depthNodes)
            {
                foreach (Node nextNode in node.nextNodes)
                {
                    CreateLine(node.transform.position, nextNode.transform.position);
                }
            }
        }
    }
    
    private void CreateLine(Vector3 start, Vector3 end)
    {
        LineRenderer line;
        if (lineRendererPrefab != null)
        {
            line = Instantiate(lineRendererPrefab, transform);
        }
        else
        {
            GameObject lineObject = new GameObject("RuntimeMapLine");
            lineObject.transform.SetParent(transform, false);
            line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.startWidth = 0.035f;
            line.endWidth = 0.035f;
            line.startColor = new Color(0.65f, 0.7f, 0.82f, 0.65f);
            line.endColor = line.startColor;
            line.sortingOrder = 0;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                Material material = new Material(shader);
                line.material = material;
                runtimeMaterials.Add(material);
            }
        }

        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        
        lineRenderers.Add(line);
    }
    
    private void ClearLines()
    {
        foreach (LineRenderer line in lineRenderers)
        {
            Destroy(line.gameObject);
        }
        lineRenderers.Clear();

        foreach (Material material in runtimeMaterials)
            if (material != null) Destroy(material);
        runtimeMaterials.Clear();
    }

    private void OnDestroy()
    {
        foreach (Material material in runtimeMaterials)
            if (material != null) Destroy(material);

        runtimeMaterials.Clear();
    }
}
