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

    [SerializeField] public int noiseValue = 0; // 当前噪声值
    [SerializeField] public int noiseThreshold = 100; // 噪声阈值
    [SerializeField] public int noiseAddValue = 100; // 噪声阈值
    [SerializeField] public int noiseDecreaseSpeed = 5; // 噪声下降速度

    public Action OnNoiseThresholdReached; // 噪声达到阈值时触发的事件
    public Action OnNoiseChanged; // 噪声值改变时触发的事件


    private void Update()
    {
    }


    public void OnBeatFinish(BeatType type)
    {
        if (type != BeatType.Dangerous)
        {

            noiseValue -= noiseDecreaseSpeed;
            if (noiseValue <= 0)
            {
                noiseValue = 0;
            }
            
            OnNoiseChanged?.Invoke();
        }
    }
    
    
    private void OnDestroy()
    {
        instance = null;
    }

    /// <summary>
    /// 修改噪声值
    /// </summary>
    public void AddNoise()
    {
        noiseValue += noiseAddValue;
        if (noiseValue >= noiseThreshold)
        {
            Debug.Log("Dead");
            GameMgr.Instance.Lose();
        }
        else if (noiseValue <= 0)
        {
            noiseValue = 0;
            OnNoiseChanged?.Invoke();
        }
        OnNoiseChanged?.Invoke();
    }
}