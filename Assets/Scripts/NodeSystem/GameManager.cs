using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public MapGenerator mapGenerator;
    public NodeContentManager contentManager;

    private Node currentNode;
    private bool gameStarted = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (mapGenerator == null) mapGenerator = FindObjectOfType<MapGenerator>();
        if (contentManager == null) contentManager = FindObjectOfType<NodeContentManager>();
    }

    void Start()
    {
        if (!gameStarted)
            InitializeGame();
    }

    public void InitializeGame()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("GameManager: MapGenerator 未配置，无法生成地图。");
            return;
        }

        gameStarted = true;
        mapGenerator.GenerateMap();
    }

    /// <summary>
    /// 玩家点击节点时由 Node.OnMouseDown 调用。
    /// 统一处理：地图移动 → 隐藏地图 → 加载节点内容。
    /// </summary>
    public void SelectNode(Node node)
    {
        if (node == null || !node.isActive || node.isCompleted) return;

        currentNode = node;

        // 更新地图状态（标记完成、激活下一层）
        if (mapGenerator != null)
            mapGenerator.MoveToNode(node);

        // 隐藏地图
        if (mapGenerator != null && mapGenerator.mapContainer != null)
            mapGenerator.mapContainer.gameObject.SetActive(false);

        // 播放行走动画
        AnimationSwitcher animSwitcher = FindObjectOfType<AnimationSwitcher>();
        if (animSwitcher != null) animSwitcher.PlayAnimation2();

        InfiniteBackgroundScroller scroller = FindObjectOfType<InfiniteBackgroundScroller>();
        if (scroller != null) scroller.StartScrolling();

        // 加载节点内容
        if (contentManager != null)
            contentManager.LoadNodeContent(node);

        Debug.Log($"进入节点: {node.type} (深度 {node.depth})");
    }

    /// <summary>
    /// 节点内容完成后调用（战斗胜利、事件结束、商店关闭等）。
    /// 清除内容，重新打开地图。
    /// </summary>
    public void CompleteCurrentNode()
    {
        if (currentNode != null && currentNode.type == NodeType.Boss)
        {
            if (contentManager != null)
                contentManager.ClearCurrentContent();

            if (Application.CanStreamedLevelBeLoaded("final"))
                SceneManager.LoadScene("final");
            else
                Debug.Log("Boss 已击败：当前构建设置中未找到 final 场景。");
            return;
        }

        if (contentManager != null)
            contentManager.ClearCurrentContent();

        if (mapGenerator != null)
            mapGenerator.ReopenMap();
        Debug.Log("节点完成，返回地图");
    }
}
