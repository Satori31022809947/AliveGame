using System.Collections;
using UnityEngine;

public class LightMgr : MonoBehaviour
{
    private static LightMgr instance;
    public static LightMgr Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<LightMgr>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("LightMgr");
                    instance = obj.AddComponent<LightMgr>();
                }
            }
            return instance;
        }
    }

    public Light sceneLight;
    public float warningFlashDuration = 0.5f;
    private Color warningColor = new Color(1f, 1f, 0f);
    private Color dangerousColor = new Color(0.5f, 0f, 0f);
    private Color NormalColor = new Color(1f, 1f, 1f);
    private int IsFlashing = 0;
    
    public void StartWarningFlash(float totalFlashTime = 0f)
    {
        IsFlashing += 1;
        StartCoroutine(WarningFlash(IsFlashing, totalFlashTime));
    }

    private IEnumerator WarningFlash(int flashIndex,float totalFlashTime)
    {
        float elapsedTime = 0f;
        Color originalColor = NormalColor;

        while (elapsedTime < totalFlashTime && IsFlashing == flashIndex)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / warningFlashDuration;
            float intensity = Mathf.Sin(progress * Mathf.PI);
            sceneLight.color = Color.Lerp(originalColor, warningColor, intensity);
            yield return null;
        }
    }

    public void SetDangerousLight()
    {
        IsFlashing += 1;
        sceneLight.color = dangerousColor;
    }

    public void ResetLight()
    {
        IsFlashing += 1;
        sceneLight.color = NormalColor;
    }
}