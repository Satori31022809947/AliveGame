using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.NetworkInformation;

public enum InputType
{
    None,
    Up,
    Down,
    Left,
    Right,
    Interact,
}

public class InputMgr : MonoBehaviour
{
    private static InputMgr instance;
    public static InputMgr Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<InputMgr>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("InputMgr");
                    instance = obj.AddComponent<InputMgr>();
                }
            }
            return instance;
        }
    }

    private bool m_enable = false; // 默认不启用输入

    // 事件声明 - 当检测到输入时触发
    public static event Action<InputType> OnInputDetected;
    
    // 具体方向移动事件 - 方便订阅特定方向
    public static event Action OnMoveUp;
    public static event Action OnMoveDown;
    public static event Action OnMoveLeft;
    public static event Action OnMoveRight;
    public static event Action OnInteract;
    
    public static event Action OnInteractFailed;

    public static event Action OnNotPerfectInput;
    
    // 输入状态记录
    private InputType lastInput = InputType.None;
    private int lastInputBeat = -1; // 记录上一次input的时候是哪一拍

    [SerializeField] private long leftEps = 200;
    [SerializeField] private long rightEps = 200;
    [SerializeField] private GameObject PerfectUI;
    [SerializeField] private GameObject NotPerfectUI;
    [SerializeField] private float UIRemainDuration = 0.2f;
    [SerializeField] private float UIFadeDuration = 0.2f;

    private float UIRemainingTime;
    private Vector3 initialUIPosition;
    private CanvasGroup perfectUICanvasGroup;
    private CanvasGroup notPerfectUICanvasGroup;
    
    

    void Start()
    {
        Debug.Log("InputMgr: 输入管理器已启动");
        if (PerfectUI != null)
        {
            initialUIPosition = PerfectUI.transform.localPosition;
            perfectUICanvasGroup = PerfectUI.AddComponent<CanvasGroup>();
        }
        if (NotPerfectUI != null)
        {
            notPerfectUICanvasGroup = NotPerfectUI.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        if (m_enable)
        {
            InputType input = InputType.None;
            
            // 检测WASD移动输入
            if (Input.GetKeyDown(KeyCode.W))
            {
                input = InputType.Up;
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                input = InputType.Down;
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                input = InputType.Left;
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                input = InputType.Right;
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                input = InputType.Interact;
            }

            // 处理输入
            if (input != InputType.None)
            {
                TryProcessInput(input);
            }
        }
        UIRemainingTime -= Time.deltaTime;
        
        if (UIRemainingTime > 0)
        {
            float progress = 1 - UIRemainingTime/ UIFadeDuration;
            float moveDistance = 100f; // 上移距离

            if (UIRemainingTime > UIFadeDuration)
            {
                progress = 0;
            }
            
            if (PerfectUI != null)
            {
                PerfectUI.transform.localPosition = initialUIPosition + new Vector3(0, moveDistance * progress, 0);
                perfectUICanvasGroup.alpha = 1 - progress;
            }
            if (NotPerfectUI != null)
            {
                NotPerfectUI.transform.localPosition = initialUIPosition + new Vector3(0, moveDistance * progress, 0);
                notPerfectUICanvasGroup.alpha = 1 - progress;
            }
        }
        else
        {
            UIRemainingTime = 0;
            PerfectUI.gameObject.SetActive(false);
            NotPerfectUI.gameObject.SetActive(false);
        }
    }
    
    
    /// <summary>
    /// 尝试处理输入，一拍只能input一次
    /// </summary>
    /// <param name="inputType">输入类型</param>
    /// <param name="currentBeat">当前节拍数</param>
    /// <returns>是否成功处理输入</returns>
    public bool TryProcessInput(InputType inputType)
    {
        long curTime = DateTime.UtcNow.ToUniversalTime().Ticks / 10000;

        int currentBeat = BeatMgr.Instance.GetNearestBeat(curTime);
        bool isPerfect = true;
        if (currentBeat <= lastInputBeat)
        {
            isPerfect = false;
            Debug.Log($"Unperfect Because {currentBeat} repeated move");
        }
        
        lastInputBeat = currentBeat;
        
        Debug.Log($"Input Succeed {inputType} at beat {currentBeat}");
        if (isPerfect)
        {
            isPerfect &= IsPerfectInput(curTime, currentBeat);
            if (!isPerfect)
            {
                Debug.Log($"Unperfect Because {curTime} delta {BeatMgr.Instance.GetBeatTime(currentBeat) - curTime}");
            }
        }

        if (!isPerfect)
        {
            NotPerfectUI.SetActive(true);
            PerfectUI.SetActive(false);
            StartCoroutine(DelayNonPerfectInputActions());
        }
        else
        {
            NotPerfectUI.SetActive(false);
            PerfectUI.SetActive(true);
            Debug.Log("Perfect Input");
        }

        UIRemainingTime = UIRemainDuration + UIFadeDuration;
        ProcessInput(inputType);
        return true;
    }

    private IEnumerator DelayNonPerfectInputActions()
    {
        OnNotPerfectInput?.Invoke();
        yield return new WaitForSeconds(0.05f);
        Debug.Log("Not Perfect Input");
        NoiseControlMgr.Instance.AddNoise();
    }

    public bool IsPerfectInput(long curTime, long currentBeatIndex)
    {
        long beatTime = BeatMgr.Instance.GetBeatTime(currentBeatIndex);
        return beatTime - leftEps <= curTime && curTime <= beatTime + rightEps;
    }
    
    /// <summary>
    /// 处理检测到的输入
    /// </summary>
    /// <param name="inputType">输入类型</param>
    private void ProcessInput(InputType inputType)
    {
        lastInput = inputType;
        
        // 触发通用输入事件
        OnInputDetected?.Invoke(inputType);
        
        // 触发具体的方向事件
        switch (inputType)
        {
            case InputType.Up:
                OnMoveUp?.Invoke();
                Debug.Log("InputMgr: 检测到向上移动输入 (W键)");
                AudioMgr.Instance.PlaySoundEffect(SoundEffectType.Footstep, 1.5f);
                break;
                
            case InputType.Down:
                OnMoveDown?.Invoke();
                Debug.Log("InputMgr: 检测到向下移动输入 (S键)");
                AudioMgr.Instance.PlaySoundEffect(SoundEffectType.Footstep, 1.5f);
                break;
                
            case InputType.Left:
                OnMoveLeft?.Invoke();
                Debug.Log("InputMgr: 检测到向左移动输入 (A键)");
                AudioMgr.Instance.PlaySoundEffect(SoundEffectType.Footstep, 1.5f);
                break;
                
            case InputType.Right:
                OnMoveRight?.Invoke();
                Debug.Log("InputMgr: 检测到向右移动输入 (D键)");
                AudioMgr.Instance.PlaySoundEffect(SoundEffectType.Footstep, 1.5f);
                break;
                
            case InputType.Interact:
                OnInteract?.Invoke();
                Debug.Log("InputMgr: 检测到交互输入 (空格键)");
                break;
        }
    }

    /// <summary>
    /// 启用输入检测
    /// </summary>
    public void Enable()
    {
        m_enable = true;
        Debug.Log("InputMgr: 输入检测已启用");
    }

    /// <summary>
    /// 禁用输入检测
    /// </summary>
    public void Disable()
    {
        m_enable = false;
        PerfectUI.gameObject.SetActive(false);
        NotPerfectUI.gameObject.SetActive(false);
        Debug.Log("InputMgr: 输入检测已禁用");
    }
    
    /// <summary>
    /// 检查输入是否启用
    /// </summary>
    public bool IsEnabled()
    {
        return m_enable;
    }
    
    /// <summary>
    /// 获取上次检测到的输入
    /// </summary>
    public InputType GetLastInput()
    {
        return lastInput;
    }
    
    /// <summary>
    /// 清空所有事件订阅（用于场景切换等情况）
    /// </summary>
    public static void ClearAllEvents()
    {
        OnInputDetected = null;
        OnMoveUp = null;
        OnMoveDown = null;
        OnMoveLeft = null;
        OnMoveRight = null;
        OnInteract = null;
        Debug.Log("InputMgr: 已清空所有事件订阅");
    }
    
    void OnDestroy()
    {
        // 当InputMgr被销毁时清空事件
        ClearAllEvents();
    }
}
