using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public GameObject nodeTemplate;
    public Transform mapContainer;
    
    public int totalDepth = 10; // 总深度（不包括Boss节点）
    public float horizontalSpacing = 2.0f;
    public float verticalSpacing = 1.5f;
    public float generatedNodeScale = 0.8f; // 重命名变量以强制刷新Inspector缓存
    public Vector2 nodeColliderOffset = new Vector2(0, 3f); // 新增：节点碰撞体偏移，默认向上0.5
    public int visibleDepthAhead = 2; // 当前节点之后可见的深度
    
    private List<List<Node>> nodesByDepth = new List<List<Node>>();
    private Node campNode;
    private Node bossNode;
    private int currentDepth = 0; // 当前深度
    
    public NodeVisualizer nodeVisualizer; // 添加这一行

    [System.Serializable]
    public struct NodeIconConfig
    {
        public NodeType type;
        public Sprite icon;
    }

    public List<NodeIconConfig> nodeIcons; // 在 Inspector 中配置图标映射
    
    public void GenerateMap()
    {
        ClearExistingMap();
        
        // 创建节点列表，按深度组织
        for (int i = 0; i <= totalDepth + 1; i++)
        {
            nodesByDepth.Add(new List<Node>());
        }
        
        // 创建营地节点（深度0）
        campNode = CreateNode(NodeType.Camp, 0, new Vector2(0, 0));
        nodesByDepth[0].Add(campNode);
        
        // 创建第一层节点（两条分支）
        Node leftPath = CreateNode(NodeType.Event, 1, new Vector2(-horizontalSpacing, verticalSpacing));
        Node rightPath = CreateNode(NodeType.Battle, 1, new Vector2(horizontalSpacing, verticalSpacing));
        
        nodesByDepth[1].Add(leftPath);
        nodesByDepth[1].Add(rightPath);
        
        // 连接营地到第一层节点
        ConnectNodes(campNode, leftPath);
        ConnectNodes(campNode, rightPath);
        
        // 生成中间层节点（深度2到totalDepth）
        for (int depth = 1; depth < totalDepth; depth++)
        {
            List<Node> currentDepthNodes = nodesByDepth[depth];
            
            foreach (Node parentNode in currentDepthNodes)
            {
                // 为每个父节点创建1-2个子节点（改为1-2个）
                int childCount = Random.Range(1, 3); // 1到2个子节点
                
                for (int i = 0; i < childCount; i++)
                {
                    // 随机选择节点类型（除了Camp和Boss）
                    NodeType randomType = GetRandomNodeType();
                    
                    // 计算子节点位置
                    float xOffset = (i - (childCount - 1) / 2.0f) * horizontalSpacing;
                    Vector2 childPos = new Vector2(
                        parentNode.position.x + xOffset,
                        parentNode.position.y + verticalSpacing
                    );
                    
                    Node childNode = CreateNode(randomType, depth + 1, childPos);
                    nodesByDepth[depth + 1].Add(childNode);
                    
                    // 连接父节点和子节点
                    ConnectNodes(parentNode, childNode);
                }
            }
        }
        
        // 创建Boss节点（最后一层）
        Vector2 bossPosition = new Vector2(0, (totalDepth + 1) * verticalSpacing);
        bossNode = CreateNode(NodeType.Boss, totalDepth + 1, bossPosition);
        nodesByDepth[totalDepth + 1].Add(bossNode);
        
        // 连接最后一层节点到Boss节点
        foreach (Node node in nodesByDepth[totalDepth])
        {
            ConnectNodes(node, bossNode);
        }
        
        // 初始化节点状态
        campNode.isActive = true;
        campNode.UpdateVisuals(); // <--- 修复：强制刷新营地节点的视觉状态，让它变亮
        currentDepth = 0;
        
        // 更新节点可见性
        UpdateNodeVisibility();
        
        // 在地图生成完成后，调用可视化方法
        if (nodeVisualizer != null)
        {
            nodeVisualizer.VisualizeConnections(nodesByDepth);
        }
    }
    
    // 更新节点可见性
    public void UpdateNodeVisibility()
    {
        // 隐藏所有节点
        for (int depth = 0; depth <= totalDepth + 1; depth++)
        {
            foreach (Node node in nodesByDepth[depth])
            {
                node.gameObject.SetActive(false);
            }
        }
        
        // 显示当前深度和之后visibleDepthAhead层的节点
        int maxVisibleDepth = Mathf.Min(currentDepth + visibleDepthAhead, totalDepth + 1);
        
        for (int depth = currentDepth; depth <= maxVisibleDepth; depth++)
        {
            foreach (Node node in nodesByDepth[depth])
            {
                node.gameObject.SetActive(true);
            }
        }
        
        // 更新可视化连接
        if (nodeVisualizer != null)
        {
            nodeVisualizer.VisualizeConnections(nodesByDepth);
        }
    }
    
    // 移动到下一个节点
    public void MoveToNode(Node targetNode)
    {
        if (targetNode == null || !targetNode.isActive)
            return;
            
        // 更新当前深度
        currentDepth = targetNode.depth;
        
        // 更新节点状态
        foreach (int depth in new[] { currentDepth - 1, currentDepth })
        {
            if (depth < 0) continue;
            
            foreach (Node node in nodesByDepth[depth])
            {
                node.isActive = false;
            }
        }
        
        // 设置下一层节点为活跃
        foreach (Node nextNode in targetNode.nextNodes)
        {
            nextNode.isActive = true;
        }
        
        // 更新节点可见性
        UpdateNodeVisibility();
    }
    
    private NodeType GetRandomNodeType()
    {
        // 随机选择节点类型，排除Camp和Boss
        NodeType[] types = {
            NodeType.Event, 
            NodeType.Battle, 
            NodeType.Treasure, 
            NodeType.Shop, 
            NodeType.EliteBattle
        };
        
        return types[Random.Range(0, types.Length)];
    }
    
    private Node CreateNode(NodeType type, int depth, Vector2 position)
    {
        GameObject nodeObj = Instantiate(nodeTemplate, mapContainer);
        nodeObj.transform.position = new Vector3(position.x, position.y, 0);
        nodeObj.transform.localScale = new Vector3(generatedNodeScale, generatedNodeScale, 1f); // 应用缩放
        
        Node node = nodeObj.GetComponent<Node>();
        if (node == null)
        {
            node = nodeObj.AddComponent<Node>();
        }
        
        node.SetColliderOffset(nodeColliderOffset); // 应用碰撞体偏移
        node.Initialize(type, depth);
        node.position = position;

        // 设置节点图标
        Sprite icon = GetIconForType(type);
        if (icon != null)
        {
            node.SetIcon(icon);
        }

        // 强制设置图标层级顺序为 3
        if (node.iconRenderer != null)
        {
            node.iconRenderer.sortingOrder = 3;
        }
        
        // 根据节点类型命名
        nodeObj.name = $"Node_{type}_{depth}_{nodesByDepth[depth].Count}";
        
        return node;
    }

    private Sprite GetIconForType(NodeType type)
    {
        if (nodeIcons == null) return null;
        
        foreach (var config in nodeIcons)
        {
            if (config.type == type)
            {
                return config.icon;
            }
        }
        return null;
    }
    
    private void ConnectNodes(Node parent, Node child)
    {
        if (!parent.nextNodes.Contains(child))
        {
            parent.nextNodes.Add(child);
        }
    }
    
    private void ClearExistingMap()
    {
        // 清除现有地图
        nodesByDepth.Clear();
        
        // 保存背景图片（假设它是第一个子物体或名称包含"Background"）
        Transform background = null;
        for (int i = 0; i < mapContainer.childCount; i++)
        {
            Transform child = mapContainer.GetChild(i);
            if (child.name.Contains("map") || child.GetComponent<SpriteRenderer>()?.sprite != null)
            {
                background = child;
                background.SetParent(null);
                break;
            }
        }
        
        // 删除所有子物体
        while (mapContainer.childCount > 0)
        {
            DestroyImmediate(mapContainer.GetChild(0).gameObject);
        }
        
        // 恢复背景图片
        if (background != null)
        {
            background.SetParent(mapContainer);
        }
    }
    
    // 重新打开地图界面
    public void ReopenMap()
    {
        if (mapContainer != null)
        {
            mapContainer.gameObject.SetActive(true);
            
            // 更新节点可见性
            UpdateNodeVisibility();
        }
    }
}