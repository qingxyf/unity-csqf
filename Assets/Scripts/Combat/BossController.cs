using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossController : MonoBehaviour
{
    [Header("基础设置")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float attackInterval = 3f;
    [SerializeField] private float enragedAttackInterval = 2f; // 狂暴阶段攻击间隔缩短
    private float originalY;
    private float originalX;

    [Header("扇形攻击设置")]
    [SerializeField] private int fanBulletCount = 15;
    [SerializeField] private float fanAngleRange = 180f;
    [SerializeField] private float bulletSpeed = 5f;

    [Header("冲刺攻击设置")]
    [SerializeField] private float dashSpeed = 1f;
    [SerializeField] private float returnSpeed = 8f;

    [Header("螺旋攻击设置")]
    [SerializeField] private int spiralBulletCount = 30;
    [SerializeField] private float spiralRadius = 2f;
    [SerializeField] private float spiralRotateSpeed = 180f;

    [Header("弹幕雨设置")]
    [SerializeField] private int rainBulletCount = 40;
    [SerializeField] private float rainWidth = 8f;
    [SerializeField] private float rainSpawnY = 6f;

    [Header("十字弹幕设置")]
    [SerializeField] private int crossBulletsPerArm = 8;
    [SerializeField] private float crossRotateSpeed = 45f;

    [Header("追踪弹设置")]
    [SerializeField] private int homingBulletCount = 5;
    [SerializeField] private float homingTurnSpeed = 120f;
    [SerializeField] private float homingLifetime = 4f;

    [Header("散射冲刺设置")]
    [SerializeField] private int dashBurstBullets = 12;

    [Header("=== 狂暴阶段技能（半血以下） ===")]

    [Header("连锁闪现设置")]
    [SerializeField] private int blinkCount = 5;        // 闪现次数
    [SerializeField] private int bulletsPerBlink = 16;   // 每次闪现发射的子弹数

    [Header("引力漩涡设置")]
    [SerializeField] private float vortexDuration = 4f;  // 漩涡持续时间
    [SerializeField] private float vortexPullForce = 3f;  // 吸引力强度
    [SerializeField] private int vortexBulletWaves = 6;   // 漩涡期间发射弹幕波数

    [Header("狂暴连射设置")]
    [SerializeField] private int rageFanWaves = 3;        // 扇形波数
    [SerializeField] private int rageFanBullets = 20;     // 每波子弹数
    [SerializeField] private int rageFinishBullets = 24;  // 终结散射子弹数

    private bool isAttacking = false;
    private Transform playerTransform;
    private SimplePlayerController playerController;
    private int attackCount = 0;
    private bool isEnraged = false;
    private bool hasPlayedEnrageTransition = false;
    private int maxBossHealth;

    void Start()
    {
        originalY = transform.position.y;
        originalX = transform.position.x;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerController = player.GetComponent<SimplePlayerController>();
        }

        if (playerController != null)
            maxBossHealth = playerController.bossHealth;

        StartCoroutine(AttackRoutine());
    }

    // === 阶段判断 ===

    private bool IsBelowHalfHealth()
    {
        if (playerController == null) return false;
        return playerController.bossHealth <= maxBossHealth / 2;
    }

    private float GetCurrentAttackInterval()
    {
        return isEnraged ? enragedAttackInterval : attackInterval;
    }

    // === 攻击循环 ===

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            if (!isAttacking)
            {
                // 检查是否进入狂暴阶段
                if (!isEnraged && IsBelowHalfHealth())
                {
                    isEnraged = true;

                    if (!hasPlayedEnrageTransition)
                    {
                        hasPlayedEnrageTransition = true;
                        StartCoroutine(EnrageTransition());
                        yield return new WaitForSeconds(2.5f);
                    }
                }

                attackCount++;

                if (isEnraged)
                {
                    // 狂暴阶段：7个普通技能 + 3个狂暴技能
                    int attackType = Random.Range(0, 10);
                    StartCoroutine(GetAttackCoroutine(attackType));
                }
                else
                {
                    // 普通阶段：7个技能
                    int attackType = Random.Range(0, 7);
                    StartCoroutine(GetAttackCoroutine(attackType));
                }
            }
            yield return new WaitForSeconds(GetCurrentAttackInterval());
        }
    }

    private IEnumerator GetAttackCoroutine(int type)
    {
        switch (type)
        {
            case 0: return FanAttack();
            case 1: return DashAttack();
            case 2: return SpiralAttack();
            case 3: return BulletRainAttack();
            case 4: return CrossBarrageAttack();
            case 5: return HomingAttack();
            case 6: return DashBurstAttack();
            // 狂暴专属
            case 7: return ChainBlinkAttack();
            case 8: return GravityVortexAttack();
            case 9: return RageBarrageAttack();
            default: return FanAttack();
        }
    }

    // === 狂暴转场 ===

    private IEnumerator EnrageTransition()
    {
        isAttacking = true;
        Debug.Log("Boss 进入狂暴阶段！");

        // 快速移动到场地中央上方
        Vector3 centerTop = new Vector3(0, originalY, transform.position.z);
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, centerTop, elapsed / 0.5f);
            yield return null;
        }

        // 蓄力停顿
        yield return new WaitForSeconds(0.5f);

        // 全方位爆发一波弹幕宣告狂暴
        int burstCount = 36;
        float step = 360f / burstCount;
        for (int i = 0; i < burstCount; i++)
        {
            float angle = i * step;
            float rad = angle * Mathf.Deg2Rad;
            SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), angle);
        }

        yield return new WaitForSeconds(0.3f);

        // 第二波偏移
        for (int i = 0; i < burstCount; i++)
        {
            float angle = i * step + step / 2f;
            float rad = angle * Mathf.Deg2Rad;
            SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), angle);
        }

        yield return new WaitForSeconds(0.5f);

        // 返回原位
        transform.position = new Vector3(originalX, originalY, transform.position.z);
        isAttacking = false;
    }

    // ========================================
    // 普通阶段技能 (0-6)
    // ========================================

    private IEnumerator FanAttack()
    {
        isAttacking = true;
        float angleStep = fanAngleRange / (fanBulletCount - 1);
        float startAngle = -fanAngleRange / 2;

        // 计算朝向玩家的角度
        float targetAngle = 270f; // 默认朝下
        if (playerTransform != null)
        {
            Vector2 toPlayer = (playerTransform.position - transform.position).normalized;
            targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        }

        for (int i = 0; i < fanBulletCount; i++)
        {
            float angle = startAngle + angleStep * i + targetAngle;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            SpawnBullet(transform.position, direction, angle);
            yield return new WaitForSeconds(0.1f);
        }
        isAttacking = false;
    }

    private IEnumerator DashAttack()
    {
        isAttacking = true;

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

        while (transform.position.y > -4f)
        {
            transform.Translate(Vector3.down * dashSpeed * Time.deltaTime);
            yield return null;
        }

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

        // 每步旋转的固定角度，不依赖帧率
        float anglePerStep = spiralRotateSpeed * 0.1f; // 0.1秒间隔 × 旋转速度

        for (int i = 0; i < spiralBulletCount; i++)
        {
            float leftAngle = 225f + anglePerStep * i;
            float rightAngle = 180f - leftAngle;

            float leftRadians = leftAngle * Mathf.Deg2Rad;
            Vector3 leftSpawnPos = transform.position + new Vector3(
                Mathf.Cos(leftRadians) * spiralRadius,
                Mathf.Sin(leftRadians) * spiralRadius, 0);
            SpawnBullet(leftSpawnPos, (leftSpawnPos - transform.position).normalized, leftAngle + 180f);

            float rightRadians = rightAngle * Mathf.Deg2Rad;
            Vector3 rightSpawnPos = transform.position + new Vector3(
                Mathf.Cos(rightRadians) * spiralRadius,
                Mathf.Sin(rightRadians) * spiralRadius, 0);
            SpawnBullet(rightSpawnPos, (rightSpawnPos - transform.position).normalized, rightAngle + 180f);

            yield return new WaitForSeconds(0.1f);
        }
        isAttacking = false;
    }

    private IEnumerator BulletRainAttack()
    {
        isAttacking = true;

        for (int i = 0; i < rainBulletCount; i++)
        {
            float spawnX = transform.position.x + Random.Range(-rainWidth / 2f, rainWidth / 2f);
            Vector3 spawnPos = new Vector3(spawnX, rainSpawnY, 0);

            float angleOffset = Random.Range(-15f, 15f);
            float angle = 270f + angleOffset;
            float radians = angle * Mathf.Deg2Rad;
            SpawnBullet(spawnPos, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)), angle);

            if (i % 3 == 0)
                yield return new WaitForSeconds(0.05f);
        }
        isAttacking = false;
    }

    private IEnumerator CrossBarrageAttack()
    {
        isAttacking = true;
        int waves = 6;
        float baseAngle = 0f;

        for (int w = 0; w < waves; w++)
        {
            for (int arm = 0; arm < 4; arm++)
            {
                float armAngle = baseAngle + arm * 90f;
                for (int b = 0; b < crossBulletsPerArm; b++)
                {
                    float bulletAngle = armAngle + (b - crossBulletsPerArm / 2f) * 3f;
                    float radians = bulletAngle * Mathf.Deg2Rad;
                    SpawnBullet(transform.position, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)), bulletAngle);
                }
            }
            baseAngle += crossRotateSpeed;
            yield return new WaitForSeconds(0.3f);
        }
        isAttacking = false;
    }

    private IEnumerator HomingAttack()
    {
        isAttacking = true;

        for (int i = 0; i < homingBulletCount; i++)
        {
            float angle = Random.Range(0f, 360f);
            float radians = angle * Mathf.Deg2Rad;
            Vector3 spawnOffset = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0) * 1.5f;

            GameObject bullet = Instantiate(bulletPrefab, transform.position + spawnOffset, Quaternion.identity);
            bullet.tag = "BossBullet";

            HomingBullet homing = bullet.AddComponent<HomingBullet>();
            homing.target = playerTransform;
            homing.turnSpeed = homingTurnSpeed;
            homing.moveSpeed = bulletSpeed * 0.7f;
            homing.lifetime = homingLifetime;

            BossBulletBehavior defaultBehavior = bullet.GetComponent<BossBulletBehavior>();
            if (defaultBehavior != null) defaultBehavior.enabled = false;

            yield return new WaitForSeconds(0.4f);
        }
        isAttacking = false;
    }

    private IEnumerator DashBurstAttack()
    {
        isAttacking = true;

        Vector3 centerPos = new Vector3(0, 0, transform.position.z);
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, centerPos, elapsed / 0.4f);
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        float angleStep = 360f / dashBurstBullets;
        for (int i = 0; i < dashBurstBullets; i++)
        {
            float angle = i * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), angle);
        }

        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < dashBurstBullets; i++)
        {
            float angle = i * angleStep + angleStep / 2f;
            float rad = angle * Mathf.Deg2Rad;
            SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), angle);
        }

        yield return new WaitForSeconds(0.3f);

        elapsed = 0f;
        startPos = transform.position;
        Vector3 homePos = new Vector3(originalX, originalY, transform.position.z);

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, homePos, elapsed / 0.5f);
            yield return null;
        }

        transform.position = homePos;
        isAttacking = false;
    }

    // ========================================
    // 狂暴阶段技能 (7-9) — 半血以下解锁
    // ========================================

    /// <summary>
    /// 技能8：连锁闪现
    /// Boss在场地多个位置快速闪现，每次闪现释放一圈子弹。
    /// 视觉上像不断瞬移，留下满屏弹幕，玩家需要预判安全区域。
    /// </summary>
    private IEnumerator ChainBlinkAttack()
    {
        isAttacking = true;

        for (int i = 0; i < blinkCount; i++)
        {
            // 闪现到随机位置
            Vector3 blinkTarget = GetRandomArenaPosition();

            // 瞬移（极快移动）
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, blinkTarget, elapsed / 0.1f);
                yield return null;
            }
            transform.position = blinkTarget;

            // 到达后立刻释放一圈子弹
            float angleStep = 360f / bulletsPerBlink;
            for (int b = 0; b < bulletsPerBlink; b++)
            {
                float angle = b * angleStep + i * 15f; // 每次闪现偏移15度，错开弹幕
                float rad = angle * Mathf.Deg2Rad;
                SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), angle);
            }

            // 短暂停留
            yield return new WaitForSeconds(0.25f);
        }

        // 最后闪回原位
        float ret = 0f;
        Vector3 from = transform.position;
        Vector3 home = new Vector3(originalX, originalY, transform.position.z);
        while (ret < 0.15f)
        {
            ret += Time.deltaTime;
            transform.position = Vector3.Lerp(from, home, ret / 0.15f);
            yield return null;
        }
        transform.position = home;

        isAttacking = false;
    }

    /// <summary>
    /// 技能9：引力漩涡
    /// 在场地中央生成一个"黑洞"，持续吸引玩家向中心移动。
    /// 同时Boss从上方发射弹幕，玩家需要对抗吸力躲避子弹。
    /// </summary>
    private IEnumerator GravityVortexAttack()
    {
        isAttacking = true;

        // 创建漩涡中心标记（用一颗不动的子弹做视觉标记）
        Vector3 vortexCenter = new Vector3(0, -1f, 0);
        GameObject vortexMarker = Instantiate(bulletPrefab, vortexCenter, Quaternion.identity);
        vortexMarker.tag = "BossBullet";
        vortexMarker.transform.localScale = Vector3.one * 2f; // 放大作为视觉标记
        // 禁止它移动
        BossBulletBehavior markerBehavior = vortexMarker.GetComponent<BossBulletBehavior>();
        if (markerBehavior != null) markerBehavior.enabled = false;
        // 让它也别因碰撞被销毁 — 移除碰撞
        Collider2D markerCol = vortexMarker.GetComponent<Collider2D>();
        if (markerCol != null) markerCol.enabled = false;

        float elapsed = 0f;
        int wavesFired = 0;
        float waveInterval = vortexDuration / vortexBulletWaves;
        float waveTimer = 0f;

        while (elapsed < vortexDuration)
        {
            elapsed += Time.deltaTime;
            waveTimer += Time.deltaTime;

            // 持续吸引玩家
            if (playerTransform != null)
            {
                Vector3 pullDir = (vortexCenter - playerTransform.position).normalized;
                playerTransform.position += pullDir * vortexPullForce * Time.deltaTime;
            }

            // 旋转漩涡标记（视觉效果）
            if (vortexMarker != null)
                vortexMarker.transform.Rotate(0, 0, 360f * Time.deltaTime);

            // 定时从Boss位置发射弹幕
            if (waveTimer >= waveInterval && wavesFired < vortexBulletWaves)
            {
                waveTimer = 0f;
                wavesFired++;

                // 朝玩家方向发射一小波扇形弹幕
                if (playerTransform != null)
                {
                    Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                    float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

                    for (int b = -3; b <= 3; b++)
                    {
                        float angle = baseAngle + b * 12f;
                        float rad = angle * Mathf.Deg2Rad;
                        SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), angle);
                    }
                }
            }

            yield return null;
        }

        // 漩涡结束，销毁标记
        if (vortexMarker != null) Destroy(vortexMarker);

        isAttacking = false;
    }

    /// <summary>
    /// 技能10：狂暴连射
    /// 三波交错扇形弹幕（每波角度不同，交叉覆盖），
    /// 紧接着一次全屏散射作为终结。整个过程极为密集。
    /// </summary>
    private IEnumerator RageBarrageAttack()
    {
        isAttacking = true;

        // 先移动到场地上方中央
        Vector3 centerTop = new Vector3(0, originalY, transform.position.z);
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, centerTop, elapsed / 0.3f);
            yield return null;
        }

        // === 三波交错扇形 ===
        float[] waveBaseAngles = { 250f, 270f, 290f }; // 左偏、正下、右偏

        for (int w = 0; w < rageFanWaves; w++)
        {
            float baseAngle = waveBaseAngles[w % waveBaseAngles.Length];
            float fanRange = 120f;
            float step = fanRange / (rageFanBullets - 1);
            float start = baseAngle - fanRange / 2f;

            for (int b = 0; b < rageFanBullets; b++)
            {
                float angle = start + step * b;
                float rad = angle * Mathf.Deg2Rad;
                SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), angle);
            }

            yield return new WaitForSeconds(0.35f);
        }

        // === 短暂蓄力 ===
        yield return new WaitForSeconds(0.4f);

        // === 终结散射：全方位两波 ===
        float finishStep = 360f / rageFinishBullets;
        for (int i = 0; i < rageFinishBullets; i++)
        {
            float angle = i * finishStep;
            float rad = angle * Mathf.Deg2Rad;
            SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), angle);
        }

        yield return new WaitForSeconds(0.15f);

        for (int i = 0; i < rageFinishBullets; i++)
        {
            float angle = i * finishStep + finishStep / 2f;
            float rad = angle * Mathf.Deg2Rad;
            SpawnBullet(transform.position, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), angle);
        }

        yield return new WaitForSeconds(0.3f);

        // 返回原位
        elapsed = 0f;
        startPos = transform.position;
        Vector3 home = new Vector3(originalX, originalY, transform.position.z);

        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, home, elapsed / 0.4f);
            yield return null;
        }
        transform.position = home;

        isAttacking = false;
    }

    // ========================================
    // 工具方法
    // ========================================

    private void SpawnBullet(Vector3 position, Vector2 direction, float angle)
    {
        GameObject bullet = Instantiate(bulletPrefab, position, Quaternion.Euler(0, 0, angle));
        bullet.tag = "BossBullet";

        BossBulletBehavior behavior = bullet.GetComponent<BossBulletBehavior>();
        if (behavior != null)
        {
            behavior.SetDirection(direction);
        }
    }

    private Vector3 GetRandomArenaPosition()
    {
        if (Camera.main == null)
            return Vector3.zero;

        float camHeight = 2f * Camera.main.orthographicSize;
        float camWidth = camHeight * Camera.main.aspect;
        float cx = Camera.main.transform.position.x;
        float cy = Camera.main.transform.position.y;

        float x = Random.Range(cx - camWidth * 0.35f, cx + camWidth * 0.35f);
        float y = Random.Range(cy - camHeight * 0.25f, cy + camHeight * 0.35f);

        return new Vector3(x, y, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SimplePlayerController player = other.GetComponent<SimplePlayerController>();
            if (player != null)
                player.TakeDamage(10);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            SimplePlayerController player = other.gameObject.GetComponent<SimplePlayerController>();
            if (player != null)
                player.TakeDamage(10);
        }
    }
}
