using UnityEngine;

// PathManager 的逻辑已合并到 GameManager 和 MapGenerator。
// 保留此脚本以避免场景引用丢失。
public class PathManager : MonoBehaviour
{
    public MapGenerator mapGenerator;

    public void StartJourney()
    {
        if (mapGenerator != null)
            mapGenerator.GenerateMap();
    }
}
