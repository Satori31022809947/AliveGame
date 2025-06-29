using System;
using System.Collections;
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
    [SerializeField] private PlayerController playerController;
    [SerializeField] private float DelayStartCheckTime = 0.1f;
    [SerializeField] private float EarlyStopCheckTime = 0.1f;

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
                item.index = t.index + 2;
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

    public long GetBeatTime(long index)
    {
        return startTime + index * 60000 / bpm;
    }

    public long GetBeatLength(long count)
    {
        return count * 60000 / bpm;
    }

    public void SetBeatStartTime(long time)
    {
        startTime = time + baseTime;
        beatIndex = 0;
        beatUI.UpdateBeatUI(beatIndex, BeatType.None);
        for (int i = 0; i < 3; i++)
        {
            beatUI.ShowObjectBasedOnIndex(i, GetBeatLength(3 - i)/1000.0f);
        }
        beatUI.OnShowBeatUI();
        Enable();
    }

    private void OnBeat()
    {
        // 每拍调用的函数，内容留空，可按需实现
        // 一拍结束时候的处理
        NoiseControlMgr.Instance.OnBeatFinish(GetBeatType(beatIndex));
        beatIndex++;
        if (beatIndex >= GameMgr.Instance.BeatLimit)
        {
            GameMgr.Instance.Lose();
            return;
        }
        // 一拍开始时候的处理
        BeatType beatType = GetBeatType(beatIndex);
        switch (beatType)
        {
            case BeatType.Dangerous:
                Debug.Log("BeatMgr: OnBeat: Dangerous");
                StartCoroutine(DelayStartCheck());
                LightMgr.Instance.SetDangerousLight();
                break;
            case BeatType.Warning:
                Debug.Log("BeatMgr: OnBeat: Warning");
                LightMgr.Instance.StartWarningFlash(GetBeatLength(1)/1000.0f);
                break;
            case BeatType.None:
                playerController.detectDangerous = false;
                LightMgr.Instance.ResetLight();
                break;
        }   
        beatUI.UpdateBeatUI(beatIndex, beatType);
        beatType = GetBeatType(beatIndex + 1);
        if (beatType == BeatType.Warning)
        {
            StartCoroutine(DelayPlayWarningSound());
        }
        if (beatType != BeatType.Dangerous)
        {
            StartCoroutine(EarlyStopCheck());
        }
    }

    private IEnumerator DelayPlayWarningSound()
    {
        yield return new WaitForSeconds(0.15f);
        AudioMgr.Instance.PlaySoundEffect(SoundEffectType.Warning, 2f);
    }
    private IEnumerator DelayStartCheck()
    {
        yield return new WaitForSeconds(DelayStartCheckTime);
        playerController.detectDangerous = true;
        Debug.Log("BeatMgr: OnBeat: StartCheckDangerous");
    }

    private IEnumerator EarlyStopCheck()
    {
        float waitTime = GetBeatLength(1) / 1000.0f;
        yield return new WaitForSeconds(waitTime - EarlyStopCheckTime);
        if (playerController.detectDangerous)
        {
            playerController.detectDangerous = false;
            Debug.Log("BeatMgr: OnBeat: EndCheckDangerous");
        }
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
        beatUI.gameObject.SetActive(false);
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

    public int GetNearestBeat(long curTime)
    {
        
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