using UnityEngine;
using System.Collections;

public class AnimationSwitcher : MonoBehaviour
{
    // 动画控制器引用
    private Animator animator;
    
    // 动画参数名称
    public string animationTriggerName = "PlayAnimation2";
    // 返回动画1的参数名称（布尔类型）
    public string returnToAnimation1Param = "ReturnToAnimation1";
    
    // 动画2播放的时间（秒）
    public float animation2Duration = 5.0f; // 修改为5秒
    
    // 是否正在播放动画2
    private bool isPlayingAnimation2 = false;
    
    void Start()
    {
        // 获取Animator组件
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError("未找到Animator组件！请确保该脚本附加到有Animator组件的游戏对象上。");
        }
    }
    
    // Update方法已移除空格键检测，改为由外部事件触发
    
    // 播放动画2并在指定时间后恢复动画1
    public void PlayAnimation2()
    {
        if (animator != null && !isPlayingAnimation2)
        {
            // 设置标志，防止重复触发
            isPlayingAnimation2 = true;
            
            // 确保返回参数为false
            animator.SetBool(returnToAnimation1Param, false);
            
            // 触发动画2
            animator.SetTrigger(animationTriggerName);
            
            // 启动协程，等待指定时间后恢复动画1
            StartCoroutine(ResetAnimationAfterDelay(animation2Duration));
        }
    }
    
    // 协程：等待指定时间后恢复动画1
    private IEnumerator ResetAnimationAfterDelay(float delay)
    {
        // 等待指定的时间
        yield return new WaitForSeconds(delay);
        
        // 重置动画状态（恢复到动画1）
        animator.ResetTrigger(animationTriggerName);
        
        // 设置返回动画1的布尔参数为true
        animator.SetBool(returnToAnimation1Param, true);
        
        // 重置标志
        isPlayingAnimation2 = false;
    }
}