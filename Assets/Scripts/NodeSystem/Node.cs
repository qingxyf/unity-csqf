using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum NodeType
{
    Camp,
    Event,
    Battle,
    Treasure,
    Shop,
    EliteBattle,
    Boss
}

public class Node : MonoBehaviour
{
    public NodeType type;
    public List<Node> nextNodes = new List<Node>();
    public int depth;
    public Vector2 position;
    public SpriteRenderer iconRenderer;

    public bool isActive = false;
    public bool isCompleted = false;

    public Vector2 colliderOffset = Vector2.zero;

    private MapGenerator mapGenerator;
    private const float ColliderPadding = 0.05f;

    private void Awake()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        if (iconRenderer == null)
        {
            iconRenderer = GetComponent<SpriteRenderer>();
            if (iconRenderer == null) iconRenderer = GetComponentInChildren<SpriteRenderer>();
            if (iconRenderer == null) iconRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();

        RefreshColliderBounds();
        UpdateColliderInteractionState();
    }

    public void SetColliderOffset(Vector2 offset)
    {
        this.colliderOffset = offset;
        RefreshColliderBounds();
    }

    private void RefreshColliderBounds()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Vector2 offset = colliderOffset;
        Vector2 localSize = Vector2.zero;

        if (iconRenderer != null && iconRenderer.sprite != null)
        {
            Bounds bounds = iconRenderer.bounds;
            offset += (Vector2)transform.InverseTransformPoint(bounds.center);
            localSize = new Vector2(
                bounds.size.x / SafeScale(transform.lossyScale.x),
                bounds.size.y / SafeScale(transform.lossyScale.y));
        }

        if (col is CircleCollider2D circleCol)
        {
            circleCol.offset = offset;
            if (localSize != Vector2.zero)
                circleCol.radius = Mathf.Max(localSize.x, localSize.y) * 0.5f + ColliderPadding;
        }
        else if (col is BoxCollider2D boxCol)
        {
            boxCol.offset = offset;
            if (localSize != Vector2.zero)
                boxCol.size = localSize + Vector2.one * (ColliderPadding * 2f);
        }
        else if (col is CapsuleCollider2D capsuleCol)
        {
            capsuleCol.offset = offset;
            if (localSize != Vector2.zero)
                capsuleCol.size = localSize + Vector2.one * (ColliderPadding * 2f);
        }
    }

    private static float SafeScale(float scale)
    {
        return Mathf.Approximately(scale, 0f) ? 1f : Mathf.Abs(scale);
    }

    private void UpdateColliderInteractionState()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = isActive && !isCompleted;
    }

    public virtual void Initialize(NodeType nodeType, int nodeDepth)
    {
        type = nodeType;
        depth = nodeDepth;
        nextNodes.Clear();
        isActive = false;
        isCompleted = false;
        UpdateVisuals();
    }

    public void SetIcon(Sprite sprite)
    {
        if (iconRenderer != null && sprite != null)
        {
            iconRenderer.sprite = sprite;
            RefreshColliderBounds();
        }
    }

    public virtual void UpdateVisuals()
    {
        if (iconRenderer == null) return;

        if (isActive)
            iconRenderer.color = Color.white;
        else if (isCompleted)
            iconRenderer.color = Color.gray;
        else
        {
            Color color = iconRenderer.color;
            color.a = 0.5f;
            iconRenderer.color = color;
        }

        UpdateColliderInteractionState();
    }

    /// <summary>
    /// 点击节点：只负责通知 GameManager，不自行处理逻辑
    /// </summary>
    private void OnMouseDown()
    {
        if (IsPointerOverGameObject()) return;
        if (!isActive || isCompleted) return;

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.SelectNode(this);
        }
    }

    private static bool IsPointerOverGameObject()
    {
        if (EventSystem.current == null) return false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;

            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return true;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
}
