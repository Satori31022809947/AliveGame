using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GameMgr : MonoBehaviour
{
    private static GameMgr instance;
    public static GameMgr Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameMgr>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("GameMgr");
                    instance = obj.AddComponent<GameMgr>();
                }
            }
            return instance;
        }
    }

    public Button startGameButton;
    public GameObject StartGamePage;
    public GameObject creditsPage; // 制作者名单页面引用
    private DateTime gameStartTime;
    public PlayerController mainPlayer;
    public GameObject LosePage;
    public GameObject WinPage;
    public GameObject PerfectWinPage;
    public GameObject firstInteractTipUI;
    public GameObject secondInteractTipUI;
    
    [Header("屏幕震动")]
    public CameraController cameraController; // 摄像机控制器引用

    [SerializeField] public List<Vector2Int> winPositions = new List<Vector2Int>(); // 修改为二维坐标

    public DateTime GameStartTime
    {
        get { return gameStartTime; }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }
        
        // 如果没有手动设置CameraController，尝试自动查找
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
            if (cameraController == null)
            {
                Debug.LogWarning("GameMgr: 未找到CameraController，屏幕震动功能将不可用");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    [SerializeField] private int enableDelayTime = 5; // 启用延迟时间（毫秒）
    [SerializeField] public int BeatLimit = 200;       // 要在多少拍内逃脱

    public Action OnGameStart;
    public Action OnGameEnd;
    
    public void StartGame()
    {
        StartGamePage.SetActive(false);
        AudioMgr.Instance.PlayBackgroundMusic();
        
        // 在游戏开始时订阅OnInteractFailed事件
        SubscribeToInputEvents();
        
        OnGameStart?.Invoke();
        // 延迟启用 InputMgr 和 BeatMgr
        Invoke("EnableInputAndBeat", enableDelayTime / 1000f);
    }

    private void EnableInputAndBeat()
    {
        long curTime = DateTime.UtcNow.ToUniversalTime().Ticks / 10000;
        gameStartTime = DateTime.Now;
        InputMgr.Instance.Enable();
        BeatMgr.Instance.SetBeatStartTime(curTime);
    }

    public void EndGame()
    {
        // 在游戏结束时取消订阅OnInteractFailed事件
        UnsubscribeFromInputEvents();
        
        OnGameEnd?.Invoke();
        AudioMgr.Instance.StopBackgroundMusic();
        InputMgr.Instance.Disable();
        BeatMgr.Instance.Disable();
    }
    
    /// <summary>
    /// 订阅输入事件
    /// </summary>
    private void SubscribeToInputEvents()
    {
        InputMgr.OnInteractFailed += HandleInteractFailed;
        InputMgr.OnNotPerfectInput += HandleUnperfectInput;
        Debug.Log("GameMgr: 已订阅OnInteractFailed事件");
    }
    
    /// <summary>
    /// 取消订阅输入事件
    /// </summary>
    private void UnsubscribeFromInputEvents()
    {
        InputMgr.OnInteractFailed -= HandleInteractFailed;
        InputMgr.OnNotPerfectInput -= HandleUnperfectInput;
        Debug.Log("GameMgr: 已取消订阅OnInteractFailed事件");
    }
    
    /// <summary>
    /// 处理交互失败事件，触发屏幕震动
    /// </summary>
    private void HandleInteractFailed()
    {
        if (cameraController != null)
        {
            // 触发0.2秒的屏幕震动
            cameraController.StartShake(0.2f);
            Debug.Log("GameMgr: 交互失败，触发屏幕震动0.2秒");
        }
        else
        {
            Debug.LogWarning("GameMgr: CameraController为空，无法触发屏幕震动");
        }
    }
    
    /// <summary>
    /// 处理不完美input事件，触发屏幕震动
    /// </summary>
    private void HandleUnperfectInput()
    {
        if (cameraController != null)
        {
            // 触发0.2秒的屏幕震动
            cameraController.StartShake(0.2f, 0.5f);
            Debug.Log("GameMgr: 不完美Input，触发屏幕震动0.2秒");
        }
        else
        {
            Debug.LogWarning("GameMgr: CameraController为空，无法触发屏幕震动");
        }
    }

    private void OnDestroy()
    {
        // 确保在销毁时取消订阅，避免内存泄漏
        UnsubscribeFromInputEvents();
        instance = null;
    }

    public void Win()
    {
        if (mainPlayer != null && mainPlayer.HasCollectedAllRequiredItemsForPerfectEnding())
        {   
            Debug.Log("Perfect Win");
            if (PerfectWinPage != null)
            {
                PerfectWinPage.SetActive(true);
            }
        }
        else
        {
            Debug.Log(" Win");
            if (WinPage != null)
            {
                WinPage.SetActive(true);
            }
        }

        EndGame();
    }

    public void Lose()
    {
        Debug.Log("Lose");
        if (LosePage != null)
        {
            LosePage.SetActive(true);
        }
        EndGame();
    }
    
    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    /// <summary>
    /// 打开制作者名单，禁止点击开始游戏
    /// </summary>
    public void OpenCredits()
    {
        if (startGameButton != null)
        {
            startGameButton.interactable = false;
        }
        if (creditsPage != null)
        {
            creditsPage.SetActive(true);
        }
    }

    /// <summary>
    /// 关闭制作者名单，恢复开始游戏点击功能
    /// </summary>
    public void CloseCredits()
    {
        if (startGameButton != null)
        {
            startGameButton.interactable = true;
        }
        if (creditsPage != null)
        {
            creditsPage.SetActive(false);
        }
    }
}
