using UnityEngine;
using System;

public class NoiseControlMgr : MonoBehaviour
{
    private static NoiseControlMgr instance;
    public static NoiseControlMgr Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NoiseControlMgr>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("NoiseControlMgr");
                    instance = obj.AddComponent<NoiseControlMgr>();
                }
            }
            return instance;
        }
    }

    [SerializeField] private int noiseValue = 0; // 当前噪声值
    [SerializeField] private int noiseThreshold = 100; // 噪声阈值
    [SerializeField] private int noiseDecreaseSpeed = 5; // 噪声下降速度

    public Action OnNoiseThresholdReached; // 噪声达到阈值时触发的事件

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
    }


    public void OnBeatFinish(BeatType type)
    {
        if (type != BeatType.Dangerous)
        {
            AddNoise(-noiseDecreaseSpeed);
        }
    }
    
    
    /// <summary>
    /// 修改噪声值
    /// </summary>
    /// <param name="value">要增加的值</param>
    public void AddNoise(int value)
    {
        noiseValue += value;
        if (noiseValue >= noiseThreshold)
        {
            Debug.Log("Dead");
        }
        else if (noiseValue <= 0)
        {
            noiseValue = 0;
        }

        if (noiseValue > 0)
        {
            Debug.Log($"NoiseValue: {noiseValue}");
        }
    }

    /// <summary>
    /// 获取当前噪声值
    /// </summary>
    /// <returns>当前噪声值</returns>
    public float GetCurrentNoiseValue()
    {
        return noiseValue;
    }
}