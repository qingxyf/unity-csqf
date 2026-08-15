using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("UI设置")]
    public GameObject scorePanel;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI achievementText;

    [Header("计时")]
    public float targetTime = 230f; // 3分50秒

    private MidiNoteGenerator midiGenerator;
    private bool hasShownScore = false;

    void Start()
    {
        midiGenerator = FindObjectOfType<MidiNoteGenerator>();

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
        Time.timeScale = 0f;

        if (scorePanel == null) return;

        scorePanel.SetActive(true);

        if (scoreText != null && midiGenerator != null)
        {
            int total = midiGenerator.totalNotes;
            int hit = midiGenerator.score;
            int miss = midiGenerator.missCount;
            float accuracy = total > 0 ? (float)hit / total * 100f : 0f;

            scoreText.text = $"得分: {hit}\n" +
                             $"未接: {miss}\n" +
                             $"总计: {total}\n" +
                             $"准确率: {accuracy:F1}%";
        }

        if (achievementText != null && midiGenerator != null)
        {
            float accuracy = midiGenerator.totalNotes > 0
                ? (float)midiGenerator.score / midiGenerator.totalNotes * 100f
                : 0f;

            if (accuracy >= 90f)
            {
                achievementText.text = "完美演奏！获得成就：音游大师！";
                PlayerPrefs.SetInt("MidiMaster", 1);
                PlayerPrefs.Save();
            }
            else if (accuracy >= 70f)
            {
                achievementText.text = "不错的节奏感！";
            }
            else
            {
                achievementText.text = "继续练习吧！";
            }
        }
    }

    public void ReturnToStart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("start");
    }
}
