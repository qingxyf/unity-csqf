using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    [SerializeField] private Button startButton;    // 开始游戏按钮
    [SerializeField] private Button quitButton;     // 退出按钮
    [SerializeField] private Button thirdButton;     // 退出按钮
    [SerializeField] private Button forthButton;
    void Start()
    {
        // 添加按钮点击事件监听
        startButton.onClick.AddListener(StartGame);
        quitButton.onClick.AddListener(QuitGame);
        thirdButton.onClick.AddListener(ThirdButton);
        forthButton.onClick.AddListener(ForthButton);
    }

    void StartGame()
    {
        // 加载游戏主场景
        // "MainScene" 替换为你的游戏主场景名称
        SceneManager.LoadScene("SampleScene");
    }

    void QuitGame()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    void ThirdButton()
    {
        SceneManager.LoadScene("thirdscene");
    }
    void ForthButton()
    {
        SceneManager.LoadScene("forth");
    }
} 