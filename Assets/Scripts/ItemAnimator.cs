using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 道具动画控制器 - 负责道具的浮动和旋转动画
/// </summary>
public class ItemAnimator : MonoBehaviour
{
    [Header("动画设置")]
    [SerializeField] private float floatHeight = 0.2f;      // 浮动高度
    [SerializeField] private float floatSpeed = 2f;         // 浮动速度
    [SerializeField] private float rotateSpeed = 50f;       // 旋转速度
    
    private Vector3 startPosition;
    private float randomOffset;
    
    void Start()
    {
        // 记录初始位置
        startPosition = transform.position;
        
        // 添加随机偏移，避免所有道具同步动画
        randomOffset = Random.Range(0f, 2f * Mathf.PI);
    }
    
    void Update()
    {
        // 浮动动画
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed + randomOffset) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        
        // 旋转动画
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// 设置动画参数
    /// </summary>
    public void SetAnimationParams(float height, float floatSpd, float rotateSpd)
    {
        floatHeight = height;
        floatSpeed = floatSpd;
        rotateSpeed = rotateSpd;
    }
    
    /// <summary>
    /// 停止动画
    /// </summary>
    public void StopAnimation()
    {
        enabled = false;
    }
    
    /// <summary>
    /// 重新开始动画
    /// </summary>
    public void StartAnimation()
    {
        enabled = true;
        startPosition = transform.position;
    }
} 