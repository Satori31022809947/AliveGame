using System;
using UnityEngine;

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
    
    private int beatIndex;

    private void Start()
    {
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
        Enable();
    }

    private void OnBeat()
    {
        // 每拍调用的函数，内容留空，可按需实现
        Debug.Log("BeatMgr: OnBeat");
        beatIndex++;
        beatUI.UpdateBeatUI(beatIndex);
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