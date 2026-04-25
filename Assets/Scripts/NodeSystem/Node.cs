using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NodeType
{
    Camp,       // 营地节点
    Event,      // 事件节点
    Battle,     // 战斗节点
    Treasure,   // 宝藏节点
    Shop,       // 商店节点
    EliteBattle,// 劲敌节点
    Boss        // Boss节点
}

public class Node : MonoBehaviour
{
    public NodeType type;
    public List<Node> nextNodes = new List<Node>();
    public int depth; // 节点深度，从0开始
    
    public Vector2 position; // 节点在地图上的位置
    
    // 节点的视觉表现
    public SpriteRenderer iconRenderer;
    
    // 节点激活状态
    public bool isActive = false;
    public bool isCompleted = false;

    // 碰撞体偏移
    public Vector2 colliderOffset = Vector2.zero;
    
    private MapGenerator mapGenerator;
    
    private void Awake()
    {
        // 获取MapGenerator引用
        mapGenerator = FindObjectOfType<MapGenerator>();
        
        // 自动获取或添加 SpriteRenderer
        if (iconRenderer == null)
        {
            iconRenderer = GetComponent<SpriteRenderer>();
            if (iconRenderer == null)
            {
                // 如果自身没有，尝试在子物体里找（以防你的图片是子物体）
                iconRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            
            // 如果还是没有，就给自己加一个
            if (iconRenderer == null)
            {
                iconRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }
        
        // 添加碰撞器（如果没有）
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
        }
        
        // 应用初始偏移
        SetColliderOffset(colliderOffset);
    }
    
    // 设置碰撞体偏移
    public void SetColliderOffset(Vector2 offset)
    {
        this.colliderOffset = offset;
        Collider2D col = GetComponent<Collider2D>();
        
        if (col != null)
        {
            if (col is CircleCollider2D circleCol)
            {
                circleCol.offset = offset;
            }
            else if (col is BoxCollider2D boxCol)
            {
                boxCol.offset = offset;
            }
            else if (col is CapsuleCollider2D capsuleCol)
            {
                capsuleCol.offset = offset;
            }
        }
    }
    
    // 初始化节点
    public virtual void Initialize(NodeType nodeType, int nodeDepth)
    {
        type = nodeType;
        depth = nodeDepth;
        
        // UpdateVisuals 会在 MapGenerator 设置完 Sprite 后再次被调用以调整颜色
        UpdateVisuals();
    }

    // 设置节点的图标图片
    public void SetIcon(Sprite sprite)
    {
        if (iconRenderer != null && sprite != null)
        {
            iconRenderer.sprite = sprite;
        }
    }
    
    // 更新节点视觉效果
    public virtual void UpdateVisuals()
    {
        // 根据节点类型设置不同的图标
        // 这里需要根据实际资源进行设置
        
        // 根据激活状态更新视觉效果
        if (iconRenderer != null)
        {
            // 激活状态显示正常颜色
            if (isActive)
            {
                iconRenderer.color = Color.white;
            }
            // 完成状态显示灰色
            else if (isCompleted)
            {
                iconRenderer.color = Color.gray;
            }
            // 未激活状态显示半透明
            else
            {
                Color color = iconRenderer.color;
                color.a = 0.5f;
                iconRenderer.color = color;
            }
        }
    }
    
    // 玩家进入节点时触发
    public virtual void OnEnter()
    {
        isActive = true;
        // 根据节点类型触发不同的事件
    }
    
    // 玩家完成节点时触发
    public virtual void OnComplete()
    {
        isCompleted = true;
        isActive = false;
        
        // 激活下一个可选节点
        foreach (Node node in nextNodes)
        {
            node.isActive = true;
        }
        
        // 更新视觉效果
        UpdateVisuals();
    }
    
    // 处理鼠标点击
    private void OnMouseDown()
    {
        if (isActive && !isCompleted && mapGenerator != null)
        {
            // 触发移动动画和背景滚动
            AnimationSwitcher animSwitcher = FindObjectOfType<AnimationSwitcher>();
            if (animSwitcher != null)
            {
                animSwitcher.PlayAnimation2();
            }

            InfiniteBackgroundScroller scroller = FindObjectOfType<InfiniteBackgroundScroller>();
            if (scroller != null)
            {
                scroller.StartScrolling();
            }

            // 调用MapGenerator的MoveToNode方法
            mapGenerator.MoveToNode(this);
            
            // 触发节点进入事件
            OnEnter();
            
            // 触发节点完成事件
            OnComplete();
            
            // 暂时关闭地图界面
            if (mapGenerator.mapContainer != null)
            {
                mapGenerator.mapContainer.gameObject.SetActive(false);
            }
            
            // 根据节点类型触发相应的内容
            TriggerNodeContent();
        }
    }
    
    // 触发节点内容
    private void TriggerNodeContent()
    {
        // 查找场景中的NodeContentManager
        NodeContentManager contentManager = FindObjectOfType<NodeContentManager>();
        
        if (contentManager != null)
        {
            contentManager.LoadNodeContent(type);
        }
        else
        {
            Debug.LogError("Node: 未找到 NodeContentManager！");
        }
        
        // 保留原有的Log以便调试
        Debug.Log($"进入节点: {type}");
    }
}