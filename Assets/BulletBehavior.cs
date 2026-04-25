using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    [SerializeField] private float speed = 5f;  // 子弹速度
    
    void Update()
    {
        // 向上移动
        transform.Translate(Vector3.up * speed * Time.deltaTime);
        
        // 如果子弹移出屏幕，销毁它
        if (!IsVisibleFromCamera())
        {
            Destroy(gameObject);
        }
    }
    
    private bool IsVisibleFromCamera()
    {
        Camera mainCamera = Camera.main;
        Vector3 screenPoint = mainCamera.WorldToViewportPoint(transform.position);
        return screenPoint.y < 1.1f;  // 给一点余量再销毁
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
       
        if (other.CompareTag("boss"))
        {
            SimplePlayerController playerController = FindObjectOfType<SimplePlayerController>();
            if (playerController != null)
            {
                playerController.OnBossHit();  // 调用OnBossHit方法来处理boss受击
            }
            Destroy(gameObject);  // 销毁子弹
        }
    }
} 