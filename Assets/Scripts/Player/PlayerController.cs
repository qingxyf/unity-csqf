using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    public float baseSpeed = 5f;
    private float currentSpeed;

    [Header("射击设置")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform shootPoint;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI statusText;

    [Header("音效")]
    public AudioSource audioSource;
    public AudioClip collectSound;
    public AudioClip comboBreakSound;
    public AudioClip powerUpSound;
    public AudioClip chunriyingSound; // 春日影音效

    [Header("通关设置")]
    public int winScore = 40;

    // --- 积分系统 ---
    private int score = 0;
    private int combo = 0;
    private float comboTimer = 0f;
    private float comboWindow = 2f; // 连击窗口（秒）
    private float scoreMultiplier = 1f;

    // --- 状态效果 ---
    private bool hasMagnet = false;
    private bool hasShield = false;
    private float magnetRange = 3f;
    private float statusTimer = 0f;
    private string activeStatus = "";

    // --- 难度递增 ---
    private float difficultyMultiplier = 1f;

    // --- 无敌帧 ---
    private bool isInvincible = false;

    void Start()
    {
        currentSpeed = baseSpeed;
        UpdateUI();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (shootPoint == null)
            shootPoint = transform;
    }

    void Update()
    {
        HandleMovement();
        UpdateComboTimer();
        UpdateStatusTimer();
        UpdateDifficulty();

        if (hasMagnet)
            AttractNearbyTargets();
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal") * currentSpeed * Time.deltaTime;
        float moveY = Input.GetAxis("Vertical") * currentSpeed * Time.deltaTime;
        transform.Translate(new Vector3(moveX, moveY, 0));

        // 限制在屏幕内
        Vector3 pos = Camera.main.WorldToViewportPoint(transform.position);
        pos.x = Mathf.Clamp(pos.x, 0.05f, 0.95f);
        pos.y = Mathf.Clamp(pos.y, 0.05f, 0.95f);
        transform.position = Camera.main.ViewportToWorldPoint(pos);
    }

    // === 碰撞处理 ===
    private void OnTriggerEnter2D(Collider2D collision)
    {
        string tag = collision.gameObject.tag;

        switch (tag)
        {
            case "Target": // 普通积分目标
                CollectTarget(collision.gameObject, 1);
                break;

            case "Target1": // 冰冻陷阱 → 改为追踪敌人碰撞
                HandleEnemyHit(collision.gameObject);
                break;

            case "Target2": // 加速道具 → 改为随机道具
                HandlePowerUp(collision.gameObject);
                break;

            case "Target3": // 春日影音效目标
                if (chunriyingSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(chunriyingSound);
                }
                CollectTarget(collision.gameObject, 5);
                collision.gameObject.SetActive(false); // 播放后消失
                break;

            case "BonusTarget": // 奖励目标（新增）
                CollectTarget(collision.gameObject, 3);
                break;
        }
    }

    // === 收集目标 ===
    private void CollectTarget(GameObject target, int basePoints)
    {
        // 连击计算
        combo++;
        comboTimer = comboWindow;

        // 积分 = 基础分 × 连击倍率 × 道具倍率
        float comboBonus = 1f + (combo - 1) * 0.25f; // 每连击+25%
        int points = Mathf.CeilToInt(basePoints * comboBonus * scoreMultiplier);
        score += points;

        // 音效
        if (collectSound != null && audioSource != null)
            audioSource.PlayOneShot(collectSound);

        // 重新定位目标
        ICollectible collectible = target.GetComponent<ICollectible>();
        if (collectible != null)
        {
            collectible.OnCollected();
        }
        else
        {
            // 兼容旧的 Target 对象
            target.transform.position = GetRandomScreenPosition(target.transform.position, 3f);
        }

        UpdateUI();

        if (score >= winScore)
        {
            SceneManager.LoadScene("final");
        }
    }

    // === 敌人碰撞 ===
    private void HandleEnemyHit(GameObject enemy)
    {
        if (isInvincible) return;

        if (hasShield)
        {
            hasShield = false;
            activeStatus = "";
            UpdateUI();
            return;
        }

        // 扣分 + 断连击
        int penalty = Mathf.Max(1, combo);
        score = Mathf.Max(0, score - penalty);
        combo = 0;
        comboTimer = 0f;

        if (comboBreakSound != null && audioSource != null)
            audioSource.PlayOneShot(comboBreakSound);

        // 短暂冰冻
        StartCoroutine(FreezePlayer(1.5f));

        // 无敌帧
        StartCoroutine(InvincibilityFrames(2f));

        // 敌人碰撞后重新定位
        ICollectible collectible = enemy.GetComponent<ICollectible>();
        if (collectible != null)
        {
            collectible.OnCollected();
        }
        else
        {
            // 兼容旧的 NewBehaviourScript
            var script = enemy.GetComponent<NewBehaviourScript>();
            if (script != null && script.IsActive())
            {
                enemy.transform.GetChild(0).gameObject.SetActive(false);
                script.SetInactive();
            }
        }

        UpdateUI();
    }

    // === 道具处理 ===
    private void HandlePowerUp(GameObject powerUp)
    {
        // 随机选择一种效果
        int effect = Random.Range(0, 4);

        if (powerUpSound != null && audioSource != null)
            audioSource.PlayOneShot(powerUpSound);

        switch (effect)
        {
            case 0: // 加速
                StartCoroutine(SpeedBoost(8f, 5f));
                activeStatus = "加速中!";
                break;
            case 1: // 磁铁
                StartCoroutine(MagnetEffect(5f));
                activeStatus = "磁铁吸附!";
                break;
            case 2: // 双倍积分
                StartCoroutine(ScoreMultiplierEffect(2f, 6f));
                activeStatus = "双倍积分!";
                break;
            case 3: // 护盾
                hasShield = true;
                activeStatus = "护盾保护!";
                statusTimer = 10f;
                break;
        }

        // 隐藏道具
        var speedScript = powerUp.GetComponent<SpeedUP>();
        if (speedScript != null && speedScript.IsActive())
        {
            powerUp.transform.GetChild(0).gameObject.SetActive(false);
            speedScript.SetInactive();
        }

        UpdateUI();
    }

    // === 状态效果协程 ===
    private IEnumerator SpeedBoost(float boostedSpeed, float duration)
    {
        currentSpeed = boostedSpeed;
        statusTimer = duration;
        yield return new WaitForSeconds(duration);
        currentSpeed = baseSpeed;
        if (activeStatus == "加速中!") activeStatus = "";
        UpdateUI();
    }

    private IEnumerator MagnetEffect(float duration)
    {
        hasMagnet = true;
        statusTimer = duration;
        yield return new WaitForSeconds(duration);
        hasMagnet = false;
        if (activeStatus == "磁铁吸附!") activeStatus = "";
        UpdateUI();
    }

    private IEnumerator ScoreMultiplierEffect(float multiplier, float duration)
    {
        scoreMultiplier = multiplier;
        statusTimer = duration;
        yield return new WaitForSeconds(duration);
        scoreMultiplier = 1f;
        if (activeStatus == "双倍积分!") activeStatus = "";
        UpdateUI();
    }

    private IEnumerator FreezePlayer(float duration)
    {
        float savedSpeed = currentSpeed;
        currentSpeed = 0f;
        yield return new WaitForSeconds(duration);
        currentSpeed = savedSpeed > 0 ? savedSpeed : baseSpeed;
    }

    private IEnumerator InvincibilityFrames(float duration)
    {
        isInvincible = true;

        // 闪烁效果
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                sr.enabled = !sr.enabled;
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            sr.enabled = true;
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }

        isInvincible = false;
    }

    // === 磁铁吸附 ===
    private void AttractNearbyTargets()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Target");
        foreach (var t in targets)
        {
            float dist = Vector2.Distance(transform.position, t.transform.position);
            if (dist < magnetRange && dist > 0.1f)
            {
                Vector3 dir = (transform.position - t.transform.position).normalized;
                t.transform.position += dir * 8f * Time.deltaTime;
            }
        }
    }

    // === 连击计时 ===
    private void UpdateComboTimer()
    {
        if (combo > 0 && comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                combo = 0;
                UpdateUI();
            }
        }
    }

    // === 状态计时 ===
    private void UpdateStatusTimer()
    {
        if (statusTimer > 0)
        {
            statusTimer -= Time.deltaTime;
            if (statusTimer <= 0)
            {
                if (activeStatus == "护盾保护!") hasShield = false;
                activeStatus = "";
                UpdateUI();
            }
        }
    }

    // === 难度递增 ===
    private void UpdateDifficulty()
    {
        // 每10分难度提升一级
        difficultyMultiplier = 1f + (score / 10) * 0.15f;
    }

    public float GetDifficultyMultiplier()
    {
        return difficultyMultiplier;
    }

    // === UI ===
    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"积分: {score}";
        }

        if (comboText != null)
        {
            if (combo > 1)
            {
                float comboBonus = 1f + (combo - 1) * 0.25f;
                comboText.text = $"{combo} 连击! x{comboBonus:F1}";
            }
            else
            {
                comboText.text = "";
            }
        }

        if (statusText != null)
        {
            statusText.text = activeStatus;
        }
    }

    // === 工具方法 ===
    public static Vector3 GetRandomScreenPosition(Vector3 currentPos, float minDistance)
    {
        Camera cam = Camera.main;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        float camLeft = cam.transform.position.x - camWidth / 2;
        float camRight = cam.transform.position.x + camWidth / 2;
        float camBottom = cam.transform.position.y - camHeight / 2;
        float camTop = cam.transform.position.y + camHeight / 2;

        Vector3 randomPos;
        int attempts = 0;
        do
        {
            float rx = Random.Range(camLeft + 0.5f, camRight - 0.5f);
            float ry = Random.Range(camBottom + 0.5f, camTop - 0.5f);
            randomPos = new Vector3(rx, ry, currentPos.z);
            attempts++;
        }
        while (Vector3.Distance(currentPos, randomPos) < minDistance && attempts < 50);

        return randomPos;
    }
}
