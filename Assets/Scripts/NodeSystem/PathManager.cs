using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    public MapGenerator mapGenerator;
    
    private Node currentNode;
    
    public void StartJourney()
    {
        // 生成新地图
        mapGenerator.GenerateMap();
        
        // 设置当前节点为营地节点
        currentNode = FindNodeOfType(NodeType.Camp);
        currentNode.OnEnter();
    }
    
    public void MoveToNode(Node targetNode)
    {
        if (currentNode.nextNodes.Contains(targetNode) && targetNode.isActive)
        {
            // 完成当前节点
            currentNode.OnComplete();
            
            // 移动到新节点
            currentNode = targetNode;
            currentNode.OnEnter();
            
            // 检查是否到达Boss节点
            if (currentNode.type == NodeType.Boss)
            {
                Debug.Log("到达Boss节点，准备最终战斗！");
            }
        }
        else
        {
            Debug.LogWarning("无法移动到选定节点！");
        }
    }
    
    private Node FindNodeOfType(NodeType type)
    {
        // 查找特定类型的节点（用于找到起始的营地节点）
        Node[] allNodes = FindObjectsOfType<Node>();
        foreach (Node node in allNodes)
        {
            if (node.type == type)
            {
                return node;
            }
        }
        return null;
    }
    
    // 获取当前可选的下一个节点
    public List<Node> GetAvailableNextNodes()
    {
        List<Node> availableNodes = new List<Node>();
        
        foreach (Node node in currentNode.nextNodes)
        {
            if (node.isActive && !node.isCompleted)
            {
                availableNodes.Add(node);
            }
        }
        
        return availableNodes;
    }
}