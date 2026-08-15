using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.IO;
using System.Linq;

public class MidiNoteGenerator : MonoBehaviour
{
    [Header("音符预制体")]
    public GameObject example1Prefab; // 下方音符（低音）
    public GameObject example2Prefab; // 上方音符（高音）

    [Header("MIDI设置")]
    public string midiFileName = "missyou.mid";
    [Tooltip("要使用的音轨索引，-1=自动选择音符最多的旋律音轨")]
    public int preferredTrackIndex = -1;

    [Header("生成设置")]
    public float moveSpeed = 10f;
    public float spawnHeight = -2.5f;  // 下方音符高度
    public float spawnHeight2 = 1f;    // 上方音符高度
    [Tooltip("开始前的延迟秒数（等待音乐播放）")]
    public float startDelay = 0f;

    [Header("音符过滤")]
    [Tooltip("两个音符之间的最小间隔（秒），低于此值的音符会被跳过")]
    public float minNoteInterval = 0.15f;
    [Tooltip("音高分界线：高于此值生成上方音符，低于则生成下方音符")]
    public int pitchThreshold = 60; // 60 = C4（中央C）
    [Tooltip("是否自动计算音高分界线（使用该音轨的中位音高）")]
    public bool autoDetectThreshold = true;

    [Header("连续相同方向限制")]
    [Tooltip("同一方向最多连续出现几个音符，超过后强制切换")]
    public int maxConsecutiveSameDirection = 3;

    private MidiFile midiFileData;
    public int hitCount = 0;
    public int score = 0;
    public int missCount = 0;
    public int totalNotes = 0;

    private List<NoteEvent> noteEvents = new List<NoteEvent>();
    private int currentNoteIndex = 0;
    private Camera mainCamera;
    private float gameStartTime;

    // 用于追踪连续同方向
    private int consecutiveSameDir = 0;
    private bool lastWasUp = false;

    void Start()
    {
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

        LoadAndProcessMidi();
        gameStartTime = Time.time + startDelay;

        Debug.Log($"加载完成：筛选后 {noteEvents.Count} 个音符（音高分界线={pitchThreshold}）");
    }

    private void LoadAndProcessMidi()
    {
        try
        {
            string midiPath = Path.Combine(Application.streamingAssetsPath, midiFileName);

            if (!File.Exists(midiPath))
            {
                Debug.LogError($"MIDI文件不存在：{midiPath}");
                enabled = false;
                return;
            }

            byte[] midiBytes = File.ReadAllBytes(midiPath);
            using (MemoryStream ms = new MemoryStream(midiBytes))
            {
                midiFileData = MidiFile.Read(ms);
            }

            var tempoMap = midiFileData.GetTempoMap();

            // === 第一步：选择最佳音轨 ===
            TrackChunk selectedTrack = SelectBestTrack(tempoMap);
            if (selectedTrack == null)
            {
                Debug.LogError("未找到包含音符的音轨！");
                enabled = false;
                return;
            }

            // === 第二步：提取该音轨的所有 NoteOn 事件（带音高） ===
            var rawNotes = new List<NoteEvent>();
            foreach (var timedEvent in selectedTrack.GetTimedEvents())
            {
                if (timedEvent.Event is NoteOnEvent noteOn && noteOn.Velocity > 0)
                {
                    double time = timedEvent.TimeAs<MetricTimeSpan>(tempoMap).TotalSeconds;
                    rawNotes.Add(new NoteEvent
                    {
                        time = time,
                        pitch = noteOn.NoteNumber
                    });
                }
            }

            rawNotes.Sort((a, b) => a.time.CompareTo(b.time));

            if (rawNotes.Count == 0)
            {
                Debug.LogError("选中的音轨没有音符！");
                enabled = false;
                return;
            }

            // === 第三步：自动计算音高分界线 ===
            if (autoDetectThreshold)
            {
                var pitches = rawNotes.Select(n => n.pitch).OrderBy(p => p).ToList();
                pitchThreshold = pitches[pitches.Count / 2]; // 中位数
                Debug.Log($"自动检测音高分界线：{pitchThreshold}（{GetNoteName(pitchThreshold)}）");
            }

            // === 第四步：去重 + 最小间隔过滤 ===
            // 同一时间点只保留一个音符（取最高音，因为旋律通常在最高音）
            noteEvents.Clear();
            double lastAcceptedTime = -999;

            int i = 0;
            while (i < rawNotes.Count)
            {
                // 找出同一时间点的所有音符（容差 0.01 秒）
                int j = i;
                int highestPitch = rawNotes[i].pitch;
                while (j < rawNotes.Count && rawNotes[j].time - rawNotes[i].time < 0.01)
                {
                    if (rawNotes[j].pitch > highestPitch)
                        highestPitch = rawNotes[j].pitch;
                    j++;
                }

                double noteTime = rawNotes[i].time;

                // 最小间隔过滤
                if (noteTime - lastAcceptedTime >= minNoteInterval)
                {
                    noteEvents.Add(new NoteEvent
                    {
                        time = noteTime,
                        pitch = highestPitch
                    });
                    lastAcceptedTime = noteTime;
                }

                i = j;
            }

            totalNotes = noteEvents.Count;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载MIDI文件时出错：{e.Message}\n{e.StackTrace}");
            enabled = false;
        }
    }

