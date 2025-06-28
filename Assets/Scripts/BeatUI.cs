using UnityEngine;
using UnityEngine.UI;

public class BeatUI : MonoBehaviour
{
    [SerializeField] private GameObject[] objectsToShow; // 三个需要根据 BeatIndex 显示的 GameObject

    [SerializeField] private GameObject DangerEffect;
    [SerializeField] private GameObject WarningEffect;
    [SerializeField] private Text beatRatioText; // 新增：用于显示比值的 Text 组件
    [SerializeField] private Slider beatProgressSlider; // 新增：用于显示条状进度的 Slider 组件

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
        HideAllObjects();
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