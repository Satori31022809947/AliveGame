using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private bool m_enable = false;
    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (m_enable)
        {
            InputType input = InputType.None;
            if (Input.GetKeyDown(KeyCode.W))
            {
                input = InputType.Up;
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                input = InputType.Down;
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                input = InputType.Left;
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                input = InputType.Right;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                input = InputType.Interact;
            }

            if (input != InputType.None)
            {
                // TODO: try do input
            }
        }
    }

    public void Enable()
    {
        m_enable = true;
    }

    public void Disable()
    {
        m_enable = false;
    }
}
