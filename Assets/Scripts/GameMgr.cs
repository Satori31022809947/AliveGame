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

    public void StartGame()
    {
        long curTime = DateTime.UtcNow.ToUniversalTime().Ticks / 10000;
        StartGamePage.SetActive(false);
        InputMgr.Instance.Enable();
        AudioMgr.Instance.PlayBackgroundMusic();
        BeatMgr.Instance.SetBeatStartTime(curTime);
        gameStartTime = DateTime.Now;
    }

    public void EndGame()
    {
    }
}
