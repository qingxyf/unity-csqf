using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public int score = 0;
    public TextMeshProUGUI scoreText;
    private float normalSpeed = 5f;
    public AudioSource audioSource;
    public AudioClip chunriyingSound;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float shootInterval = 0.5f;
    [SerializeField] private Transform shootPoint;
    private bool canShoot = true;

    void Start()
    {
        UpdateScoreDisplay();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
       
        if (shootPoint == null)
        {
            shootPoint = transform;
        }
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        float moveY = Input.GetAxis("Vertical") * speed * Time.deltaTime;
        transform.Translate(new Vector3(moveX, moveY, 0));

        // 限制玩家在屏幕范围内
        Vector3 pos = Camera.main.WorldToViewportPoint(transform.position);
        pos.x = Mathf.Clamp(pos.x, 0.05f, 0.95f);
        pos.y = Mathf.Clamp(pos.y, 0.05f, 0.95f);
        transform.position = Camera.main.ViewportToWorldPoint(pos);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            score++;
            UpdateScoreDisplay();
            Vector3 newPosition = GetRandomPosition(collision.gameObject.transform.position, 5f);
            collision.gameObject.transform.position = newPosition;
            Debug.Log("Score: " + score);
        }
        if (collision.gameObject.CompareTag("Target1"))
        {
            NewBehaviourScript script = collision.gameObject.GetComponent<NewBehaviourScript>();
            if (script.IsActive())
            {
                collision.transform.GetChild(0).gameObject.SetActive(false);
                script.SetInactive();
                StartCoroutine(StopPlayerMovement());
            }
        }
        if (collision.gameObject.CompareTag("Target2"))
        {
            SpeedUP speedScript = collision.gameObject.GetComponent<SpeedUP>();
            if (speedScript.IsActive())
            {
                collision.transform.GetChild(0).gameObject.SetActive(false);
                speedScript.SetInactive();
                StartCoroutine(speedup());
            }
        }
        if (collision.gameObject.CompareTag("Target3"))
        {
            if (chunriyingSound != null)
            {
                audioSource.PlayOneShot(chunriyingSound);
                collision.gameObject.SetActive(false);
            }
            else
            {
                collision.gameObject.SetActive(false);
            }
        }
    }

    private Vector3 GetRandomPosition(Vector3 currentPosition, float minDistance)
    {
        Camera mainCamera = Camera.main;
        float camHeight = 2f * mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;
        float camLeft = mainCamera.transform.position.x - camWidth / 2;
        float camRight = mainCamera.transform.position.x + camWidth / 2;
        float camBottom = mainCamera.transform.position.y - camHeight / 2;
        float camTop = mainCamera.transform.position.y + camHeight / 2;

        Vector3 randomPosition;
        do
        {
            float randomX = Random.Range(camLeft, camRight);
            float randomY = Random.Range(camBottom, camTop);
            randomPosition = new Vector3(randomX, randomY, currentPosition.z);
        }
        while (Vector3.Distance(currentPosition, randomPosition) < minDistance);

        return randomPosition;
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = "chaoqingfeng:" + score;
            if (score >= 40)
            {
                SceneManager.LoadScene("final");
            }
        }
    }

    private IEnumerator StopPlayerMovement()
    {
        speed = 0f;
        yield return new WaitForSeconds(5f);
        speed = normalSpeed;
    }

    private IEnumerator speedup()
    {
        speed = 10f;
        yield return new WaitForSeconds(5f);
        speed = normalSpeed;
    }

    private IEnumerator HideAfterSound(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }
}