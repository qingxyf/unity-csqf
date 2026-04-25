using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("UI设置")]
    public GameObject scorePanel; // 分数面板
    public TextMeshProUGUI scoreText; // 分数文本
    public TextMeshProUGUI achievementText; // 成就文本

    private float targetTime = 230f; // 3分50秒 = 230秒
    private MidiNoteGenerator midiGenerator;
    private bool hasShownScore = false;

    void Start()
    {
        // 获取MidiNoteGenerator实例
        midiGenerator = FindObjectOfType<MidiNoteGenerator>();
        
        // 确保分数面板初始状态为隐藏
        if (scorePanel != null)
        {
            scorePanel.SetActive(false);
        }
    }

    void Update()
    {
        if (!hasShownScore && Time.time >= targetTime)
        {
            ShowScorePanel();
        }
    }

    void ShowScorePanel()
    {
        hasShownScore = true;
        Time.timeScale = 0f; // 暂停游戏

        if (scorePanel != null)
        {
            scorePanel.SetActive(true);
            
            // 显示分数
            if (scoreText != null)
            {
                scoreText.text = "你的得分: " + midiGenerator.score;
            }

            // 检查是否获得成就
            if (achievementText != null)
            {
                if (midiGenerator.score >= 480)
                {
                    achievementText.text = "恭喜获得成就：音游高手！";
                    PlayerPrefs.SetInt("MidiMaster", 1); // 保存成就
                    PlayerPrefs.Save();
                }
                else
                {
                    achievementText.text = "继续加油！";
                }
            }
        }
    }

    public void ReturnToStart()
    {
        Time.timeScale = 1f; // 恢复游戏时间
        SceneManager.LoadScene("Start");
    }
}