using System;
using System.Collections.Generic;
using System.IO;
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
    [SerializeField] private BeatUI beatUI;             // 节拍器UI
    [SerializeField] public List<BeatSequenceItem> BeatSequence = new List<BeatSequenceItem>();

    private Dictionary<int, BeatType> BeatMap =  new Dictionary<int, BeatType>();

    private int beatIndex;

    private void Start()
    {
        foreach (var t in BeatSequence)
        {
            BeatMap.Add(t.index, t.type);
        }
    }

    private void Update()
    {
        if (m_enable)
        {
            long curTime = DateTime.UtcNow.ToUniversalTime().Ticks / 10000;
            if (curTime >= startTime + (beatIndex + 1) * (60000 / bpm))
            {
                OnBeat();
            }
        }
    }

    public void SetBeatStartTime(long time)
    {
        startTime = time;
        beatIndex = 0;
        beatUI.UpdateBeatUI(beatIndex);
        Enable();
    }

    private void OnBeat()
    {
        // 每拍调用的函数，内容留空，可按需实现
        Debug.Log("BeatMgr: OnBeat");
        beatIndex++;
        beatUI.UpdateBeatUI(beatIndex);
        if (BeatMap.ContainsKey(beatIndex))
        {
            switch (BeatMap[beatIndex])
            {
                case BeatType.Dangerous:
                    Debug.Log("BeatMgr: OnBeat: Dangerous");
                    break;
            }   
        }
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

}