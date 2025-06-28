using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class BeatSequenceItem
{
    public int index;
    public BeatType type;
}

public enum BeatType
{
    None,
    Dangerous,
    Warning,
}



public class BeatMgr : MonoBehaviour
{
    private static BeatMgr instance;
    public static BeatMgr Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BeatMgr>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("BeatMgr");
                    instance = obj.AddComponent<BeatMgr>();
                }
            }
            return instance;
        }
    }

    private bool m_enable = false;  // 默认禁用
    [SerializeField] private int bpm = 108;
    [SerializeField] private long startTime = 0;         // 节拍开始初始时间 (毫秒)
    [SerializeField] private long baseTime = 0;         // 节拍开始初始时间 (毫秒)
    [SerializeField] private BeatUI beatUI;             // 节拍器UI
    [SerializeField] private string beatConfigPath = "BeatConfig.json";
    private List<BeatSequenceItem> BeatSequence = new List<BeatSequenceItem>();
    private Dictionary<int, BeatType> BeatMap =  new Dictionary<int, BeatType>();

    private int beatIndex;

    private void Start()
    {
        LoadBeatConfig();
        foreach (var t in BeatSequence)
        {
            BeatMap.Add(t.index, t.type);
        }
    }

    private void LoadBeatConfig()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, beatConfigPath);
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            BeatConfig config = JsonUtility.FromJson<BeatConfig>(json);
            
            foreach (var t in config.beatSequences)
            {
                Debug.Log(t.index + "  " + t.type);
                BeatSequenceItem item = new BeatSequenceItem();
                item.index = t.index;
                item.type = ParseBeatType(t.type);
                BeatSequence.Add(item);
            }
        }
        else
        {
            Debug.LogError("Beat config file not found: " + filePath);
        }
    }

    private void Update()
    {
        if (m_enable)
        {
            long curTime = DateTime.UtcNow.ToUniversalTime().Ticks / 10000;
            if (curTime >= GetBeatTime(beatIndex + 1))
            {
                OnBeat();
            }
        }
    }

    private long GetBeatTime(long index)
    {
        return startTime + index * 60000 / bpm;
    }

    public void SetBeatStartTime(long time)
    {
        startTime = time + baseTime;
        beatIndex = 0;
        beatUI.UpdateBeatUI(beatIndex, BeatType.None);
        Enable();
    }

    private void OnBeat()
    {
        // 每拍调用的函数，内容留空，可按需实现
        Debug.Log("BeatMgr: OnBeat");
        // 一拍结束时候的处理
        NoiseControlMgr.Instance.OnBeatFinish(GetBeatType(beatIndex));
        beatIndex++;
        // 一拍开始时候的处理
        BeatType beatType = GetBeatType(beatIndex);
        switch (beatType)
        {
            case BeatType.Dangerous:
                Debug.Log("BeatMgr: OnBeat: Dangerous");
                break;
            case BeatType.Warning:
                Debug.Log("BeatMgr: OnBeat: Warning");
                break;
        }   
        beatUI.UpdateBeatUI(beatIndex, beatType);
    }

    public BeatType GetBeatType(int beatIndex)
    {
        if (BeatMap.ContainsKey(beatIndex))
        {
            return BeatMap[beatIndex];
        }
        return BeatType.None;
    }
    
    public void SetBPM(int newBpm)
    {
        bpm = newBpm;
    }
    

    /// <summary>
    /// 启用输入检测
    /// </summary>
    public void Enable()
    {
        m_enable = true;
        Debug.Log("BeatMgr: 节拍器已启用");
    }

    /// <summary>
    /// 禁用输入检测
    /// </summary>
    public void Disable()
    {
        m_enable = false;
        Debug.Log("BeatMgr: 节拍器已禁用");
    }
    
    /// <summary>
    /// 检查输入是否启用
    /// </summary>
    public bool IsEnabled()
    {
        return m_enable;
    }

    public int GetBeatIndex()
    {
        return beatIndex;
    }

    public int GetNearestBeat()
    {
        long curTime = DateTime.UtcNow.ToUniversalTime().Ticks / 10000;
        
        long curBeatTime = GetBeatTime(beatIndex);
        long nextBeatTime = GetBeatTime(beatIndex + 1);

        if (Mathf.Abs(curBeatTime - curTime) <= Mathf.Abs(nextBeatTime - curTime))
        {
            return beatIndex;
        }
        else
        {
            return beatIndex + 1;
        }
    }
    
    /// <summary>
    /// 解析节拍类型
    /// </summary>
    private BeatType ParseBeatType(string beatTypeString)
    {
        if (System.Enum.TryParse<BeatType>(beatTypeString, out BeatType result))
        {
            return result;
        }
        
        Debug.LogWarning($"BeatMgr: 未知的道具类型 {beatTypeString}，默认为 None");
        return BeatType.None;
    }
}

public class BeatConfig
{
    public BeatConfigItem[] beatSequences;
}

[System.Serializable]
public class BeatConfigItem
{
    public int index;
    public string type;
}