using UnityEngine;
using TMPro;
using System.Collections;

public class SpeedUP : MonoBehaviour
{
    private Camera mainCamera;
    private float spawnDelay;
    private bool isActive = false;
    private GameObject visualObject;

    void Start()
    {
        mainCamera = Camera.main;
        
        // 检查是否有子物体，如果没有就创建一个
        if (transform.childCount == 0)
        {
            // 创建一个新的子物体
            GameObject child = new GameObject("VisualObject");
            child.transform.parent = transform;
            child.transform.localPosition = Vector3.zero;
            child.transform.localScale = new Vector3(1f, 1f, 1f);
            
            // 将当前物体的 SpriteRenderer 移动到子物体
            SpriteRenderer parentSprite = GetComponent<SpriteRenderer>();
            if (parentSprite != null)
            {
                SpriteRenderer childSprite = child.AddComponent<SpriteRenderer>();
                childSprite.sprite = parentSprite.sprite;
                childSprite.color = parentSprite.color;
                Destroy(parentSprite);
            }
            
            visualObject = child;
        }
        else
        {
            visualObject = transform.GetChild(0).gameObject;
        }
        
        visualObject.SetActive(false);
        StartCoroutine(SpawnRoutine());
    }

    private Vector3 GetRandomPosition(Vector3 currentPosition, float minDistance)
    {
        Camera mainCamera = Camera.main;

        // 获取摄像机的边界
        float camHeight = 2f * mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;

        float camLeft = mainCamera.transform.position.x - camWidth / 2;
        float camRight = mainCamera.transform.position.x + camWidth / 2;
        float camBottom = mainCamera.transform.position.y - camHeight / 2;
        float camTop = mainCamera.transform.position.y + camHeight / 2;

        Vector3 randomPosition;
        do
        {
            float randomX = Random.Range(camLeft, camRight);
            float randomY = Random.Range(camBottom, camTop);
            randomPosition = new Vector3(randomX, randomY, currentPosition.z);
        }
        while (Vector3.Distance(currentPosition, randomPosition) < minDistance); // 确保新位置与当前距离大于最小距离

        return randomPosition;
    }

    private IEnumerator SpawnRoutine()
    {   
        while (true)
        {
            spawnDelay = Random.Range(10f, 15f);
            Debug.Log($"Waiting for {spawnDelay} seconds before spawn");
            
            yield return new WaitForSeconds(spawnDelay);
            
            Vector3 worldPosition = GetRandomPosition(transform.position, 5f);
            transform.position = worldPosition;
            
            visualObject.SetActive(true);
            isActive = true;
            Debug.Log("Object spawned");
            
            yield return new WaitForSeconds(5f);
            
            if (isActive)
            {
                visualObject.SetActive(false);
                isActive = false;
                Debug.Log("Object hidden");
            }
            yield return new WaitForSeconds(5f);
        }
    }

    // 如果物体被摧毁，确保isActive设置为false
    void OnDestroy()
    {
        isActive = false;
    }

    public void SetInactive()
    {
        isActive = false;
        visualObject.SetActive(false);
    }
    public bool IsActive()
    {
        return isActive;
    }
}

