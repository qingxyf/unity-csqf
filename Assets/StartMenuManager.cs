using UnityEngine;
using UnityEngine.UI;

public class StartMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject thirdButton;  // 第三个需要解锁的按钮

    void Start()
    {
        // 检查是否已经打败过Boss
        bool hasDefeatedBoss = PlayerPrefs.GetInt("HasDefeatedBoss", 0) == 1;
        
        // 根据是否打败过Boss来显示或隐藏第三个按钮
        if (thirdButton != null)
        {
            thirdButton.SetActive(hasDefeatedBoss);
        }
    }

    // 如果需要重置游戏进度（测试用）
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey("HasDefeatedBoss");
        PlayerPrefs.Save();
        if (thirdButton != null)
        {
            thirdButton.SetActive(false);
        }
    }
} 