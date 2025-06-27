using UnityEngine;
using UnityEngine.UI;

public class BeatUI : MonoBehaviour
{
    [SerializeField] private Text beatIndexText; // 用于显示 BeatIndex 的 Text 组件
    [SerializeField] private GameObject[] objectsToShow; // 三个需要根据 BeatIndex 显示的 GameObject

    private void Start()
    {
    }

    public void Update()
    {
        
    }

    private void OnDestroy()
    {
    }

    public void UpdateBeatUI(int beatIndex)
    {
        HideAllObjects();
        beatIndexText.text = "Beat Index: " + beatIndex;
        ShowObjectBasedOnIndex(beatIndex);
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