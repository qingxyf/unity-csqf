using UnityEngine;
using System.Collections;

public class ObjectMover : MonoBehaviour
{
    [Header("移动设置")]
    public float startX = 15f;
    public float endX = 9f;
    public float moveDuration = 1.0f; // 移动过程持续1秒
    
    private Coroutine moveCoroutine;

    // 开始移动流程
    // totalWaitTime: 总等待时间（通常对应主角走路动画的总时长，如5秒）
    public void StartApproach(float totalWaitTime)
    {
        // 先把物体放到起始位置
        Vector3 pos = transform.position;
        pos.x = startX;
        transform.position = pos;
        
        // 确保物体是激活的
        gameObject.SetActive(true);
        
        // 停止之前的协程（如果有）
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        
        // 计算需要等待的时间：总时长 - 移动时长
        // 例如：5秒总时长 - 1秒移动 = 等待4秒后再开始动
        float waitDelay = Mathf.Max(0, totalWaitTime - moveDuration);
        
        Debug.Log($"物体 {gameObject.name} 将在 {waitDelay} 秒后开始进入视野...");
        moveCoroutine = StartCoroutine(MoveRoutine(waitDelay));
    }

    private IEnumerator MoveRoutine(float delay)
    {
        // 1. 等待
        yield return new WaitForSeconds(delay);
        
        Debug.Log($"物体 {gameObject.name} 开始移动！");
        
        // 2. 开始移动
        float elapsed = 0f;
        Vector3 startPos = transform.position; // 应该是 startX
        startPos.x = startX;
        
        Vector3 endPos = transform.position;
        endPos.x = endX;
        
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            // 使用 SmoothStep 让移动更平滑，模拟减速进场效果
            t = Mathf.SmoothStep(0f, 1f, t);
            
            Vector3 currentPos = transform.position;
            currentPos.x = Mathf.Lerp(startX, endX, t);
            transform.position = currentPos;
            
            yield return null;
        }
        
        // 确保最后位置精确
        Vector3 finalPos = transform.position;
        finalPos.x = endX;
        transform.position = finalPos;
    }
}
