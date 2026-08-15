using UnityEngine;

public class BossBulletBehavior : MonoBehaviour
{
    [SerializeField] private float speed = 5f;  // 子弹速度
    private Vector2 direction;  // 子弹移动方向
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 获取子弹的旋转角度
        float angle = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        // 计算移动方向
        direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    void Update()
    {
        // 按方向移动子弹
        transform.Translate(direction * speed * Time.deltaTime);
        
        // 检查子弹是否超出屏幕太远
        if (IsFarFromScreen())
        {
            Destroy(gameObject);
        }
    }

    private bool IsFarFromScreen()
    {
        Camera mainCamera = Camera.main;
        Vector3 screenPoint = mainCamera.WorldToViewportPoint(transform.position);
        return screenPoint.x < -1f || screenPoint.x > 2f || 
               screenPoint.y < -1f || screenPoint.y > 2f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SimplePlayerController player = other.GetComponent<SimplePlayerController>();
            if (player != null)
            {
                player.TakeDamage(1);
            }
            Destroy(gameObject);
        }
    }

    // 设置子弹方向的方法（如果需要从外部设置）
    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
    }
}
