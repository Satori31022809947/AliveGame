using UnityEngine;
using UnityEngine.UI;

public class NoiseUI : MonoBehaviour
{
    [SerializeField] private Text noiseText;
    [SerializeField] private Slider noiseProgressBar;

    private void OnEnable()
    {
        NoiseControlMgr.Instance.OnNoiseChanged += UpdateUI;
    }

    private void OnDisable()
    {
        NoiseControlMgr.Instance.OnNoiseChanged -= UpdateUI;
    }

    private void Start()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        float currentNoise = NoiseControlMgr.Instance.noiseValue;
        float threshold = NoiseControlMgr.Instance.noiseThreshold;

        noiseText.text = "Noise:" + currentNoise + " / " + threshold;
        noiseProgressBar.value = currentNoise / threshold;
    }
}