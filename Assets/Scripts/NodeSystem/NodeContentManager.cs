using UnityEngine;

public class NodeContentManager : MonoBehaviour
{
    [Header("节点内容预制体")]
    public GameObject campContentPrefab;
    public GameObject eventContentPrefab;
    public GameObject battleContentPrefab;
    public GameObject treasureContentPrefab;
    public GameObject shopContentPrefab;
    public GameObject eliteBattleContentPrefab;
    public GameObject bossContentPrefab;

    private GameObject currentContent;

    public bool HasCurrentContent => currentContent != null;

    public void LoadNodeContent(Node node)
    {
        if (node == null) return;
        LoadNodeContent(node.type, node.depth, node, GetCurrentSessionToken());
    }

    public void LoadNodeContent(NodeType nodeType)
    {
        LoadNodeContent(nodeType, -1, null, GetCurrentSessionToken());
    }

    private void LoadNodeContent(NodeType nodeType, int nodeDepth, Node boundNode, int sessionToken)
    {
        EnsureCoreManagers();
        ClearCurrentContent();

        GameObject prefab = GetPrefabForType(nodeType);
        if (prefab == null)
        {
            currentContent = CreateFallbackContent(nodeType, nodeDepth);
            BindContentController(currentContent, boundNode, sessionToken);
            return;
        }

        currentContent = Instantiate(prefab);
        EnsureContentController(currentContent, nodeType, nodeDepth);
        BindContentController(currentContent, boundNode, sessionToken);

        // 营地设置 sorting order
        if (nodeType == NodeType.Camp)
        {
            SpriteRenderer[] renderers = currentContent.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in renderers) sr.sortingOrder = 2;
        }

        // 如果有进场动画组件
        ObjectMover mover = currentContent.GetComponentInChildren<ObjectMover>();
        if (mover != null)
        {
            AnimationSwitcher animSwitcher = FindObjectOfType<AnimationSwitcher>();
            float duration = animSwitcher != null ? animSwitcher.animation2Duration : 5f;
            mover.StartApproach(duration);
        }
    }

    private GameObject GetPrefabForType(NodeType type)
    {
        switch (type)
        {
            case NodeType.Camp: return campContentPrefab;
            case NodeType.Event: return eventContentPrefab;
            case NodeType.Battle: return battleContentPrefab;
            case NodeType.Treasure: return treasureContentPrefab;
            case NodeType.Shop: return shopContentPrefab;
            case NodeType.EliteBattle: return eliteBattleContentPrefab;
            case NodeType.Boss: return bossContentPrefab;
            default: return null;
        }
    }

    private void EnsureCoreManagers()
    {
        DeckManager.EnsureInstance();
        PlayerStats.EnsureInstance();
    }

    private GameObject CreateFallbackContent(NodeType nodeType, int nodeDepth)
    {
        GameObject content = new GameObject($"Fallback_{nodeType}_Content");

        switch (nodeType)
        {
            case NodeType.Event:
                content.AddComponent<EventManager>();
                break;
            case NodeType.Treasure:
                RewardChoiceUI treasure = content.AddComponent<RewardChoiceUI>();
                treasure.title = "宝藏：选择一张卡牌";
                treasure.choiceCount = 3;
                break;
            case NodeType.Shop:
                content.AddComponent<ShopManager>();
                break;
            case NodeType.Battle:
                CombatController battle = content.AddComponent<CombatController>();
                battle.isEliteBattle = false;
                battle.isBossBattle = false;
                battle.showCardRewardOnVictory = true;
                break;
            case NodeType.EliteBattle:
                CombatController elite = content.AddComponent<CombatController>();
                elite.isEliteBattle = true;
                elite.isBossBattle = false;
                elite.showCardRewardOnVictory = true;
                break;
            case NodeType.Boss:
                CombatController boss = content.AddComponent<CombatController>();
                boss.isEliteBattle = false;
                boss.isBossBattle = true;
                boss.showCardRewardOnVictory = false;
                break;
            case NodeType.Camp:
                CampManager camp = content.AddComponent<CampManager>();
                camp.autoBuildUI = true;
                camp.ConfigureForNodeDepth(nodeDepth);
                break;
        }

        return content;
    }

    private void EnsureContentController(GameObject content, NodeType nodeType, int nodeDepth)
    {
        if (content == null) return;

        switch (nodeType)
        {
            case NodeType.Camp:
                CampManager camp = content.GetComponentInChildren<CampManager>(true);
                if (camp == null)
                    camp = content.AddComponent<CampManager>();
                camp.ConfigureForNodeDepth(nodeDepth);
                break;
            case NodeType.Event:
                if (content.GetComponentInChildren<EventManager>(true) == null)
                    content.AddComponent<EventManager>();
                break;
            case NodeType.Treasure:
                if (content.GetComponentInChildren<RewardChoiceUI>(true) == null)
                    content.AddComponent<RewardChoiceUI>();
                break;
            case NodeType.Shop:
                if (content.GetComponentInChildren<ShopManager>(true) == null)
                    content.AddComponent<ShopManager>();
                break;
            case NodeType.Battle:
            case NodeType.EliteBattle:
            case NodeType.Boss:
                CombatController controller = content.GetComponentInChildren<CombatController>(true);
                if (controller == null)
                    controller = content.AddComponent<CombatController>();
                controller.isEliteBattle = nodeType == NodeType.EliteBattle;
                controller.isBossBattle = nodeType == NodeType.Boss;
                controller.showCardRewardOnVictory = nodeType != NodeType.Boss;
                break;
        }
    }

    private void BindContentController(GameObject content, Node node, int sessionToken)
    {
        if (content == null) return;

        NodeContentController controller = content.GetComponentInChildren<NodeContentController>(true);
        if (controller != null)
            controller.BindNode(node, sessionToken);
    }

    private int GetCurrentSessionToken()
    {
        return GameManager.Instance != null ? GameManager.Instance.CurrentContentSession : 0;
    }

    public void ClearCurrentContent()
    {
        if (currentContent != null)
        {
            if (Application.isPlaying)
                Destroy(currentContent);
            else
                DestroyImmediate(currentContent);
            currentContent = null;
        }
    }
}
