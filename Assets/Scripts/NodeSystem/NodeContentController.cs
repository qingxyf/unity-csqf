using UnityEngine;

/// <summary>
/// Shared lifecycle adapter for content shown after entering a map node.
/// Content controllers own their UI/gameplay, while GameManager owns map
/// progression. The completion gate prevents duplicate button clicks from
/// granting rewards or reopening the map more than once.
/// </summary>
public abstract class NodeContentController : MonoBehaviour
{
    private Node boundNode;
    private int boundSessionToken;
    private bool completionStarted;
    private bool completionRequested;

    public Node BoundNode => boundNode;
    public int BoundSessionToken => boundSessionToken;
    public bool CompletionRequested => completionStarted || completionRequested;

    public virtual void BindNode(Node node)
    {
        BindNode(node, GameManager.Instance != null ? GameManager.Instance.CurrentContentSession : 0);
    }

    public virtual void BindNode(Node node, int sessionToken)
    {
        boundNode = node;
        boundSessionToken = sessionToken;
        completionStarted = false;
        completionRequested = false;
        OnNodeBound(node);
    }

    protected virtual void OnNodeBound(Node node)
    {
    }

    protected bool TryBeginCompletion()
    {
        if (completionStarted || completionRequested)
            return false;

        completionStarted = true;
        return true;
    }

    protected bool TryCompleteNode()
    {
        if (completionRequested || GameManager.Instance == null)
            return false;

        if (!completionStarted)
            completionStarted = true;

        if (!GameManager.Instance.TryCompleteCurrentNode(boundNode, boundSessionToken))
        {
            completionStarted = false;
            return false;
        }

        completionRequested = true;
        return true;
    }
}
