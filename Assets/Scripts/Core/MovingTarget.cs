using UnityEngine;

public class MovingTarget : MonoBehaviour, ICollectible
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float directionChangeInterval = 2f;

    [Header("行为")]
    [SerializeField] private bool fleesFromPlayer = false;
    [SerializeField] private float fleeRange = 3f;
    [SerializeField] private float fleeSpeedMultiplier = 2f;

    private Vector2 moveDirection;
    private float dirTimer;
    private Transform playerTransform;
    private bool isActive = true;

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        PickNewDirection();
    }

    void Update()
    {
        if (!isActive) return;

        dirTimer -= Time.deltaTime;
        if (dirTimer <= 0)
            PickNewDirection();

        // 检查是否需要逃跑
        float speed = moveSpeed;
        if (fleesFromPlayer && playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist < fleeRange)
            {
                // 远离玩家
                moveDirection = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                speed = moveSpeed * fleeSpeedMultiplier;
            }
        }

        transform.Translate(moveDirection * speed * Time.deltaTime);
        ClampAndBounce();
    }

    private void PickNewDirection()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        dirTimer = Random.Range(directionChangeInterval * 0.5f, directionChangeInterval * 1.5f);
    }

    private void ClampAndBounce()
    {
        if (Camera.main == null) return;
        Vector3 vp = Camera.main.WorldToViewportPoint(transform.position);

        if (vp.x < 0.03f || vp.x > 0.97f) moveDirection.x = -moveDirection.x;
        if (vp.y < 0.03f || vp.y > 0.97f) moveDirection.y = -moveDirection.y;

        vp.x = Mathf.Clamp(vp.x, 0.03f, 0.97f);
        vp.y = Mathf.Clamp(vp.y, 0.03f, 0.97f);
        transform.position = Camera.main.ViewportToWorldPoint(vp);
    }

    public void OnCollected()
    {
        // 重新定位到屏幕随机位置
        transform.position = PlayerController.GetRandomScreenPosition(transform.position, 4f);
        PickNewDirection();
    }

    public bool IsActive() => isActive;
}
