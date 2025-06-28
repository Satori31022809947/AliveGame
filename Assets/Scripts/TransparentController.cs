using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransparentController : MonoBehaviour
{
    private Renderer objectRenderer;
    private Material material;
    private Camera mainCamera;
    
    // Start is called before the first frame update
    void Start()
    {
        // 获取同层级的renderer
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            // 创建材质实例以避免修改共享材质
            material = objectRenderer.material;
        }
        
        // 获取主摄像机
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (objectRenderer == null || material == null || mainCamera == null)
            return;
            
        // 获取摄像机的z坐标
        float cameraZ = mainCamera.transform.position.z;
        
        // 计算距离：自己的z坐标 - 摄像机的z坐标，取最大值(0, 距离)
        float distance = Mathf.Max(0, transform.position.z - cameraZ);
        
        // 计算alpha值
        float alpha;
        if (distance >= 20f)
        {
            alpha = 1f;
        }
        else if (distance <= 0f)
        {
            alpha = 0f;
        }
        else
        {
            // 在0到1之间均匀变化
            alpha = distance / 20f;
        }
        
        // 修改material的basecolor.alpha
        Color baseColor = material.color;
        baseColor.a = alpha;
        material.color = baseColor;
    }
}
