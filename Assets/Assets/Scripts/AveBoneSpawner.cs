using UnityEngine;
using UnityEngine.SceneManagement;  // 确保引入场景管理
using System.Collections;

public class AveBoneSpawner : MonoBehaviour
{
    [SerializeField] private GameObject aveObject;
    [SerializeField] private GameObject boneObject;
    
    private float spawnDelay;
    private bool isActive = false;
    private Vector3 aveOriginalPosition;

    void Start()
    {
        if (aveObject == null || boneObject == null)
        {
            Debug.LogError("Ave或Bone对象未赋值！");
            return;
        }

        // 在游戏开始时初始化一次随机种子
        Random.InitState((int)System.DateTime.Now.Ticks);

        // 创建一个空对象作为Ave和Bone的父对象
        GameObject container = new GameObject("SpawnContainer");
        container.transform.position = transform.position;
        
        // 设置Ave和Bone为container的子对象
        aveObject.transform.SetParent(container.transform, true);
        boneObject.transform.SetParent(container.transform, true);
        
        // 保存container的引用
        transform.SetParent(container.transform);

        SetupAveTrigger();
        aveOriginalPosition = aveObject.transform.localPosition;
        aveObject.SetActive(false);
        boneObject.SetActive(false);

        StartCoroutine(SpawnRoutine());
    }

    private void SetupAveTrigger()
    {
        // 确保Ave有BoxCollider2D
        BoxCollider2D aveCollider = aveObject.GetComponent<BoxCollider2D>();
        if (aveCollider == null)
        {
            aveCollider = aveObject.AddComponent<BoxCollider2D>();
        }
        aveCollider.isTrigger = true;

        // 添加触发器脚本
        PlayerKiller playerKiller = aveObject.GetComponent<PlayerKiller>();
        if (playerKiller == null)
        {
            aveObject.AddComponent<PlayerKiller>();
        }
    }

    private Vector3 GetRandomPosition(float minDistance)
    {
        Camera mainCamera = Camera.main;

        // 获取摄像机的边界
        float camHeight = 2f * mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;

        float camLeft = mainCamera.transform.position.x - camWidth / 2;
        float camRight = mainCamera.transform.position.x + camWidth / 2;
        float camBottom = mainCamera.transform.position.y - camHeight / 2;
        float camTop = mainCamera.transform.position.y + camHeight / 2;

        Vector3 currentPos = transform.position;
        Vector3 randomPosition;
        
        do
        {
            float randomX = Random.Range(camLeft, camRight);
            float randomY = Random.Range(camBottom, camTop);
            randomPosition = new Vector3(randomX, randomY, currentPos.z);
        }
        while (Vector3.Distance(currentPos, randomPosition) < minDistance);

        Debug.Log($"生成新位置: {randomPosition}, 当前位置: {currentPos}");
        return randomPosition;
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            spawnDelay = Random.Range(10f, 15f);
            Debug.Log($"等待 {spawnDelay} 秒后重新生成");
            yield return new WaitForSeconds(spawnDelay);

            // 随机位置生成
            Vector3 newPosition = GetRandomPosition(5f);
            
            // 移动父对象到新位置
            transform.parent.position = newPosition;

            // 显示对象
            boneObject.SetActive(true);
            aveObject.SetActive(true);
            isActive = true;
            Debug.Log("对象已生成");

            // 等待1秒后开始旋转动画
            yield return new WaitForSeconds(1f);

            // 执行Ave的旋转动画
            float rotationDuration = 1f;  // 旋转持续1秒
            float totalRotation = -720f;  // 逆时针旋转2圈 = -720度
            float startTime = Time.time;

            // 获取bone的中心点作为旋转中心
            Vector3 rotationCenter = boneObject.transform.position;
            
            // 计算Ave到旋转中心的初始向量
            Vector3 initialOffset = aveObject.transform.position - rotationCenter;
            float radius = initialOffset.magnitude;  // 旋转半径

            while (Time.time - startTime < rotationDuration)
            {
                float progress = (Time.time - startTime) / rotationDuration;
                float currentAngle = totalRotation * progress * Mathf.Deg2Rad;
                
                // 计算新位置
                float newX = rotationCenter.x + radius * Mathf.Cos(currentAngle);
                float newY = rotationCenter.y + radius * Mathf.Sin(currentAngle);
                
                // 更新Ave的位置
                aveObject.transform.position = new Vector3(newX, newY, aveObject.transform.position.z);
                
                // 更新Ave的旋转，使其始终朝向运动方向
                float angle = Mathf.Atan2(newY - rotationCenter.y, newX - rotationCenter.x) * Mathf.Rad2Deg;
                aveObject.transform.rotation = Quaternion.Euler(0, 0, angle);
                
                yield return null;
            }

            // 重置Ave的旋转和位置
            aveObject.transform.rotation = Quaternion.identity;
            
            // 隐藏对象
            if (isActive)
            {
                aveObject.SetActive(false);
                boneObject.SetActive(false);
                isActive = false;
                Debug.Log("对象已隐藏");
            }

            // 等待5秒
            yield return new WaitForSeconds(5f);
        }
    }

    void OnDestroy()
    {
        isActive = false;
    }

    public void SetInactive()
    {
        isActive = false;
        aveObject.SetActive(false);
        boneObject.SetActive(false);
    }

    public bool IsActive()
    {
        return isActive;
    }
}

// 将触发器逻辑移到单独的脚本中
public class PlayerKiller : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.Log("触发了玩家碰撞");
            StartCoroutine(KillPlayerAndLoadScene(other.gameObject));
        }
    }

    private IEnumerator KillPlayerAndLoadScene(GameObject player)
    {
        // 摧毁玩家
        Destroy(player);
        Debug.Log("玩家已被摧毁");

        // 等待短暂时间
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("准备加载开始场景");
        
        // 加载开始场景
        SceneManager.LoadScene("start");
    }
} 