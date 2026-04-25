using UnityEngine;

public class InfiniteBackgroundScroller : MonoBehaviour
{
    public float scrollSpeed = 5f;
    public Transform[] backgrounds; // 背景图片数组，通常是两个相同的图片
    
    private float backgroundWidth;
    private bool isScrolling = false;
    private AnimationSwitcher animationSwitcher; // 引用AnimationSwitcher脚本
    private float scrollTimer = 0f; // 计时器
    
    void Start()
    {
        // 获取背景宽度（假设所有背景图片宽度相同）
        if (backgrounds.Length > 0 && backgrounds[0].GetComponent<SpriteRenderer>() != null)
        {
            backgroundWidth = backgrounds[0].GetComponent<SpriteRenderer>().bounds.size.x;
        }
        
        // 查找场景中的AnimationSwitcher组件
        animationSwitcher = FindObjectOfType<AnimationSwitcher>();
        if (animationSwitcher == null)
        {
            Debug.LogWarning("未找到AnimationSwitcher组件，将使用默认滚动时间。");
        }
    }
    
    void Update()
    {
        // 空格键检测已移除，改为由外部事件触发
        
        // 如果正在滚动，更新计时器
        if (isScrolling)
        {
            scrollTimer += Time.deltaTime;
            
            // 检查是否达到动画2的持续时间
            if (animationSwitcher != null && scrollTimer >= animationSwitcher.animation2Duration)
            {
                StopScrolling();
                scrollTimer = 0f;
            }
            
            // 移动所有背景
            foreach (Transform background in backgrounds)
            {
                background.Translate(Vector3.left * scrollSpeed * Time.deltaTime);
                
                // 检查是否需要重置位置
                if (background.position.x < -backgroundWidth)
                {
                    // 计算新位置：将背景移到最后一个背景的右侧
                    Vector3 newPos = background.position;
                    newPos.x += backgroundWidth * backgrounds.Length;
                    background.position = newPos;
                }
            }
        }
    }
    
    public void StartScrolling()
    {
        isScrolling = true;
        scrollTimer = 0f; // 重置计时器
        Debug.Log("背景滚动状态: 开始");
    }
    
    public void StopScrolling()
    {
        isScrolling = false;
        Debug.Log("背景滚动状态: 停止");
    }
}