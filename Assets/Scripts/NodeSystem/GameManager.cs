using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public MapGenerator mapGenerator;
    public NodeContentManager contentManager;
    public RoguelikeRunController runController;

    private Node currentNode;
    private bool currentNodeCompleted;
    private int currentContentSession;
    private bool gameStarted = false;

    public Node CurrentNode => currentNode;
    public bool CurrentNodeCompleted => currentNodeCompleted;
    public int CurrentContentSession => currentContentSession;
    public RoguelikeRunController RunController => runController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
            return;
        }

        Instance = this;

        if (mapGenerator == null) mapGenerator = FindObjectOfType<MapGenerator>();
        if (contentManager == null) contentManager = FindObjectOfType<NodeContentManager>();

        if (mapGenerator == null)
            mapGenerator = gameObject.AddComponent<MapGenerator>();

        if (contentManager == null)
        {
            GameObject contentObject = new GameObject("RuntimeNodeContentManager");
            contentObject.transform.SetParent(transform, false);
            contentManager = contentObject.AddComponent<NodeContentManager>();
        }

        if (runController == null)
            runController = GetComponent<RoguelikeRunController>();
        if (runController == null)
            runController = gameObject.AddComponent<RoguelikeRunController>();
        if (runController.gameManager == null)
            runController.gameManager = this;
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

        if (runController != null)
        {
            runController.StartNewRun();
            return;
        }

        gameStarted = true;
        mapGenerator.GenerateMap();
    }

    /// <summary>
    /// Rebuilds the route after a terminal result while invalidating callbacks
    /// from the previous map and its node-content session.
    /// </summary>
    public bool ResetMapForNewRun()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("GameManager: MapGenerator 未配置，无法开始新的一局。");
            return false;
        }

        if (contentManager != null)
            contentManager.ClearCurrentContent();

        currentNode = null;
        currentNodeCompleted = false;
        currentContentSession++;
        mapGenerator.GenerateMap();
        mapGenerator.ReopenMap();
        gameStarted = true;
        return true;
    }

    /// <summary>
    /// 玩家点击节点时由 Node.OnMouseDown 调用。
    /// 统一处理：地图移动 → 隐藏地图 → 加载节点内容。
    /// </summary>
    public void SelectNode(Node node)
    {
        if (node == null || !node.isActive || node.isCompleted) return;
        if (currentNode != null && !currentNodeCompleted) return;

        currentNode = node;
        currentNodeCompleted = false;
        currentContentSession++;

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
        TryCompleteCurrentNode(null);
    }

    /// <summary>
    /// Completes the currently selected node exactly once. A controller may
    /// pass its bound node to reject stale callbacks from content that has
    /// already been replaced.
    /// </summary>
    public bool TryCompleteCurrentNode(Node expectedNode)
    {
        return TryCompleteCurrentNode(expectedNode, currentContentSession);
    }

    public bool TryCompleteCurrentNode(Node expectedNode, int expectedSession)
    {
        if (currentNode == null || currentNodeCompleted)
            return false;

        if (expectedNode != null && expectedNode != currentNode)
            return false;

        if (expectedSession != 0 && expectedSession != currentContentSession)
            return false;

        if (expectedNode == null && expectedSession == 0)
            return false;

        currentNodeCompleted = true;

        if (currentNode != null && currentNode.type == NodeType.Boss)
        {
            if (contentManager != null)
                contentManager.ClearCurrentContent();

            if (runController != null)
                return runController.CompleteRun();

            Debug.LogError("GameManager: Boss 已完成，但未配置肉鸽单局控制器。已留在当前地图。");
            if (mapGenerator != null)
            {
                mapGenerator.ReopenMap();
            }
            return true;
        }

        if (contentManager != null)
            contentManager.ClearCurrentContent();

        if (mapGenerator != null)
            mapGenerator.ReopenMap();
        Debug.Log("节点完成，返回地图");
        return true;
    }
}
