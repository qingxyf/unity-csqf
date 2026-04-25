using UnityEngine;

public class ShowSecondExample : MonoBehaviour
{
    public GameObject target; // 目标 GameObject
    public GameObject secondExample; // 第二个示例的 GameObject
    public float showTime = 1f; // 显示时间（秒）

    private void Update()
    {
        // 点击鼠标左键
        if (Input.GetMouseButtonDown(0))
        {
            // 创建一个 Raycast 来检测鼠标点击的位置
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            // 如果点击的位置在目标 GameObject 上
            if (hit.collider != null && hit.collider.gameObject == target)
            {
                // 激活第二个示例
                ShowSecondExampleMethod();
            }
        }
    }

    private void ShowSecondExampleMethod()
    {
        // 激活第二个示例
        secondExample.SetActive(true);

        // 等待 2 秒后关闭第二个示例
        Invoke("HideSecondExample", showTime);
    }

    private void HideSecondExample()
    {
        // 关闭第二个示例
        secondExample.SetActive(false);
    }
}
