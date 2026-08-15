using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public GameObject nodeTemplate;
    public Transform mapContainer;

    [Header("地图结构")]
    public int totalDepth = 10;
    public int nodesPerLayer = 3;         // 每层固定节点数
    public float horizontalSpacing = 2.0f;
    public float verticalSpacing = 1.5f;
    public float generatedNodeScale = 0.8f;
    public Vector2 nodeColliderOffset = Vector2.zero;
    public int visibleDepthAhead = 2;

    [Header("节点类型权重")]
    public float battleWeight = 35f;
    public float eventWeight = 25f;
    public float shopWeight = 10f;
    public float treasureWeight = 15f;
    public float eliteBattleWeight = 10f;
    public float campWeight = 5f;

    [Header("可视化")]
    public NodeVisualizer nodeVisualizer;

    [System.Serializable]
    public struct NodeIconConfig
    {
        public NodeType type;
        public Sprite icon;
    }
    public List<NodeIconConfig> nodeIcons;

    private List<List<Node>> nodesByDepth = new List<List<Node>>();
    private int currentDepth = 0;

    public List<List<Node>> GetNodesByDepth() => nodesByDepth;

    public void GenerateMap()
    {
        ClearExistingMap();

        for (int i = 0; i <= totalDepth + 1; i++)
            nodesByDepth.Add(new List<Node>());

        // === 深度0：营地（起点） ===
        Node campNode = CreateNode(NodeType.Camp, 0, new Vector2(0, 0));
        nodesByDepth[0].Add(campNode);

        // === 深度1 ~ totalDepth：中间层 ===
        for (int depth = 1; depth <= totalDepth; depth++)
        {
            int nodeCount = nodesPerLayer;

            // 特殊层：每3层保证有一个商店，每5层有精英战
            bool forceShop = (depth % 3 == 0);
            bool forceElite = (depth % 5 == 0);
            // Boss前一层强制营地休息
            bool forceCamp = (depth == totalDepth);

            for (int i = 0; i < nodeCount; i++)
            {
                NodeType type;

                if (forceCamp && i == nodeCount / 2)
                    type = NodeType.Camp;
                else if (forceShop && i == 0)
                    type = NodeType.Shop;
                else if (forceElite && i == nodeCount - 1)
                    type = NodeType.EliteBattle;
                else
                    type = GetWeightedRandomType();

                float xOffset = (i - (nodeCount - 1) / 2f) * horizontalSpacing;
                // 加一点随机偏移，让地图不那么死板
                xOffset += Random.Range(-0.3f, 0.3f);

                Vector2 pos = new Vector2(xOffset, depth * verticalSpacing);
                Node node = CreateNode(type, depth, pos);
                nodesByDepth[depth].Add(node);
            }

            // 连接上一层到当前层
            ConnectLayers(nodesByDepth[depth - 1], nodesByDepth[depth]);
        }

        // === 最终层：Boss ===
        Vector2 bossPos = new Vector2(0, (totalDepth + 1) * verticalSpacing);
        Node bossNode = CreateNode(NodeType.Boss, totalDepth + 1, bossPos);
        nodesByDepth[totalDepth + 1].Add(bossNode);

        // 所有最后一层节点连接到Boss
        foreach (Node node in nodesByDepth[totalDepth])
            ConnectNodes(node, bossNode);

        // 初始状态
        campNode.isActive = true;
        campNode.UpdateVisuals();
        currentDepth = 0;

        UpdateNodeVisibility();

        if (nodeVisualizer != null)
            nodeVisualizer.VisualizeConnections(nodesByDepth);
    }

    /// <summary>
    /// 连接两层节点：保证每个父节点至少连1个子节点，每个子节点至少被1个父节点连接。
    /// 避免出现孤立节点或断路。
    /// </summary>
    private void ConnectLayers(List<Node> parents, List<Node> children)
    {
        if (parents.Count == 0 || children.Count == 0) return;

        // 第一步：每个父节点至少连一个最近的子节点
        foreach (Node parent in parents)
        {
            Node closest = null;
            float minDist = float.MaxValue;

            foreach (Node child in children)
            {
                float dist = Mathf.Abs(parent.position.x - child.position.x);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = child;
                }
            }

            if (closest != null)
                ConnectNodes(parent, closest);
        }

        // 第二步：检查是否有子节点没有被任何父节点连接
        foreach (Node child in children)
        {
            bool hasParent = false;
            foreach (Node parent in parents)
            {
                if (parent.nextNodes.Contains(child))
                {
                    hasParent = true;
                    break;
                }
            }

            if (!hasParent)
            {
                // 找最近的父节点连上
                Node closest = null;
                float minDist = float.MaxValue;
                foreach (Node parent in parents)
                {
                    float dist = Mathf.Abs(parent.position.x - child.position.x);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = parent;
                    }
                }
                if (closest != null)
                    ConnectNodes(closest, child);
            }
        }

        // 第三步：额外随机连接（让路线更丰富，约30%概率额外连一条）
        foreach (Node parent in parents)
        {
            foreach (Node child in children)
            {
                if (!parent.nextNodes.Contains(child))
                {
                    float dist = Mathf.Abs(parent.position.x - child.position.x);
                    if (dist <= horizontalSpacing * 1.2f && Random.value < 0.3f)
                    {
                        ConnectNodes(parent, child);
                    }
                }
            }
        }
    }

    private NodeType GetWeightedRandomType()
    {
        float total = battleWeight + eventWeight + shopWeight + treasureWeight + eliteBattleWeight + campWeight;
        float roll = Random.Range(0, total);

        if (roll < battleWeight) return NodeType.Battle;
        roll -= battleWeight;
        if (roll < eventWeight) return NodeType.Event;
        roll -= eventWeight;
        if (roll < treasureWeight) return NodeType.Treasure;
        roll -= treasureWeight;
        if (roll < shopWeight) return NodeType.Shop;
        roll -= shopWeight;
        if (roll < eliteBattleWeight) return NodeType.EliteBattle;
        return NodeType.Camp;
    }

    // === 节点移动 ===

    public void MoveToNode(Node targetNode)
    {
        if (targetNode == null || !targetNode.isActive) return;

        currentDepth = targetNode.depth;

        // 当前层和之前的层全部设为非活跃
        for (int d = 0; d <= currentDepth; d++)
        {
            foreach (Node node in nodesByDepth[d])
            {
                node.isActive = false;
                node.UpdateVisuals();
            }
        }

        // 目标节点标记为已完成（视觉上变灰）
        targetNode.isCompleted = true;
        targetNode.UpdateVisuals();

        // 下一层节点中，只有被目标节点连接的才激活
        foreach (Node nextNode in targetNode.nextNodes)
        {
            nextNode.isActive = true;
            nextNode.UpdateVisuals();
        }

        UpdateNodeVisibility();
    }

    // === 可见性管理 ===

    public void UpdateNodeVisibility()
    {
        for (int depth = 0; depth <= totalDepth + 1; depth++)
        {
            foreach (Node node in nodesByDepth[depth])
                node.gameObject.SetActive(false);
        }

        int maxVisible = Mathf.Min(currentDepth + visibleDepthAhead, totalDepth + 1);
        for (int depth = Mathf.Max(0, currentDepth - 1); depth <= maxVisible; depth++)
        {
            foreach (Node node in nodesByDepth[depth])
                node.gameObject.SetActive(true);
        }

        if (nodeVisualizer != null)
            nodeVisualizer.VisualizeConnections(nodesByDepth);
    }

    public void ReopenMap()
    {
        if (mapContainer != null)
        {
            mapContainer.gameObject.SetActive(true);
            UpdateNodeVisibility();
        }
    }

    // === 工具方法 ===

    private Node CreateNode(NodeType type, int depth, Vector2 position)
    {
        GameObject nodeObj = Instantiate(nodeTemplate, mapContainer);
        nodeObj.transform.position = new Vector3(position.x, position.y, 0);
        nodeObj.transform.localScale = new Vector3(generatedNodeScale, generatedNodeScale, 1f);

        Node node = nodeObj.GetComponent<Node>();
        if (node == null) node = nodeObj.AddComponent<Node>();

        node.SetColliderOffset(nodeColliderOffset);
        node.Initialize(type, depth);
        node.position = position;

        Sprite icon = GetIconForType(type);
        if (icon != null) node.SetIcon(icon);
        if (node.iconRenderer != null) node.iconRenderer.sortingOrder = 3;

        nodeObj.name = $"Node_{type}_{depth}_{nodesByDepth[depth].Count}";
        return node;
    }

    private Sprite GetIconForType(NodeType type)
    {
        if (nodeIcons == null) return null;
        foreach (var config in nodeIcons)
        {
            if (config.type == type) return config.icon;
        }
        return null;
    }

    private void ConnectNodes(Node parent, Node child)
    {
        if (!parent.nextNodes.Contains(child))
            parent.nextNodes.Add(child);
    }

    private void ClearExistingMap()
    {
        nodesByDepth.Clear();

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

        while (mapContainer.childCount > 0)
            DestroyImmediate(mapContainer.GetChild(0).gameObject);

        if (background != null)
            background.SetParent(mapContainer);
    }
}
