using UnityEngine;
using UnityEngine.UI;

public class BeatUI : MonoBehaviour
{
    [SerializeField] private Text beatIndexText; // 用于显示 BeatIndex 的 Text 组件
    [SerializeField] private GameObject[] objectsToShow; // 三个需要根据 BeatIndex 显示的 GameObject

    [SerializeField] private GameObject DangerEffect;
    [SerializeField] private GameObject WarningEffect;
    
    private void Start()
    {
        beatIndexText.gameObject.SetActive(false);
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
        
        beatIndexText.gameObject.SetActive(true);
        HideAllObjects();
        beatIndexText.text = "Beat Index: " + beatIndex;
        ShowObjectBasedOnIndex(beatIndex);
        ShowEffect(beatType);
    }

    private void ShowEffect(BeatType beatType)
    {
        
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
    }

    private void ShowObjectBasedOnIndex(int beatIndex)
    {
        HideAllObjects();
        int indexToShow = beatIndex % 3;

        if (indexToShow >= 0 && indexToShow < objectsToShow.Length && objectsToShow[indexToShow] != null)
        {
            objectsToShow[indexToShow].SetActive(true);
        }
    }
}