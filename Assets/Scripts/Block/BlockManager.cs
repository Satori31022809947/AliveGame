using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地块数据结构
/// </summary>
[System.Serializable]
public class BlockData
{
    public int blockId;                    // 地块唯一ID
    public Vector3 position;               // 地块位置
    public BlockType blockType;            // 地块类型
    public Transform blockTransform;       // 地块的Transform引用
    public bool isWalkable = true;         // 是否可行走
    public float movementSpeed = 1f;       // 移动速度修正
    public string blockName;               // 地块名称
    
    // 矩阵坐标
    public int row;                        // 行坐标
    public int column;                     // 列坐标
    
    public BlockData(int id, Vector3 pos, BlockType type, Transform trans, int r = -1, int c = -1)
    {
        blockId = id;
        position = pos;
        blockType = type;
        blockTransform = trans;
        row = r;
        column = c;
        blockName = $"Block_({r},{c})";
    }
}

/// <summary>
/// 地块类型枚举
/// </summary>
public enum BlockType
{
    Normal,      // 普通地块
    Speed,       // 加速地块
    Slow,        // 减速地块
    Teleport,    // 传送地块
    Obstacle     // 障碍物
}

/// <summary>
/// 移动方向枚举
/// </summary>
public enum Direction
{
    Up,          // W键 - 向上
    Down,        // S键 - 向下
    Left,        // A键 - 向左
    Right        // D键 - 向右
}

