using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeVisualizer : MonoBehaviour
{
    public LineRenderer lineRendererPrefab;
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    
    public void VisualizeConnections(List<List<Node>> nodesByDepth)
    {
        ClearLines();
        
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
        LineRenderer line = Instantiate(lineRendererPrefab, transform);
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
    }
}