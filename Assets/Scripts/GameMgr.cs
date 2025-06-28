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
    private DateTime gameStartTime;
    public PlayerController mainPlayer;
    public GameObject LosePage;
    public GameObject WinPage;
    public GameObject PerfectWinPage;

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
    }

    // Update is called once per frame
    void Update()
    {
    }

    [SerializeField] private int enableDelayTime = 3000; // 启用延迟时间（毫秒）
    [SerializeField] public int BeatLimit = 200;       // 要在多少拍内逃脱

    public Action OnGameStart;
    public Action OnGameEnd;
    
    public void StartGame()
    {
        StartGamePage.SetActive(false);
        AudioMgr.Instance.PlayBackgroundMusic();
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
        OnGameEnd?.Invoke();
        AudioMgr.Instance.StopBackgroundMusic();
        InputMgr.Instance.Disable();
        BeatMgr.Instance.Disable();
    }

    private void OnDestroy()
    {
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
    }

    public void Lose()
    {
        Debug.Log("Lose");
        if (LosePage != null)
        {
            LosePage.SetActive(true);
        }
    }
}