    /// <summary>
    /// 选择最佳音轨：优先用户指定，否则自动选择"最可能是旋律"的音轨
    /// 判定标准：排除鼓轨（Channel 10/9），选音符数适中且音域最集中的
    /// </summary>
    private TrackChunk SelectBestTrack(TempoMap tempoMap)
    {
        var tracks = midiFileData.Chunks.OfType<TrackChunk>().ToList();

        if (tracks.Count == 0) return null;

        // 用户手动指定了音轨
        if (preferredTrackIndex >= 0 && preferredTrackIndex < tracks.Count)
        {
            Debug.Log($"使用用户指定的音轨 #{preferredTrackIndex}");
            return tracks[preferredTrackIndex];
        }

        // 自动选择：评分每个音轨
        TrackChunk bestTrack = null;
        float bestScore = -1;

        for (int t = 0; t < tracks.Count; t++)
        {
            var track = tracks[t];
            var notes = new List<int>();
            bool isDrum = false;

            foreach (var timedEvent in track.GetTimedEvents())
            {
                if (timedEvent.Event is NoteOnEvent noteOn && noteOn.Velocity > 0)
                {
                    // Channel 9 (0-indexed) = 鼓轨，跳过
                    if (noteOn.Channel == 9)
                    {
                        isDrum = true;
                        break;
                    }
                    notes.Add(noteOn.NoteNumber);
                }
            }

            if (isDrum || notes.Count < 10) continue;

            // 评分：音域集中 + 音符数量适中（不要太少也不要太密）
            int minPitch = notes.Min();
            int maxPitch = notes.Max();
            int range = maxPitch - minPitch;

            // 旋律音轨特征：音域在20-40范围内，音符数量在50-500之间
            float rangeScore = range > 0 ? Mathf.Clamp01(1f - Mathf.Abs(range - 30) / 30f) : 0;
            float countScore = Mathf.Clamp01(Mathf.Min(notes.Count, 500f) / 500f);
            float score = rangeScore * 0.6f + countScore * 0.4f;

            Debug.Log($"音轨 #{t}: {notes.Count} 音符, 音域 {minPitch}-{maxPitch} (范围{range}), 评分={score:F2}");

            if (score > bestScore)
            {
                bestScore = score;
                bestTrack = track;
            }
        }

        if (bestTrack == null && tracks.Count > 0)
        {
            // 兜底：随便选一个有音符的
            bestTrack = tracks.FirstOrDefault(t =>
                t.GetTimedEvents().Any(e => e.Event is NoteOnEvent));
        }

        return bestTrack;
    }

    void Update()
    {
        if (currentNoteIndex >= noteEvents.Count) return;

        float currentTime = Time.time - gameStartTime;

        // 可能需要一帧跳过多个已过时的音符（防止卡顿后堆积）
        while (currentNoteIndex < noteEvents.Count && currentTime >= noteEvents[currentNoteIndex].time)
        {
            var note = noteEvents[currentNoteIndex];
            bool isUp = note.pitch >= pitchThreshold;

            // 防止同方向连续过多
            if (isUp == lastWasUp)
            {
                consecutiveSameDir++;
                if (consecutiveSameDir >= maxConsecutiveSameDirection)
                {
                    isUp = !isUp; // 强制切换方向
                    consecutiveSameDir = 0;
                }
            }
            else
            {
                consecutiveSameDir = 0;
            }
            lastWasUp = isUp;

            if (isUp)
                SpawnNote(spawnHeight2, example2Prefab);
            else
                SpawnNote(spawnHeight, example1Prefab);

            hitCount++;
            currentNoteIndex++;
        }
    }

    private void SpawnNote(float height, GameObject prefab)
    {
        Vector3 spawnPosition = mainCamera.ViewportToWorldPoint(new Vector3(1.05f, 0.5f, 0));
        spawnPosition.z = 0;
        spawnPosition.y = height;

        GameObject note = Instantiate(prefab, spawnPosition, Quaternion.identity);
        ExampleBehavior behavior = note.AddComponent<ExampleBehavior>();
        behavior.Initialize(moveSpeed, height == spawnHeight, this);
    }

    public void OnNoteMissed()
    {
        missCount++;
    }

    private string GetNoteName(int midiNote)
    {
        string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int octave = (midiNote / 12) - 1;
        string name = names[midiNote % 12];
        return $"{name}{octave}";
    }
}

public struct NoteEvent
{
    public double time;
    public int pitch;
}

public class ExampleBehavior : MonoBehaviour
{
    private float moveSpeed;
    private bool isExample1;
    private Camera mainCamera;
    private MidiNoteGenerator generator;
    private bool scored = false;

    public void Initialize(float speed, bool isFirst, MidiNoteGenerator gen)
    {
        moveSpeed = speed;
        isExample1 = isFirst;
        mainCamera = Camera.main;
        generator = gen;
    }

    void Update()
    {
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);
        if (vp.x < -0.05f)
        {
            // 超出屏幕左侧 = 玩家没接到
            if (!scored && generator != null)
            {
                generator.OnNoteMissed();
            }
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            scored = true;
            if (generator != null)
            {
                generator.score++;
            }
            Destroy(gameObject);
        }
    }
}
