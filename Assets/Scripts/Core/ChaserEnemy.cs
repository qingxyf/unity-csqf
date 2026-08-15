using UnityEngine;
using System.Collections;

public class ChaserEnemy : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float baseChaseSpeed = 2f;
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float detectionRange = 5f;

    [Header("行为设置")]
    [SerializeField] private float chargeCooldown = 3f;
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeDuration = 0.5f;

    private Transform playerTransform;
    private Vector2 wanderDirection;
    private float wanderTimer;
    private bool isCharging = false;
    private float chargeCooldownTimer = 0f;
    private float difficultyMultiplier = 1f;

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        PickNewWanderDirection();
    }

    void Update()
    {
        // 获取难度倍率
        if (playerTransform != null)
        {
            var pc = playerTransform.GetComponent<PlayerController>();
            if (pc != null) difficultyMultiplier = pc.GetDifficultyMultiplier();
        }

        if (isCharging) return;

        chargeCooldownTimer -= Time.deltaTime;

        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);

            if (dist < detectionRange)
            {
                // 追踪模式
                ChasePlayer();

                // 冲刺攻击
                if (dist < 2.5f && chargeCooldownTimer <= 0)
                {
                    StartCoroutine(ChargeAttack());
                }
            }
            else
            {
                Wander();
            }
        }
        else
        {
            Wander();
        }

        ClampToScreen();
    }

    private void ChasePlayer()
    {
        Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        float speed = baseChaseSpeed * difficultyMultiplier;
        transform.Translate(dir * speed * Time.deltaTime);
    }

    private void Wander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0)
        {
            PickNewWanderDirection();
        }

        transform.Translate(wanderDirection * wanderSpeed * Time.deltaTime);
    }

    private void PickNewWanderDirection()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        wanderTimer = Random.Range(1.5f, 3f);
    }

    private IEnumerator ChargeAttack()
    {
        isCharging = true;
        chargeCooldownTimer = chargeCooldown;

        // 预警：短暂停顿
        yield return new WaitForSeconds(0.3f);

        // 冲刺方向
        Vector2 chargeDir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        float elapsed = 0f;

        while (elapsed < chargeDuration)
        {
            transform.Translate(chargeDir * chargeSpeed * difficultyMultiplier * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isCharging = false;
    }

    private void ClampToScreen()
    {
        if (Camera.main == null) return;
        Vector3 pos = Camera.main.WorldToViewportPoint(transform.position);

        // 碰到边界就反弹
        if (pos.x < 0.02f || pos.x > 0.98f)
            wanderDirection.x = -wanderDirection.x;
        if (pos.y < 0.02f || pos.y > 0.98f)
            wanderDirection.y = -wanderDirection.y;

        pos.x = Mathf.Clamp(pos.x, 0.02f, 0.98f);
        pos.y = Mathf.Clamp(pos.y, 0.02f, 0.98f);
        transform.position = Camera.main.ViewportToWorldPoint(pos);
    }
}
