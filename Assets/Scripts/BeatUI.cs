using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BeatUI : MonoBehaviour
{
    [SerializeField] private GameObject[] objectsToShow; // 三个需要根据 BeatIndex 显示的 GameObject

    [SerializeField] private GameObject DangerEffect;
    [SerializeField] private GameObject WarningEffect;
    [SerializeField] private GameObject EndPos;
    [SerializeField] private Text beatRatioText; // 新增：用于显示比值的 Text 组件
    [SerializeField] private Slider beatProgressSlider; // 新增：用于显示条状进度的 Slider 组件
    [SerializeField] private Vector2 startPosition; // 修改为 RectTransform 类型
    [SerializeField] private Vector2 endPosition; // 修改为 RectTransform 类型
    [SerializeField] private float moveDuration = 1f; // 新增：移动时长

    private void Start()
    {
        if (beatRatioText != null) {
            beatRatioText.gameObject.SetActive(false);
        }
        if (beatProgressSlider != null) {
            beatProgressSlider.gameObject.SetActive(false);
        }
        HideAllObjects();
    }

    public void Update()
    {
        
    }

    private void OnDestroy()
    {
    }

    public void UpdateBeatUI(int beatIndex, BeatType beatType)
    {
        
        if (beatRatioText != null) {
            beatRatioText.gameObject.SetActive(true);
            beatRatioText.text = beatIndex + "/" + GameMgr.Instance.BeatLimit;
        }
        if (beatProgressSlider != null) {
            beatProgressSlider.gameObject.SetActive(true);
            float ratio = (float)beatIndex / GameMgr.Instance.BeatLimit;
            beatProgressSlider.value = ratio;
        }

        if (EndPos != null)
        {
            EndPos.SetActive(true);
        }
        ShowObjectBasedOnIndex(beatIndex);
        ShowEffect(beatType);
    }

    private void ShowEffect(BeatType beatType)
    {
        if (WarningEffect != null)
        {
            WarningEffect.SetActive(false);
        }
        if (DangerEffect != null)
        {
            DangerEffect.SetActive(false);
        }
        
        switch (beatType)
        {
            
            case BeatType.Warning:
                if (WarningEffect != null)
                {
                    WarningEffect.SetActive(true);
                }
                break;
            case BeatType.Dangerous:
                if (DangerEffect != null)
                {
                    DangerEffect.SetActive(true);
                }
                break;
        }
    }

    private void HideAllObjects()
    {
        foreach (var obj in objectsToShow)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        if (WarningEffect != null)
        {
            WarningEffect.SetActive(false);
        }
        if (DangerEffect != null)
        {
            DangerEffect.SetActive(false);
        }
        EndPos.SetActive(false);
    }

    private void ShowObjectBasedOnIndex(int beatIndex)
    {
        int indexToShow = beatIndex % 3;

        if (indexToShow >= 0 && indexToShow < objectsToShow.Length && objectsToShow[indexToShow] != null)
        {
            var targetRect = objectsToShow[indexToShow].GetComponent<RectTransform>();
            if (targetRect != null && startPosition != null && endPosition != null)
            {
                objectsToShow[indexToShow].SetActive(true);
                targetRect.anchoredPosition = startPosition;
                StartCoroutine(MoveObject(targetRect, startPosition, endPosition, moveDuration));
            }
        }
    }

    private IEnumerator MoveObject(RectTransform target, Vector2 startPos, Vector2 endPos, float duration)
    {
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            target.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        target.anchoredPosition = endPos;
    }
}