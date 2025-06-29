using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmissionController : MonoBehaviour
{
    [Header("呼吸灯设置")]
    [SerializeField] private float breathingSpeed = 2f; // 呼吸速度
    [SerializeField] private Color minEmissionColor = Color.black; // 最小发光颜色 (0,0,0)
    [SerializeField] private Color maxEmissionColor = Color.white; // 最大发光颜色 (1,1,1)
    
    private Renderer objectRenderer;
    private Material material;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    
    // Start is called before the first frame update
    void Start()
    {
        // 获取挂载物体的Renderer组件
        objectRenderer = GetComponent<Renderer>();
        
        if (objectRenderer == null)
        {
            Debug.LogError("EmissionController: 未找到Renderer组件！");
            enabled = false;
            return;
        }
        
        // 获取材质
        material = objectRenderer.material;
        
        // 确保材质支持Emission
        if (!material.HasProperty("_EmissionColor"))
        {
            Debug.LogWarning("EmissionController: 材质不支持Emission属性！");
        }
        
        // 启用材质的Emission
        material.EnableKeyword("_EMISSION");
    }

    // Update is called once per frame
    void Update()
    {
        if (material == null) return;
        
        // 使用PingPong函数实现来回变化的效果
        // PingPong在0到1之间来回变化
        float breathingValue = Mathf.PingPong(Time.time * breathingSpeed, 1f);
        
        // 在最小和最大颜色之间插值
        Color currentEmissionColor = Color.Lerp(minEmissionColor, maxEmissionColor, breathingValue);
        
        // 设置材质的发光颜色
        material.SetColor(EmissionColor, currentEmissionColor);
    }
    
    void OnDestroy()
    {
        // 清理材质实例，避免内存泄漏
        if (material != null)
        {
            DestroyImmediate(material);
        }
    }
}
