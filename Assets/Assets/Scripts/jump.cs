using UnityEngine;

public class JumpController : MonoBehaviour
{
    [Header("跳跃参数")]
    [SerializeField] private float initialVelocity = 10f; // 初始向上速度
    [SerializeField] private float gravity = -9.8f; // 重力加速度
    
    private bool isJumping = false; // 是否正在跳跃
    private float currentVelocity; // 当前速度
    private float initialY; // 初始Y坐标
    private Vector3 startPosition; // 起始位置

    void Start()
    {
        startPosition = transform.position;
        initialY = startPosition.y;
    }

    void Update()
    {
        // 只有在未跳跃状态下按空格才能开始跳跃
        if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
        {
            StartJump();
        }

        // 如果正在跳跃，更新位置
        if (isJumping)
        {
            UpdateJump();
        }
    }

    void StartJump()
    {
        isJumping = true;
        currentVelocity = initialVelocity;
    }

    void UpdateJump()
    {
        // 更新速度（v = v0 + at）
        currentVelocity += gravity * Time.deltaTime;

        // 更新位置（s = s0 + vt）
        Vector3 newPosition = transform.position;
        newPosition.y += currentVelocity * Time.deltaTime;
        transform.position = newPosition;

        // 检查是否返回起始位置
        if (transform.position.y <= initialY && currentVelocity < 0)
        {
            // 重置位置和状态
            transform.position = startPosition;
            isJumping = false;
        }
    }
}