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
    [SerializeField] private Vector2 startPosition; // 修改为 RectTransform 类型
    [SerializeField] private Vector2 endPosition; // 修改为 RectTransform 类型
    [SerializeField] private float moveDuration = 1f; // 新增：移动时长

    private void Start()
    {
        HideAllObjects();
    }

    public void Update()
    {
        
    }

    public void OnShowBeatUI()
    {
        
    }
    private void OnDestroy()
    {
    }

    public void UpdateBeatUI(int beatIndex, BeatType beatType)
    {  
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

    public void ShowObjectBasedOnIndex(int beatIndex, float assumedTime = 0f)
    {
        int indexToShow = beatIndex % 3;

        if (indexToShow >= 0 && indexToShow < objectsToShow.Length && objectsToShow[indexToShow] != null)
        {
            // 原对象不显示
            objectsToShow[indexToShow].SetActive(false);
            StartCoroutine(MoveAndDestroyObject(indexToShow, startPosition, endPosition, moveDuration, assumedTime));
        }
    }

    private IEnumerator MoveAndDestroyObject(int indexToShow, Vector2 startPos, Vector2 endPos, float duration, float assumedTime = 0f)
    {
        /*
         *  0 ~ getbeatlength(3)-duration   empty
         *  getbeatlength(3)-duration ~ getlength(3)
         */

        indexToShow = (indexToShow + 1) % 3;
        float beatLength = BeatMgr.Instance.GetBeatLength(3) / 1000.0f;
        float createTime = beatLength - duration;
        
        if (assumedTime < beatLength)
        {
            float elapsedTime = assumedTime;
            while (elapsedTime < createTime)
            {   
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // 复制对象
            GameObject objToDestroy = Instantiate(objectsToShow[indexToShow], objectsToShow[indexToShow].transform.parent);
            RectTransform target = objToDestroy.GetComponent<RectTransform>();
            if (target != null)
            {
                objToDestroy.SetActive(true);;
            }
            
            while (elapsedTime < beatLength)
            {
                target.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsedTime / duration);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            target.anchoredPosition = endPos;
            Destroy(objToDestroy);
        }
    }
}