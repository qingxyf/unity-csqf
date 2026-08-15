using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class SimplePlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;     // 移动速度

    [Header("射击设置")]
    [SerializeField] private GameObject bulletPrefab;   // 子弹预制体
    [SerializeField] private float shootInterval = 0.5f;  // 射击间隔
    [SerializeField] private Transform shootPoint;      // 射击点（可选）

    [Header("玩家属性")]
    [SerializeField] private int maxHealth = 10;  // 最大血量
    private int currentHealth;  // 当前血量

    [Header("Boss属性")]
    public int bossHealth = 100;  // Boss的血量
    public TextMeshProUGUI bossHealthText;  // Boss血量显示
    private int hitCount = 0;  // 记录击中次数

    private bool canShoot = true;  // 是否可以射击

    [Header("UI设置")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Image healthBarFill;        // 玩家血条填充图
    [SerializeField] private Image bossHealthBarFill;    // Boss血条填充图（可选）
    private int maxBossHealth;

    void Start()
    {
        currentHealth = maxHealth;
        maxBossHealth = bossHealth;
        UpdateBossHealthDisplay();
        UpdatePlayerHealthBar();

        // 检查是否已设置子弹预制体
        if (bulletPrefab == null)
        {
            Debug.LogError("未设置子弹预制体！");
            return;
        }

        // 如果没有设置射击点，使用玩家自身位置
        if (shootPoint == null)
        {
            shootPoint = transform;
        }

        // 开始自动射击协程
        StartCoroutine(AutoShoot());
    }

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // 获取输入
        float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float moveY = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

        // 移动玩家
        transform.Translate(new Vector3(moveX, moveY, 0));

        // 限制玩家在屏幕范围内
        Vector3 pos = Camera.main.WorldToViewportPoint(transform.position);
        pos.x = Mathf.Clamp(pos.x, 0.05f, 0.95f);
        pos.y = Mathf.Clamp(pos.y, 0.05f, 0.95f);
        transform.position = Camera.main.ViewportToWorldPoint(pos);
    }

    private IEnumerator AutoShoot()
    {
        while (true)
        {
            if (canShoot)
            {
                // 在射击点位置生成子弹
                Vector3 spawnPosition = shootPoint.position;
                Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

                // 等待射击间隔
                canShoot = false;
                yield return new WaitForSeconds(shootInterval);
                canShoot = true;
            }
            yield return null;
        }
    }

    public void OnBossHit()
    {
        bossHealth--;
        hitCount++;

        UpdateBossHealthDisplay();

        if (bossHealth <= 0)
        {
            ShowVictoryPanel();
        }
    }

    private void UpdateBossHealthDisplay()
    {
        if (bossHealthText != null)
        {
            if (bossHealth > 0)
                bossHealthText.text = "BossHP:" + bossHealth;
            else
                bossHealthText.text = "";
        }

        if (bossHealthBarFill != null)
        {
            float ratio = Mathf.Clamp01((float)bossHealth / maxBossHealth);
            bossHealthBarFill.fillAmount = ratio;

            // Boss血条：红色渐深
            bossHealthBarFill.color = Color.Lerp(Color.red, new Color(0.8f, 0.2f, 0.2f), ratio);
        }
    }

    private void ShowVictoryPanel()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);  // 显示胜利面板
            Time.timeScale = 0f;  // 暂停游戏
            PlayerPrefs.SetInt("HasDefeatedBoss", 1);  // 记录已经打败过Boss
            PlayerPrefs.Save();  // 保存数据
        }
    }

    public void ReturnToStartScene()
    {
        Time.timeScale = 1f;  // 恢复游戏时间
        SceneManager.LoadScene("start");  // 切换到开始场景
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdatePlayerHealthBar();
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdatePlayerHealthBar()
    {
        if (healthBarFill != null)
        {
            float ratio = Mathf.Clamp01((float)currentHealth / maxHealth);
            healthBarFill.fillAmount = ratio;

            // 血量颜色渐变：绿 → 黄 → 红
            if (ratio > 0.5f)
                healthBarFill.color = Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f);
            else
                healthBarFill.color = Color.Lerp(Color.red, Color.yellow, ratio * 2f);
        }
    }

    private void Die()
    {
        SceneManager.LoadScene("start");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BossBullet"))
        {
            TakeDamage(1);  // Boss子弹造成1点伤害
            Destroy(other.gameObject);  // 销毁子弹
        }
    }
}
