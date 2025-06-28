using UnityEngine;
using UnityEngine.UI;

public class NoiseUI : MonoBehaviour
{
    [SerializeField] private Text noiseText;
    [SerializeField] private Slider noiseProgressBar;

    private void OnEnable()
    {
        NoiseControlMgr.Instance.OnNoiseChanged += UpdateUI;
        GameMgr.Instance.OnGameStart += OnGameStart;
        GameMgr.Instance.OnGameEnd += OnGameEnd;
    }

    private void OnDisable()
    {
        NoiseControlMgr.Instance.OnNoiseChanged -= UpdateUI;
        GameMgr.Instance.OnGameStart -= OnGameStart;
        GameMgr.Instance.OnGameEnd -= OnGameEnd;
    }

    private void Start()
    {
    }

    private void OnGameStart()
    {
        noiseText.gameObject.SetActive(true);
        noiseProgressBar.gameObject.SetActive(true);
    }


    private void OnGameEnd()
    {
        noiseText.gameObject.SetActive(false);
        noiseProgressBar.gameObject.SetActive(false);
    }


    private void UpdateUI()
    {
        float currentNoise = NoiseControlMgr.Instance.noiseValue;
        float threshold = NoiseControlMgr.Instance.noiseThreshold;

        noiseText.text = "Noise:" + currentNoise + " / " + threshold;
        noiseProgressBar.value = currentNoise / threshold;
    }
}