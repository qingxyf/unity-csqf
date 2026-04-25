using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeContentManager : MonoBehaviour
{
    // 各种节点内容的预制体
    public GameObject eventContentPrefab;
    public GameObject battleContentPrefab;
    public GameObject treasureContentPrefab;
    public GameObject shopContentPrefab;
    public GameObject eliteBattleContentPrefab;
    public GameObject bossContentPrefab;
    public GameObject campContentPrefab; // 新增：Camp的预制体槽位

    // 当前激活的内容
    private GameObject currentContent;

    // 引用主角的动画切换器
    private AnimationSwitcher playerAnimationSwitcher;
    // 引用背景滚动控制器
    private InfiniteBackgroundScroller backgroundScroller;

    private void Start()
    {
        // 尝试自动查找主角身上的动画切换器
        playerAnimationSwitcher = FindObjectOfType<AnimationSwitcher>();
        // 尝试自动查找背景滚动控制器
        backgroundScroller = FindObjectOfType<InfiniteBackgroundScroller>();
    }

    // 根据节点类型加载相应内容
    public void LoadNodeContent(NodeType nodeType)
    {
        // 如果引用为空，尝试重新查找
        if (playerAnimationSwitcher == null)
            playerAnimationSwitcher = FindObjectOfType<AnimationSwitcher>();
            
        if (backgroundScroller == null)
            backgroundScroller = FindObjectOfType<InfiniteBackgroundScroller>();
            
        // 播放进入节点的动画
        if (playerAnimationSwitcher != null)
        {
            Debug.Log("触发主角行走动画...");
            playerAnimationSwitcher.PlayAnimation2();
        }
        else
        {
            Debug.LogError("NodeContentManager: 未找到 AnimationSwitcher！");
        }
        
        // 启动背景滚动
        if (backgroundScroller != null)
        {
            Debug.Log("触发背景滚动...");
            backgroundScroller.StartScrolling();
        }
        else
        {
            Debug.LogError("NodeContentManager: 未找到 InfiniteBackgroundScroller！");
        }

        // 清除当前内容
        ClearCurrentContent();
        
        // 根据节点类型创建新内容
        switch (nodeType)
        {
            case NodeType.Camp:
                // 营地特殊处理
                HandleCampNode();
                break;
            case NodeType.Event:
                currentContent = Instantiate(eventContentPrefab);
                break;
            case NodeType.Battle:
                currentContent = Instantiate(battleContentPrefab);
                break;
            case NodeType.Treasure:
                currentContent = Instantiate(treasureContentPrefab);
                break;
            case NodeType.Shop:
                currentContent = Instantiate(shopContentPrefab);
                break;
            case NodeType.EliteBattle:
                currentContent = Instantiate(eliteBattleContentPrefab);
                break;
            case NodeType.Boss:
                currentContent = Instantiate(bossContentPrefab);
                break;
        }
        
        // 初始化内容
        if (currentContent != null)
        {
            // 检查内容物体上是否有 ObjectMover 组件（包括子物体）
            ObjectMover mover = currentContent.GetComponentInChildren<ObjectMover>();
            
            if (mover != null)
            {
                Debug.Log($"找到移动脚本 ObjectMover，准备启动进场效果...");
                
                // 获取动画总时长，默认为5秒
                float duration = 5.0f;
                if (playerAnimationSwitcher != null)
                {
                    duration = playerAnimationSwitcher.animation2Duration;
                }
                
                // 启动移动进场效果
                mover.StartApproach(duration);
            }
            else
            {
                Debug.LogWarning($"在 {currentContent.name} 上未找到 ObjectMover 脚本，将不会播放进场移动。");
            }
            
            // 这里可以调用内容对象上的初始化方法
        }
    }
    
    private void HandleCampNode()
    {
        // 营地节点的特殊处理
        Debug.Log("<color=green>抵达营地！</color>");
        
        // 实例化Camp预制体
        if (campContentPrefab != null)
        {
            currentContent = Instantiate(campContentPrefab);

            // 设置 Sorting Order 为 2
            SetSortingOrder(currentContent, 2);
        }
    }

    // 辅助方法：设置物体及其子物体的 Sorting Order
    private void SetSortingOrder(GameObject obj, int order)
    {
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            sr.sortingOrder = order;
        }
    }
    
    public void ClearCurrentContent()
    {
        if (currentContent != null)
        {
            Destroy(currentContent);
            currentContent = null;
        }
    }
}