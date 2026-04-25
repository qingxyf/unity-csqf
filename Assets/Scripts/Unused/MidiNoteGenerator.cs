using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.IO;

public class MidiNoteGenerator : MonoBehaviour
{
    [Header("示例预制体")]
    public GameObject example1Prefab; // 示例1的预制体
    public GameObject example2Prefab; // 示例2的预制体
    
    [Header("MIDI设置")]
    public string midiFileName = "missyou.mid"; // MIDI文件名称
    
    [Header("生成设置")]
    public float moveSpeed = 10f; // 示例移动速度
    public float spawnHeight = -2.5f; // 示例1的生成高度
    public float spawnHeight2 = 1f; // 示例2的生成高度
    public float TIME_OFFSET = 8.89f; // 添加时间偏移常量

    private MidiFile midiFileData;
    public int hitCount = 0;
    public int score = 0; // 记录分数
    private List<double> noteTimes = new List<double>(); // 存储音符事件的时间点
    private int currentNoteIndex = 0; // 当前处理的音符索引
    private Camera mainCamera;

    void Start()
    {
        // 检查必要组件
        if (example1Prefab == null || example2Prefab == null)
        {
            Debug.LogError("预制体未设置！请在Inspector中设置example1Prefab和example2Prefab");
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("找不到主摄像机！");
            enabled = false;
            return;
        }

        try
        {
            // 从StreamingAssets加载MIDI文件
            string midiPath = Path.Combine(Application.streamingAssetsPath, midiFileName);
            
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
                Debug.LogWarning("创建StreamingAssets文件夹");
            }

            if (!File.Exists(midiPath))
            {
                Debug.LogError($"MIDI文件不存在：{midiPath}\n请确保将MIDI文件放在StreamingAssets文件夹中！");
                enabled = false;
                return;
            }

            // 读取MIDI文件
            byte[] midiBytes = File.ReadAllBytes(midiPath);
            using (MemoryStream memoryStream = new MemoryStream(midiBytes))
            {
                midiFileData = MidiFile.Read(memoryStream);
            }

            // 获取所有音轨
            foreach (var chunk in midiFileData.Chunks)
            {
                if (chunk is TrackChunk trackChunk)
                {
                    // 获取所有事件
                    var timedEvents = trackChunk.GetTimedEvents();

                    // 循环遍历事件
                    foreach (var timedEvent in timedEvents)
                    {
                        // 检查事件类型
                        if (timedEvent.Event is NoteOnEvent)
                        {
                            // 获取当前音符事件的时间并减去偏移量
                            double currentTime = timedEvent.TimeAs<MetricTimeSpan>(midiFileData.GetTempoMap()).TotalSeconds - TIME_OFFSET;
                            noteTimes.Add(currentTime); // 添加到时间点列表
                        }
                    }
                }
            }

            Debug.Log($"成功加载MIDI文件，音符数量：{noteTimes.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载MIDI文件时出错：{e.Message}");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (currentNoteIndex < noteTimes.Count)
        {
            // 检查当前时间是否到达下一个音符时间点
            if (Time.time >= noteTimes[currentNoteIndex])
            {
                // 生成随机数
                int randomNumber = Random.Range(1, 3);
                hitCount++;
                
                // 根据随机数生成不同的示例
                if (randomNumber == 1)
                {
                    SpawnExample1();
                }
                else
                {
                    SpawnExample2();
                }

                // 输出随机数
                Debug.Log("时间点：" + noteTimes[currentNoteIndex] + "，随机数：" + randomNumber + "，hitCount：" + hitCount);

                // 移动到下一个音符时间点
                currentNoteIndex++;
            }
        }
    }

    void SpawnExample1()
    {
        // 获取摄像机视口的右边界
        Vector3 spawnPosition = mainCamera.ViewportToWorldPoint(new Vector3(1, 0.5f, 0));
        spawnPosition.z = 0; // 确保z坐标为0
        spawnPosition.y = spawnHeight; // 设置固定高度

        GameObject example = Instantiate(example1Prefab, spawnPosition, Quaternion.identity);
        ExampleBehavior behavior = example.AddComponent<ExampleBehavior>();
        behavior.Initialize(moveSpeed, true); // true表示这是示例1
    }

    void SpawnExample2()
    {
        // 获取摄像机视口的右边界，使用独立的生成高度
        Vector3 spawnPosition = mainCamera.ViewportToWorldPoint(new Vector3(1, 0.5f, 0));
        spawnPosition.z = 0; // 确保z坐标为0
        spawnPosition.y = spawnHeight2; // 使用示例2的生成高度

        GameObject example = Instantiate(example2Prefab, spawnPosition, Quaternion.identity);
        ExampleBehavior behavior = example.AddComponent<ExampleBehavior>();
        behavior.Initialize(moveSpeed, false); // false表示这是示例2
    }
}

// 新建一个ExampleBehavior类来控制示例的行为
public class ExampleBehavior : MonoBehaviour
{
    private float moveSpeed;
    private bool isExample1;
    private Camera mainCamera;

    public void Initialize(float speed, bool isFirst)
    {
        moveSpeed = speed;
        isExample1 = isFirst;
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 向左移动
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

        // 检查是否超出摄像机视野
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(transform.position);
        if (viewportPoint.x < 0 || viewportPoint.x > 1 ||
            viewportPoint.y < 0 || viewportPoint.y > 1)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 获取MidiNoteGenerator实例并增加分数
            MidiNoteGenerator generator = FindObjectOfType<MidiNoteGenerator>();
            Destroy(gameObject);
            if (generator != null)
            {
                generator.score++;
                Debug.Log("得分：" + generator.score);
            }
        }
    }
}
