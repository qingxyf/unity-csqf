using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public MapGenerator mapGenerator;
    public PathManager pathManager;
    public NodeContentManager contentManager;
    
    // 当前游戏状态
    private bool gameStarted = false;
    
    private void Awake()
    {
        if (mapGenerator == null) mapGenerator = FindObjectOfType<MapGenerator>();
        if (pathManager == null) pathManager = FindObjectOfType<PathManager>();
        if (contentManager == null) contentManager = FindObjectOfType<NodeContentManager>();
    }

    void Start()
    {
        // 初始化游戏
        InitializeGame();
    }
    
    public void InitializeGame()
    {
        // 开始新的旅程
        pathManager.StartJourney();
        gameStarted = true;
    }
    
    // 玩家选择移动到某个节点
    public void SelectNode(Node targetNode)
    {
        if (!gameStarted) return;
        
        pathManager.MoveToNode(targetNode);
        
        // 加载节点内容
        contentManager.LoadNodeContent(targetNode.type);
    }
    
    // 完成当前节点内容
    public void CompleteCurrentNode()
    {
        Debug.Log("节点完成，正在返回地图...");

        // 1. 清除当前节点的内容（如营地UI、战斗场景等）
        if (contentManager != null)
        {
            contentManager.ClearCurrentContent();
        }

        // 2. 重新激活地图界面
        if (mapGenerator != null && mapGenerator.mapContainer != null)
        {
            mapGenerator.mapContainer.gameObject.SetActive(true);
            
            // 可选：调用 UpdateNodeVisibility 确保显示正确
            mapGenerator.UpdateNodeVisibility();
        }
        else
        {
            Debug.LogError("GameManager: MapGenerator or mapContainer not found!");
        }
    }
}