/// <summary>
/// 统一的地块管理器 - 单例模式
/// </summary>
public class BlockManager : MonoBehaviour
{
    private static BlockManager instance;
    public static BlockManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BlockManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("BlockManager");
                    instance = obj.AddComponent<BlockManager>();
                }
            }
            return instance;
        }
    }

    // 所有地块数据
    private Dictionary<int, BlockData> allBlocks = new Dictionary<int, BlockData>();
    
    // 矩阵配置
    [Header("矩阵配置")]
    [SerializeField] private int rows = 2;           // 行数 (n)
    [SerializeField] private int columns = 2;        // 列数 (m)
    
    // 矩阵地块数据 - 使用二维数组存储，便于相邻查找
    private BlockData[,] blockMatrix;
    
    // 用于玩家移动的特殊地块列表（保留兼容性，但主要使用矩阵移动）
    [SerializeField] private List<int> playerMoveTargets = new List<int>();
    
    // 调试用 - 在编辑器中显示地块信息
    [SerializeField] private bool showDebugInfo = true;

    void Awake()
    {
        // 确保只有一个实例
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 初始化矩阵
        InitializeMatrix();
        // 自动发现场景中的所有地块
        DiscoverAllBlocks();
    }
    
    /// <summary>
    /// 初始化矩阵数据结构
    /// </summary>
    private void InitializeMatrix()
    {
        blockMatrix = new BlockData[rows, columns];
    }

    /// <summary>
    /// 自动发现场景中所有带有"Block"标签或特定命名的地块
    /// 优先查找当前GameObject的子物体
    /// </summary>
    private void DiscoverAllBlocks()
    {
        List<GameObject> blocks = new List<GameObject>();
        
        // 方法1：优先查找当前GameObject的所有子物体
        if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                // 只添加直接子物体，或者名称包含Block/Plane的子物体
                if (child.name.Contains("Block") || child.name.Contains("Plane") || child.name.Contains("block") || child.name.Contains("plane"))
                {
                    blocks.Add(child.gameObject);
                }
                else
                {
                    // 如果直接子物体名称不包含Block，也将其视为地块（适用于你的使用场景）
                    blocks.Add(child.gameObject);
                }
            }
            Debug.Log($"BlockManager: 从子物体中发现了 {blocks.Count} 个地块");
        }
        
        // 方法2：如果没有子物体，通过标签查找
        if (blocks.Count == 0)
        {
            GameObject[] taggedBlocks = GameObject.FindGameObjectsWithTag("Block");
            blocks.AddRange(taggedBlocks);
            Debug.Log($"BlockManager: 通过标签发现了 {taggedBlocks.Length} 个地块");
        }
        
        // 方法3：如果前两种方法都没找到，全局名称搜索
        if (blocks.Count == 0)
        {
            Transform[] allTransforms = FindObjectsOfType<Transform>();
            
            foreach (Transform t in allTransforms)
            {
                if (t.name.Contains("Block") || t.name.Contains("Plane") || t.name.Contains("block") || t.name.Contains("plane"))
                {
                    blocks.Add(t.gameObject);
                }
            }
            Debug.Log($"BlockManager: 通过全局搜索发现了 {blocks.Count} 个地块");
        }

        // 按矩阵顺序注册地块
        int expectedBlockCount = rows * columns;
        if (blocks.Count != expectedBlockCount)
        {
            Debug.LogWarning($"BlockManager: 发现了 {blocks.Count} 个地块，但矩阵配置需要 {expectedBlockCount} 个地块！");
        }
        
        // 按矩阵坐标分配地块
        for (int i = 0; i < blocks.Count && i < expectedBlockCount; i++)
        {
            int row = i / columns;      // 计算行
            int col = i % columns;      // 计算列
            int blockId = row * columns + col;  // 矩阵ID
            
            RegisterBlockToMatrix(blockId, blocks[i].transform, BlockType.Normal, row, col);
        }

        Debug.Log($"BlockManager: 总共发现并注册了 {blocks.Count} 个地块到 {rows}×{columns} 矩阵中");
    }

    /// <summary>
    /// 注册一个地块
    /// </summary>
    public void RegisterBlock(int blockId, Transform blockTransform, BlockType blockType = BlockType.Normal)
    {
        if (!allBlocks.ContainsKey(blockId))
        {
            BlockData newBlock = new BlockData(blockId, blockTransform.position, blockType, blockTransform);
            allBlocks[blockId] = newBlock;
            
            // 为前几个地块自动添加到玩家移动目标列表
            if (playerMoveTargets.Count < 4)
            {
                playerMoveTargets.Add(blockId);
            }
        }
    }
    
    /// <summary>
    /// 注册地块到矩阵系统
    /// </summary>
    public void RegisterBlockToMatrix(int blockId, Transform blockTransform, BlockType blockType, int row, int col)
    {
        if (IsValidPosition(row, col))
        {
            BlockData newBlock = new BlockData(blockId, blockTransform.position, blockType, blockTransform, row, col);
            allBlocks[blockId] = newBlock;
            blockMatrix[row, col] = newBlock;
        }
    }

    /// <summary>
    /// 获取指定ID的地块数据
    /// </summary>
    public BlockData GetBlock(int blockId)
    {
        return allBlocks.ContainsKey(blockId) ? allBlocks[blockId] : null;
    }

    /// <summary>
    /// 获取所有地块数据
    /// </summary>
    public Dictionary<int, BlockData> GetAllBlocks()
    {
        return allBlocks;
    }

    /// <summary>
    /// 获取玩家移动目标地块列表
    /// </summary>
    public List<int> GetPlayerMoveTargets()
    {
        return playerMoveTargets;
    }

    /// <summary>
    /// 设置玩家移动目标地块
    /// </summary>
    public void SetPlayerMoveTargets(List<int> targets)
    {
        playerMoveTargets = targets;
    }

    /// <summary>
    /// 按子物体顺序自动设置玩家移动目标（便利方法）
    /// </summary>
    public void SetPlayerMoveTargetsFromChildren()
    {
        playerMoveTargets.Clear();
        
        // 按子物体的顺序添加到移动目标
        for (int i = 0; i < transform.childCount && i < 4; i++)
        {
            if (allBlocks.ContainsKey(i))
            {
                playerMoveTargets.Add(i);
            }
        }
        
        Debug.Log($"BlockManager: 自动设置了 {playerMoveTargets.Count} 个玩家移动目标");
    }

    /// <summary>
    /// 重新扫描子物体地块（在运行时添加/删除地块后调用）
    /// </summary>
    public void RefreshBlocks()
    {
        allBlocks.Clear();
        playerMoveTargets.Clear();
        InitializeMatrix();
        DiscoverAllBlocks();
    }
    
    /// <summary>
    /// 检查坐标是否在矩阵范围内
    /// </summary>
    public bool IsValidPosition(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < columns;
    }
    
    /// <summary>
    /// 根据行列坐标获取地块
    /// </summary>
    public BlockData GetBlockAt(int row, int col)
    {
        if (IsValidPosition(row, col))
        {
            return blockMatrix[row, col];
        }
        return null;
    }
    
    /// <summary>
    /// 获取相邻地块
    /// </summary>
    public BlockData GetAdjacentBlock(int currentRow, int currentCol, Direction direction)
    {
        int newRow = currentRow;
        int newCol = currentCol;
        
        switch (direction)
        {
            case Direction.Up:      // W键 - 向上（行减少）
                newRow--;
                break;
            case Direction.Down:    // S键 - 向下（行增加）
                newRow++;
                break;
            case Direction.Left:    // A键 - 向左（列减少）
                newCol--;
                break;
            case Direction.Right:   // D键 - 向右（列增加）
                newCol++;
                break;
        }
        
        return GetBlockAt(newRow, newCol);
    }
    
    /// <summary>
    /// 获取矩阵配置信息
    /// </summary>
    public (int rows, int columns) GetMatrixSize()
    {
        return (rows, columns);
    }

    /// <summary>
    /// 根据位置查找最近的地块
    /// </summary>
    public BlockData FindNearestBlock(Vector3 position)
    {
        BlockData nearest = null;
        float minDistance = float.MaxValue;

        foreach (var block in allBlocks.Values)
        {
            float distance = Vector3.Distance(position, block.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = block;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 获取指定类型的所有地块
    /// </summary>
    public List<BlockData> GetBlocksByType(BlockType blockType)
    {
        List<BlockData> result = new List<BlockData>();
        foreach (var block in allBlocks.Values)
        {
            if (block.blockType == blockType)
            {
                result.Add(block);
            }
        }
        return result;
    }

    // Unity编辑器中的调试显示
    void OnDrawGizmos()
    {
        if (!showDebugInfo || allBlocks == null) return;

        foreach (var block in allBlocks.Values)
        {
            // 根据地块类型设置不同颜色
            switch (block.blockType)
            {
                case BlockType.Normal:
                    Gizmos.color = Color.white;
                    break;
                case BlockType.Speed:
                    Gizmos.color = Color.green;
                    break;
                case BlockType.Slow:
                    Gizmos.color = Color.yellow;
                    break;
                case BlockType.Teleport:
                    Gizmos.color = Color.blue;
                    break;
                case BlockType.Obstacle:
                    Gizmos.color = Color.red;
                    break;
            }

            // 绘制地块位置
            Gizmos.DrawWireCube(block.position, Vector3.one * 0.5f);
        }

        // 高亮显示玩家移动目标
        Gizmos.color = Color.magenta;
        foreach (int targetId in playerMoveTargets)
        {
            if (allBlocks.ContainsKey(targetId))
            {
                Gizmos.DrawWireSphere(allBlocks[targetId].position + Vector3.up, 0.8f);
            }
        }
    }
}
