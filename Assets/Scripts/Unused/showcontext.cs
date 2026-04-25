using UnityEngine;

public class Showcontext : MonoBehaviour
{
    public GameObject first;  // 第一个对象
    public GameObject second; // 第二个对象
    public GameObject third; // 第三个对象
    public GameObject fourth; // 第四个对象
    public GameObject fifth;
    public GameObject sixth;
    public GameObject seventh;
    private int currentIndex = 0; // 当前显示的对象索引
    private GameObject[] objects; // 存储所有对象的数组

    private void Start()
    {
        // 初始化对象数组
        objects = new GameObject[] { first, second, third, fourth,fifth,sixth,seventh };
        
        // 确保开始时所有对象都是隐藏的
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(false);
        }
        
        // 显示第一个对象
        if (first != null)
            first.SetActive(true);
    }

    private void Update()
    {
        // 检测任意键按下
        if (Input.anyKeyDown)
        {
            SwitchToNextObject();
        }
    }

    private void SwitchToNextObject()
    {
        // 隐藏当前对象
        if (currentIndex < objects.Length && objects[currentIndex] != null)
        {
            objects[currentIndex].SetActive(false);
        }

        // 移动到下一个索引
        currentIndex++;

        // 如果还没有到最后一个对象，显示下一个
        if (currentIndex < objects.Length && objects[currentIndex] != null)
        {
            objects[currentIndex].SetActive(true);
        }
    }
}
