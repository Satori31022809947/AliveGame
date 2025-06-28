using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector3 offset; // 相机相对于玩家的偏移量
    
    [Header("屏幕震动设置")]
    [SerializeField] private float shakeDuration = 0.2f; // 震动持续时间
    [SerializeField] private float shakeMagnitude = 0.2f; // 震动强度
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // 震动衰减曲线
    
    private Vector3 originalOffset;
    private bool isShaking = false;

    // Start is called before the first frame update
    void Start()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }
        
        // 保存原始偏移量
        originalOffset = offset;
    }

    // Update is called once per frame
    void Update()
    {
        if (playerTransform != null)
        {
            Vector3 newPosition = playerTransform.position + offset;
            transform.position = newPosition;
        }
    }
    
    /// <summary>
    /// 开始屏幕震动
    /// </summary>
    /// <param name="duration">震动持续时间，如果为0则使用默认值</param>
    /// <param name="magnitude">震动强度，如果为0则使用默认值</param>
    public void StartShake(float duration = 0f, float magnitude = 0f)
    {
        if (isShaking)
        {
            StopAllCoroutines(); // 停止之前的震动
        }
        
        float actualDuration = duration > 0 ? duration : shakeDuration;
        float actualMagnitude = magnitude > 0 ? magnitude : shakeMagnitude;
        
        StartCoroutine(ShakeCoroutine(actualDuration, actualMagnitude));
    }
    
    /// <summary>
    /// 震动协程
    /// </summary>
    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        isShaking = true;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / duration;
            
            // 使用动画曲线计算当前震动强度
            float currentMagnitude = magnitude * shakeCurve.Evaluate(normalizedTime);
            
            // 生成随机震动偏移
            Vector3 shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * currentMagnitude,
                Random.Range(-1f, 1f) * currentMagnitude,
                Random.Range(-1f, 1f) * currentMagnitude
            );
            
            // 应用震动偏移到相机偏移量
            offset = originalOffset + shakeOffset;
            
            yield return null;
        }
        
        // 震动结束，恢复原始偏移量
        offset = originalOffset;
        isShaking = false;
    }
    
    /// <summary>
    /// 停止震动
    /// </summary>
    public void StopShake()
    {
        if (isShaking)
        {
            StopAllCoroutines();
            offset = originalOffset;
            isShaking = false;
        }
    }
    
    /// <summary>
    /// 设置震动参数
    /// </summary>
    public void SetShakeSettings(float duration, float magnitude)
    {
        shakeDuration = Mathf.Max(0f, duration);
        shakeMagnitude = Mathf.Max(0f, magnitude);
    }
    
    /// <summary>
    /// 获取当前是否正在震动
    /// </summary>
    public bool IsShaking()
    {
        return isShaking;
    }
}
