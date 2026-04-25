using UnityEngine;
using System.Collections;

public class BulletSpawner : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;  // 子弹预制体
    [SerializeField] private float spawnInterval = 0.5f;  // 生成间隔
    
    void Start()
    {
        // 检查是否已设置子弹预制体
        if (bulletPrefab == null)
        {
            Debug.LogError("未设置子弹预制体！");
            return;
        }
        
        // 开始生成子弹的协程
        StartCoroutine(SpawnBullets());
    }
    
    private IEnumerator SpawnBullets()
    {
        while (true)
        {
            // 获取摄像机视口的顶部中心位置
            Camera mainCamera = Camera.main;
            Vector3 spawnPosition = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 1.1f, 0));
            spawnPosition.z = 0;  // 确保z坐标为0
            
            // 生成子弹
            GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
            
            // 等待指定时间
            yield return new WaitForSeconds(spawnInterval);
        }
    }
} 