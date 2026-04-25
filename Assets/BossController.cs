using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("基础设置")]
    [SerializeField] private GameObject bulletPrefab;  // boss子弹预制体
    [SerializeField] private float attackInterval = 3f;  // 攻击间隔
    private float originalY;  // 记录初始Y位置
    
    [Header("扇形攻击设置")]
    [SerializeField] private int fanBulletCount = 15;  // 扇形子弹数量
    [SerializeField] private float fanAngleRange = 180f;  // 扇形角度范围
    [SerializeField] private float bulletSpeed = 5f;  // 子弹速度

    [Header("冲刺攻击设置")]
    [SerializeField] private float dashSpeed = 1f;  // 冲刺速度
    [SerializeField] private float returnSpeed = 8f;  // 返回速度

    [Header("螺旋攻击设置")]
    [SerializeField] private int spiralBulletCount = 30;  // 螺旋子弹数量
    [SerializeField] private float spiralRadius = 2f;  // 螺旋半径
    [SerializeField] private float spiralRotateSpeed = 180f;  // 螺旋旋转速度

    private bool isAttacking = false;
    private Transform playerTransform;

    void Start()
    {
        originalY = transform.position.y;
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            if (!isAttacking)
            {
                int attackType = Random.Range(0, 3);
                switch (attackType)
                {
                    case 0:
                        StartCoroutine(FanAttack());
                        break;
                    case 1:
                        StartCoroutine(DashAttack());
                        break;
                    case 2:
                        StartCoroutine(SpiralAttack());
                        break;
                }
            }
            yield return new WaitForSeconds(attackInterval);
        }
    }

    private IEnumerator FanAttack()
    {
        isAttacking = true;
        float angleStep = fanAngleRange / (fanBulletCount - 1);
        float startAngle = -fanAngleRange / 2;

        // 瞄准玩家的方向
        float targetAngle = 180f;  // 基础角度为180度（向下）
        if (playerTransform != null)
        {
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;  // 添加180度使其反向
        }

        for (int i = 0; i < fanBulletCount; i++)
        {
            float angle = startAngle + angleStep * i + targetAngle;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            
            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.Euler(0, 0, angle));
            bullet.tag = "BossBullet";
            
            BossBulletBehavior bulletBehavior = bullet.GetComponent<BossBulletBehavior>();
            if (bulletBehavior != null)
            {
                bulletBehavior.SetDirection(direction);
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        isAttacking = false;
    }

    private IEnumerator DashAttack()
    {
        isAttacking = true;
        
        // 如果有玩家，先移动到玩家的X轴位置
        if (playerTransform != null)
        {
            float targetX = playerTransform.position.x;
            float moveTime = 0.5f;
            float elapsedTime = 0f;
            float startX = transform.position.x;
            
            while (elapsedTime < moveTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveTime;
                float newX = Mathf.Lerp(startX, targetX, t);
                transform.position = new Vector3(newX, transform.position.y, transform.position.z);
                yield return null;
            }
        }

        // 向下冲刺
        while (transform.position.y > -4f)
        {
            transform.Translate(Vector3.down * dashSpeed * Time.deltaTime);
            yield return null;
        }

        // 返回原位
        while (transform.position.y < originalY)
        {
            transform.Translate(Vector3.up * returnSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = new Vector3(transform.position.x, originalY, transform.position.z);
        isAttacking = false;
    }

    private IEnumerator SpiralAttack()
    {
        isAttacking = true;
        float currentAngle = 225f;  // 从225度开始（左下方）
        
        for (int i = 0; i < spiralBulletCount; i++)
        {
            // 创建两个对称的子弹
            float leftAngle = currentAngle + (spiralRotateSpeed * Time.deltaTime * i);
            float rightAngle = 180 - leftAngle;  // 关于Y轴对称的角度
            
            // 左边的子弹
            float leftRadians = leftAngle * Mathf.Deg2Rad;
            Vector3 leftSpawnPos = transform.position + new Vector3(
                Mathf.Cos(leftRadians) * spiralRadius,
                Mathf.Sin(leftRadians) * spiralRadius,
                0
            );
            
            GameObject leftBullet = Instantiate(bulletPrefab, leftSpawnPos, Quaternion.Euler(0, 0, leftAngle + 180f));
            leftBullet.tag = "BossBullet";
            Vector3 leftDirection = (leftSpawnPos - transform.position).normalized;
            BossBulletBehavior leftBehavior = leftBullet.GetComponent<BossBulletBehavior>();
            if (leftBehavior != null)
            {
                leftBehavior.SetDirection(leftDirection);
            }

            // 右边的子弹
            float rightRadians = rightAngle * Mathf.Deg2Rad;
            Vector3 rightSpawnPos = transform.position + new Vector3(
                Mathf.Cos(rightRadians) * spiralRadius,
                Mathf.Sin(rightRadians) * spiralRadius,
                0
            );
            
            GameObject rightBullet = Instantiate(bulletPrefab, rightSpawnPos, Quaternion.Euler(0, 0, rightAngle + 180f));
            rightBullet.tag = "BossBullet";
            Vector3 rightDirection = (rightSpawnPos - transform.position).normalized;
            BossBulletBehavior rightBehavior = rightBullet.GetComponent<BossBulletBehavior>();
            if (rightBehavior != null)
            {
                rightBehavior.SetDirection(rightDirection);
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        isAttacking = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Boss hit: " + other.tag);  // 添加调试日志
        if (other.CompareTag("Player"))
        {
            SimplePlayerController player = other.GetComponent<SimplePlayerController>();
            if (player != null)
            {
                Debug.Log("Boss hit player, dealing 10 damage");  // 添加调试日志
                player.TakeDamage(10);  // 碰撞直接消灭玩家
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        Debug.Log("Boss collision: " + other.gameObject.tag);  // 添加调试日志
        if (other.gameObject.CompareTag("Player"))
        {
            SimplePlayerController player = other.gameObject.GetComponent<SimplePlayerController>();
            if (player != null)
            {
                Debug.Log("Boss collided with player, dealing 10 damage");  // 添加调试日志
                player.TakeDamage(10);  // 碰撞直接消灭玩家
            }
        }
    }
}