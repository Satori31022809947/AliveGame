using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target; // 目标物体
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f; // 旋转速度
    [SerializeField] private bool smoothRotation = true; // 是否平滑旋转

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            RotateTowardsTarget();
        }
    }

    private void RotateTowardsTarget()
    {
        // 计算从当前位置到目标位置的方向向量
        Vector3 direction = target.position - transform.position;
        
        // 将方向向量投影到XZ平面上（消除Y轴分量）
        direction.y = 0;
        
        // 如果方向向量不为零，则进行旋转
        if (direction != Vector3.zero)
        {
            // 计算目标旋转
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            if (smoothRotation)
            {
                // 平滑旋转
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                // 直接旋转
                transform.rotation = targetRotation;
            }
        }
    }

    // 在编辑器中显示指向目标的线条（仅在Scene视图中可见）
    private void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = Color.red;
            Vector3 direction = target.position - transform.position;
            direction.y = 0; // 只在XZ平面上显示
            Gizmos.DrawRay(transform.position, direction.normalized * 2f);
        }
    }
}
