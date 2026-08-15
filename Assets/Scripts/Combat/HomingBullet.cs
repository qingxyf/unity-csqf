using UnityEngine;

public class HomingBullet : MonoBehaviour
{
    public Transform target;
    public float turnSpeed = 120f;
    public float moveSpeed = 3.5f;
    public float lifetime = 4f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (target != null)
        {
            // 计算朝向目标的方向
            Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.z;

            // 平滑转向
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
        }

        // 沿当前朝向移动
        float rad = transform.eulerAngles.z * Mathf.Deg2Rad;
        Vector2 moveDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SimplePlayerController player = other.GetComponent<SimplePlayerController>();
            if (player != null)
            {
                player.TakeDamage(2); // 追踪弹伤害略高
            }
            Destroy(gameObject);
        }
    }
}
