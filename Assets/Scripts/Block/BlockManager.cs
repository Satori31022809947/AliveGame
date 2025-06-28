using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    //TODO: 初始化时不要覆盖dangerous设定
    public bool isDangerous = true;       //是否危险
    public float movementSpeed = 1f;       // 移动速度修正
    public string blockName;               // 地块名称
    
    // 矩阵坐标
    public int row;                        // 行坐标
    public int column;                     // 列坐标
    
    // 道具相关
    public ItemInteractionType interactionType = ItemInteractionType.Other;  // 交互类型
    public bool hasItem = false;                            // 是否有道具
    public bool isItemCollected = false;                    // 道具是否已被拾取/交互
    public string itemName = "";                            // 道具名称
    public GameObject itemInstance;                         // 道具3D模型实例
    public Vector3 itemOffset = Vector3.up;                // 道具相对地块的偏移
    public string linkedItemName = "";                     // 如果是多格道具的一部分，记录主道具名称
    
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
/// 道具交互类型枚举
/// </summary>
public enum ItemInteractionType
{
    Collectible,    // 可拾取道具
    Interactable,   // 可交互道具
    Other          // 其他（装饰品等）
}

/// <summary>
/// 单个坐标点数据结构
/// </summary>
[System.Serializable]
public class CoordinateData
{
    public int row;
    public int col;
    
    public CoordinateData() { }
    
    public CoordinateData(int r, int c)
    {
        row = r;
        col = c;
    }
}

/// <summary>
/// 传送门配置数据结构
/// </summary>
[System.Serializable]
public class PortalConfig
{
    public CoordinateData start;
    public CoordinateData end;
}

/// <summary>
/// 道具配置数据结构（用于JSON反序列化）
/// </summary>
[System.Serializable]
public class ItemConfig
{
    public string itemName;                    // 道具名称
    public CoordinateData[] coordinates;       // 占用的坐标数组
    public string interactionType;            // 交互类型：Collectible/Interactable/Other
}

/// <summary>
/// 道具配置文件结构
/// </summary>
[System.Serializable]
public class ItemConfigFile
{
    public ItemConfig[] itemConfigs;
}

/// <summary>
/// 地块配置数据结构（用于JSON反序列化）
/// </summary>
[System.Serializable]
public class BlockConfig
{
    public int row;                        // 行坐标
    public int column;                     // 列坐标
    public bool isDangerous = false;        // 是否危险
    public string blockType = "Normal";    // 地块类型（可选）
    public float movementSpeed = 1f;       // 移动速度修正（可选）
    public bool isWalkable = true;         // 是否可行走（可选）
}

/// <summary>
/// 地块配置文件结构
/// </summary>
[System.Serializable]
public class BlockConfigFile
{
    public BlockConfig[] blockConfigs;
    public PortalConfig[] portalConfigs;
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
    
    List<GameObject> blocks = new List<GameObject>();
    
    // 矩阵配置
    [Header("矩阵配置")]
    [SerializeField] private int rows = 2;           // 行数 (n)
    [SerializeField] private int columns = 2;        // 列数 (m)
    
    [Header("地块生成配置")]
    [SerializeField] private GameObject blockPrefab;      // 地块预制体
    [SerializeField] private bool autoGenerateBlocks = true;  // 是否自动生成地块
    [SerializeField] private float blockSpacing = 10f;       // 地块间距
    [SerializeField] private Vector3 startPosition = Vector3.zero;  // 起始位置
    
    [Header("道具配置")]
    [SerializeField] private bool useCustomItemPrefab = true;  // 是否使用自定义道具预制体
    [SerializeField] private string itemPrefabPath = "Prefabs/Item";  // 道具预制体在Resources中的路径
    [SerializeField] private bool allowFallbackToGeometry = true;  // 当预制体加载失败时是否允许回退到基础几何体
    
    // 矩阵地块数据 - 使用二维数组存储，便于相邻查找
    private BlockData[,] blockMatrix;
    
    // 用于玩家移动的特殊地块列表（保留兼容性，但主要使用矩阵移动）
    [SerializeField] private List<int> playerMoveTargets = new List<int>();
    
    // 调试用 - 在编辑器中显示地块信息
    [SerializeField] private bool showDebugInfo = true;
    
    // 地块配置数据
    private Dictionary<string, BlockConfig> blockConfigs = new Dictionary<string, BlockConfig>();
    
    // 传送门配置数据
    private Dictionary<int, CoordinateData> portalMap = new Dictionary<int, CoordinateData>();

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

    // 初始化传送门配置
    private void InitPortalConfigs(PortalConfig[] portalConfigs)
    {
        portalMap.Clear();
        foreach (var portal in portalConfigs)
        {
            portal.start.row = 8 - portal.start.row;
            portal.end.row = 8 - portal.end.row;
            int blockId = portal.start.row * columns + portal.start.col;
            portalMap[blockId] = portal.end;
        }
    }

    /// <summary>
    /// 查询某个block是否是传送门的起点，并返回对应的终点
    /// </summary>
    /// <param name="row">行坐标</param>
    /// <param name="col">列坐标</param>
    /// <param name="endPos">传送终点坐标</param>
    /// <returns>是否是传送门起点</returns>
    public bool TryGetPortalEnd(int blockId, out BlockData block)
    {
        CoordinateData coordinate;
        block = null;
        if (portalMap.TryGetValue(blockId, out coordinate))
        {
            block = blockMatrix[coordinate.row, coordinate.col];
            return true;
        }
        return false;
    }

    void Start()
    {
        // 初始化矩阵
        InitializeMatrix();
        
        // 加载地块配置
        LoadBlockConfigurations();
        
        // 根据配置选择地块初始化方式
        if (autoGenerateBlocks && blockPrefab != null)
        {
            // 自动生成地块
            GenerateBlocksFromPrefab();
        }
        else
        {
            // 自动发现场景中的所有地块
            DiscoverAllBlocks();
        }
        
        // 加载并应用道具配置
        LoadItemConfigurations();
    }
    
    /// <summary>
    /// 初始化矩阵数据结构
    /// </summary>
    private void InitializeMatrix()
    {
        blockMatrix = new BlockData[rows, columns];
    }

    /// <summary>
    /// 根据预制体自动生成并排列地块
    /// </summary>
    private void GenerateBlocksFromPrefab()
    {
        // 清空现有地块列表
        blocks.Clear();
        
        // 清理现有的子物体（如果有的话）
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
        
        Debug.Log($"BlockManager: 开始自动生成 {rows}×{columns} 的地块矩阵");
        
        // 按矩阵顺序生成地块
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // 计算地块位置
                Vector3 blockPosition = CalculateBlockPosition(row, col);
                
                // 实例化地块
                GameObject newBlock = Instantiate(blockPrefab, blockPosition, Quaternion.identity, transform);
                
                // 设置地块名称
                newBlock.name = $"Block_({row},{col})";
                
                // 添加到地块列表
                blocks.Add(newBlock);
                
                // 计算地块ID（线性索引）
                int blockId = row * columns + col;
                
                // 注册到矩阵系统
                RegisterBlockToMatrix(blockId, newBlock.transform, BlockType.Normal, row, col);
                
                Debug.Log($"BlockManager: 创建地块 [{blockId}] ({row},{col}) 位置: {blockPosition}");
            }
        }
        
        Debug.Log($"BlockManager: 成功自动生成了 {blocks.Count} 个地块");
    }
    
    /// <summary>
    /// 计算指定行列的地块世界坐标
    /// </summary>
    private Vector3 CalculateBlockPosition(int row, int col)
    {
        float x = startPosition.x + col * blockSpacing;      // 列向右（X正方向）
        float y = startPosition.y;                           // Y坐标保持不变
        float z = startPosition.z - row * blockSpacing;      // 行向后（Z负方向）
        
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// 自动发现场景中所有带有"Block"标签或特定命名的地块
    /// 优先查找当前GameObject的子物体
    /// </summary>
    private void DiscoverAllBlocks()
    {
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
    /// 注册地块到矩阵系统
    /// </summary>
    public void RegisterBlockToMatrix(int blockId, Transform blockTransform, BlockType blockType, int row, int col)
    {
        if (IsValidPosition(row, col))
        {
            BlockData newBlock = new BlockData(blockId, blockTransform.position, blockType, blockTransform, row, col);

            blocks[blockId].GetComponent<BlockController>().blockData = newBlock;
            blockMatrix[row, col] = newBlock;
            
            // 应用地块配置到地块数据
            ApplyBlockConfig(newBlock, row, col);
        }
    }

    /// <summary>
    /// 获取指定ID的地块数据
    /// </summary>
    public BlockData GetBlock(int blockId)
    {
        if (blocks.Count <= blockId) return null;
        return blocks[blockId].GetComponent<BlockController>().blockData;
    }

    /// <summary>
    /// 获取所有地块数据
    /// </summary>
    // public Dictionary<int, BlockData> GetAllBlocks()
    // {
    //     return allBlocks;
    // }

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
    // public void SetPlayerMoveTargetsFromChildren()
    // {
    //     playerMoveTargets.Clear();
    //     
    //     // 按子物体的顺序添加到移动目标
    //     for (int i = 0; i < transform.childCount && i < 4; i++)
    //     {
    //         if (allBlocks.ContainsKey(i))
    //         {
    //             playerMoveTargets.Add(i);
    //         }
    //     }
    //     
    //     Debug.Log($"BlockManager: 自动设置了 {playerMoveTargets.Count} 个玩家移动目标");
    // }

    /// <summary>
    /// 重新扫描子物体地块（在运行时添加/删除地块后调用）
    /// </summary>
    // public void RefreshBlocks()
    // {
    //     allBlocks.Clear();
    //     playerMoveTargets.Clear();
    //     InitializeMatrix();
    //     DiscoverAllBlocks();
    // }
    
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
    /// 手动重新生成地块（可在编辑器中调用）
    /// </summary>
    [ContextMenu("重新生成地块")]
    public void RegenerateBlocks()
    {
        if (blockPrefab == null)
        {
            Debug.LogError("BlockManager: 无法重新生成地块，blockPrefab为空！");
            return;
        }
        
        // 重新初始化矩阵
        InitializeMatrix();
        
        // 生成地块
        GenerateBlocksFromPrefab();
        
        Debug.Log("BlockManager: 手动重新生成地块完成");
    }
    
    /// <summary>
    /// 设置矩阵大小（会触发重新生成）
    /// </summary>
    public void SetMatrixSize(int newRows, int newColumns)
    {
        rows = newRows;
        columns = newColumns;
        
        if (autoGenerateBlocks && blockPrefab != null && Application.isPlaying)
        {
            RegenerateBlocks();
        }
    }
    
    /// <summary>
    /// 设置地块间距（会触发重新生成）
    /// </summary>
    public void SetBlockSpacing(float newSpacing)
    {
        blockSpacing = newSpacing;
        
        if (autoGenerateBlocks && blockPrefab != null && Application.isPlaying)
        {
            RegenerateBlocks();
        }
    }
    
    /// <summary>
    /// 设置起始位置（会触发重新生成）
    /// </summary>
    public void SetStartPosition(Vector3 newStartPosition)
    {
        startPosition = newStartPosition;
        
        if (autoGenerateBlocks && blockPrefab != null && Application.isPlaying)
        {
            RegenerateBlocks();
        }
    }
    
    /// <summary>
    /// 重新加载地块配置文件（可在运行时调用）
    /// </summary>
    [ContextMenu("重新加载地块配置")]
    public void ReloadBlockConfigurations()
    {
        // 重新加载配置
        LoadBlockConfigurations();
        
        // 重新应用到所有已存在的地块
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                BlockData block = GetBlockAt(row, col);
                if (block != null)
                {
                    ApplyBlockConfig(block, row, col);
                }
            }
        }
        
        Debug.Log("BlockManager: 重新加载地块配置完成");
    }
    
    /// <summary>
    /// 获取指定地块的配置信息（用于调试）
    /// </summary>
    public BlockConfig GetBlockConfig(int row, int col)
    {
        string key = GetBlockConfigKey(row, col);
        return blockConfigs.ContainsKey(key) ? blockConfigs[key] : null;
    }

    /// <summary>
    /// 设置道具预制体路径
    /// </summary>
    public void SetItemPrefabPath(string newPath)
    {
        itemPrefabPath = newPath;
        Debug.Log($"BlockManager: 道具预制体路径已更改为 Resources/{newPath}");
    }
    
    /// <summary>
    /// 启用或禁用自定义道具预制体
    /// </summary>
    public void SetUseCustomItemPrefab(bool useCustom)
    {
        useCustomItemPrefab = useCustom;
        Debug.Log($"BlockManager: 自定义道具预制体已{(useCustom ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 设置是否允许回退到基础几何体
    /// </summary>
    public void SetAllowFallbackToGeometry(bool allowFallback)
    {
        allowFallbackToGeometry = allowFallback;
        Debug.Log($"BlockManager: 几何体回退已{(allowFallback ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 获取当前道具配置信息
    /// </summary>
    public (bool useCustom, string prefabPath, bool allowFallback) GetItemPrefabConfig()
    {
        return (useCustomItemPrefab, itemPrefabPath, allowFallbackToGeometry);
    }

    /// <summary>
    /// 加载地块配置文件
    /// </summary>
    private void LoadBlockConfigurations()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "BlockConfig.json");
        
        Debug.Log($"BlockManager: 尝试加载地块配置文件: {configPath}");
        
        if (File.Exists(configPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(configPath);
                
                BlockConfigFile configFile = JsonUtility.FromJson<BlockConfigFile>(jsonContent);
                
                if (configFile == null)
                {
                    Debug.LogError("BlockManager: JSON反序列化失败，configFile为null");
                    return;
                }
                
                if (configFile.blockConfigs == null)
                {
                    Debug.LogError("BlockManager: blockConfigs数组为null");
                    return;
                }
                
                Debug.Log($"BlockManager: 成功加载地块配置，共 {configFile.blockConfigs.Length} 个地块");
                
                // 清空现有配置
                blockConfigs.Clear();
                
                // 将配置存储到字典中，以"row,column"为键
                for (int i = 0; i < configFile.blockConfigs.Length; i++)
                {
                    var blockConfig = configFile.blockConfigs[i];
                    string key = GetBlockConfigKey(blockConfig.row, blockConfig.column);
                    blockConfigs[key] = blockConfig;
                    
                    Debug.Log($"BlockManager: 加载地块配置 ({blockConfig.row},{blockConfig.column}) - 危险: {blockConfig.isDangerous}");
                }
                
                if (configFile.portalConfigs == null)
                {
                    Debug.LogError("BlockManager: portalConfigs数组为null");
                    return;
                }
                
                portalMap.Clear();
                InitPortalConfigs(configFile.portalConfigs);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"BlockManager: 加载地块配置失败 - {e.Message}");
                Debug.LogError($"StackTrace: {e.StackTrace}");
            }
        }
        else
        {
            Debug.LogWarning($"BlockManager: 地块配置文件不存在: {configPath}");
        }
    }
    
    /// <summary>
    /// 生成地块配置字典的键
    /// </summary>
    private string GetBlockConfigKey(int row, int column)
    {
        return $"{row},{column}";
    }
    
    /// <summary>
    /// 应用单个地块配置到指定地块
    /// </summary>
    private void ApplyBlockConfig(BlockData blockData, int row, int col)
    {
        string key = GetBlockConfigKey(row, col);
        
        if (blockConfigs.ContainsKey(key))
        {
            BlockConfig config = blockConfigs[key];
            
            // 应用配置到地块数据
            // blockData.isDangerous = config.isDangerous;
            // blockData.isWalkable = config.isWalkable;
            // blockData.movementSpeed = config.movementSpeed;
            
            // 解析并设置地块类型
            // switch (config.blockType.ToLower())
            // {
            //     case "speed":
            //         blockData.blockType = BlockType.Speed;
            //         break;
            //     case "slow":
            //         blockData.blockType = BlockType.Slow;
            //         break;
            //     case "teleport":
            //         blockData.blockType = BlockType.Teleport;
            //         break;
            //     case "obstacle":
            //         blockData.blockType = BlockType.Obstacle;
            //         break;
            //     default:
            //         blockData.blockType = BlockType.Normal;
            //         break;
            // }
            
            Debug.Log($"BlockManager: 应用地块配置到 ({row},{col}) - 危险: {config.isDangerous}, 类型: {blockData.blockType}");
        }
    }
    
    /// <summary>
    /// 加载道具配置文件
    /// </summary>
    private void LoadItemConfigurations()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "ItemConfig.json");
        
        Debug.Log($"BlockManager: 尝试加载道具配置文件: {configPath}");
        
        if (File.Exists(configPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(configPath);
                
                ItemConfigFile configFile = JsonUtility.FromJson<ItemConfigFile>(jsonContent);
                
                if (configFile == null)
                {
                    Debug.LogError("BlockManager: JSON反序列化失败，configFile为null");
                    return;
                }
                
                if (configFile.itemConfigs == null)
                {
                    Debug.LogError("BlockManager: itemConfigs数组为null");
                    return;
                }
                
                Debug.Log($"BlockManager: 成功加载道具配置，共 {configFile.itemConfigs.Length} 个道具");
                
                // 应用道具配置到对应地块
                for (int i = 0; i < configFile.itemConfigs.Length; i++)
                {
                    var itemConfig = configFile.itemConfigs[i];
                    Debug.Log($"BlockManager: 处理第 {i+1} 个道具配置: {itemConfig.itemName}");
                    ApplyItemConfig(itemConfig);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"BlockManager: 加载道具配置失败 - {e.Message}");
                Debug.LogError($"StackTrace: {e.StackTrace}");
            }
        }
        else
        {
            Debug.LogWarning($"BlockManager: 道具配置文件不存在: {configPath}");
        }
    }
    
    /// <summary>
    /// 应用单个道具配置到指定地块
    /// </summary>
    private void ApplyItemConfig(ItemConfig config)
    {
        
        // 验证coordinates数组
        if (config.coordinates == null)
        {
            Debug.LogError($"BlockManager: 道具 {config.itemName} 的coordinates为null，跳过创建");
            return;
        }
        
        if (config.coordinates.Length == 0)
        {
            Debug.LogError($"BlockManager: 道具 {config.itemName} 的coordinates数组长度为0，跳过创建");
            return;
        }
        
        Debug.Log($"BlockManager: 道具 {config.itemName} 有 {config.coordinates.Length} 个坐标");
        
        if (config.coordinates.Length > 3)
        {
            Debug.LogWarning($"BlockManager: 道具 {config.itemName} 占用超过3格地块，只使用前3个");
        }
        
        // 解析交互类型
        ItemInteractionType interactionType = ParseInteractionType(config.interactionType);
        
        // 检查所有坐标是否有效并转换为地块
        List<BlockData> validBlocks = new List<BlockData>();
        for (int i = 0; i < config.coordinates.Length && i < 3; i++)
        {
            CoordinateData coord = config.coordinates[i];
            
            // 验证坐标格式
            if (coord == null)
            {
                Debug.LogError($"BlockManager: 道具 {config.itemName} 第 {i+1} 个坐标为null");
                continue;
            }
            
            int row = 8 - coord.row; //tempfix：临时翻转
            int col = coord.col;
            
            
            // 检查坐标是否在矩阵范围内
            if (!IsValidPosition(row, col))
            {
                Debug.LogWarning($"BlockManager: 坐标 ({row}, {col}) 超出矩阵范围 ({rows}x{columns})，道具 {config.itemName} 将跳过此位置");
                continue;
            }
            
            // 获取对应的地块
            BlockData block = GetBlockAt(row, col);
            if (block != null)
            {
                validBlocks.Add(block);
            }
            else
            {
                Debug.LogWarning($"BlockManager: 坐标 ({row}, {col}) 没有对应的地块，道具 {config.itemName} 将跳过此位置");
            }
        }
        
        if (validBlocks.Count == 0)
        {
            Debug.LogWarning($"BlockManager: 道具 {config.itemName} 没有有效的地块，跳过创建");
            return;
        }
        
        // 为所有相关地块设置道具信息
        for (int i = 0; i < validBlocks.Count; i++)
        {
            BlockData block = validBlocks[i];
            
            // 设置基本道具数据
            block.hasItem = true;
            block.interactionType = interactionType;
            block.itemName = config.itemName;
            block.linkedItemName = config.itemName; // 所有格子都链接到同一个道具名称
            //如果item类型为other，标记地块不可走
            if (interactionType == ItemInteractionType.Other || interactionType == ItemInteractionType.Interactable)
            {
                block.isWalkable = false;
            }
            
            Debug.Log($"BlockManager: 在坐标 ({block.row}, {block.column}) 设置道具 {config.itemName} " +
                     $"(类型: {interactionType}, {i + 1}/{validBlocks.Count})");
        }
        
        // 创建3D模型
        if (interactionType == ItemInteractionType.Interactable)
        {
            // 对于interactable类型，计算所有格子的中心点位置
            Vector3 centerPosition = CalculateCenterPosition(validBlocks);
            CreateItemInstanceAtPosition(centerPosition, config, interactionType);
            
            // 将实例引用保存到第一个地块（用于后续销毁等操作）
            if (validBlocks.Count > 0)
            {
                // 这里需要找到刚创建的物体实例并保存引用
                GameObject lastCreatedItem = GameObject.Find($"Item_{config.itemName}");
                if (lastCreatedItem != null)
                {
                    validBlocks[0].itemInstance = lastCreatedItem;
                }
            }
            
            Debug.Log($"BlockManager: 在中心位置创建interactable道具 {config.itemName}，中心坐标: {centerPosition}");
        }
        else
        {
            // 对于其他类型，保持原有逻辑，只在第一个地块创建3D模型
            if (validBlocks.Count > 0)
            {
                CreateSimpleItemInstance(validBlocks[0], config);
            }
        }
        
        Debug.Log($"BlockManager: 道具 {config.itemName} 创建完成，占用 {validBlocks.Count} 个地块");
    }
    
    /// <summary>
    /// 计算多个地块的中心位置
    /// </summary>
    private Vector3 CalculateCenterPosition(List<BlockData> blocks)
    {
        if (blocks == null || blocks.Count == 0)
        {
            return Vector3.zero;
        }
        
        Vector3 centerPosition = Vector3.zero;
        foreach (BlockData block in blocks)
        {
            centerPosition += block.position;
        }
        centerPosition /= blocks.Count;
        
        return centerPosition;
    }
    
    /// <summary>
    /// 在指定位置创建道具实例
    /// </summary>
    private void CreateItemInstanceAtPosition(Vector3 position, ItemConfig config, ItemInteractionType interactionType)
    {
        // 检查是否启用自定义预制体
        if (!useCustomItemPrefab)
        {
            Debug.Log($"BlockManager: 自定义预制体已禁用，使用基础几何体创建道具 {config.itemName}");
            CreateFallbackItemInstanceAtPosition(position, config, interactionType);
            return;
        }
        
        // 从Resources文件夹加载道具预制体
        GameObject itemPrefab = Resources.Load<GameObject>(itemPrefabPath);
        
        if (itemPrefab == null)
        {
            string message = $"BlockManager: 无法加载道具预制体 Resources/{itemPrefabPath}";
            
            if (allowFallbackToGeometry)
            {
                Debug.LogWarning($"{message}，回退到基础几何体");
                CreateFallbackItemInstanceAtPosition(position, config, interactionType);
                return;
            }
            else
            {
                Debug.LogError($"{message}，且已禁用回退选项");
                return;
            }
        }
        
        // 实例化道具预制体
        GameObject itemObject = Instantiate(itemPrefab);
        
        // 设置位置（使用传入的位置）
        Vector3 itemPosition = position + Vector3.up; // 添加默认的向上偏移
        itemPosition.y += 2.1f;
        itemObject.transform.position = itemPosition;
        
        // 设置名称
        itemObject.name = $"Item_{config.itemName}";
        
        // 根据交互类型设置道具外观
        SetItemAppearance(itemObject, config.itemName);
        
        // 根据交互类型添加动画效果
        if (interactionType == ItemInteractionType.Collectible)
        {
            AddItemAnimation(itemObject);
        }
        
        Debug.Log($"BlockManager: 使用预制体 {itemPrefabPath} 在位置 {itemPosition} 创建道具模型 {config.itemName} (交互类型: {interactionType})");
    }
    
    /// <summary>
    /// 在指定位置创建回退道具实例
    /// </summary>
    private void CreateFallbackItemInstanceAtPosition(Vector3 position, ItemConfig config, ItemInteractionType interactionType)
    {
        // 创建基础几何体作为道具显示
        GameObject itemObject = CreateItemGeometry(interactionType);
        
        // 设置位置（使用传入的位置）
        Vector3 itemPosition = position + Vector3.up; // 添加默认的向上偏移
        itemObject.transform.position = itemPosition;
        
        // 设置名称
        itemObject.name = $"Item_{config.itemName}_Fallback";
        
        // 设置外观
        SetItemAppearance(itemObject, config.itemName);
        
        // 添加动画效果
        AddItemAnimation(itemObject);
        
        Debug.Log($"BlockManager: 使用回退方式在位置 {itemPosition} 创建道具模型 {config.itemName} (交互类型: {interactionType})");
    }
    
    /// <summary>
    /// 创建简化的道具3D模型实例（兼容性方法）
    /// </summary>
    private void CreateSimpleItemInstance(BlockData block, ItemConfig config)
    {
        // 解析交互类型
        ItemInteractionType interactionType = ParseInteractionType(config.interactionType);
        
        // 使用地块位置创建道具实例
        CreateItemInstanceAtPosition(block.position, config, interactionType);
        
        // 将实例引用保存到地块
        GameObject createdItem = GameObject.Find($"Item_{config.itemName}");
        if (createdItem != null)
        {
            block.itemInstance = createdItem;
        }
    }
    
    /// <summary>
    /// 创建回退道具实例（当预制体加载失败时）（兼容性方法）
    /// </summary>
    private void CreateFallbackItemInstance(BlockData block, ItemConfig config)
    {
        // 解析交互类型
        ItemInteractionType interactionType = ParseInteractionType(config.interactionType);
        
        // 使用地块位置创建回退道具实例
        CreateFallbackItemInstanceAtPosition(block.position, config, interactionType);
        
        // 将实例引用保存到地块
        GameObject createdItem = GameObject.Find($"Item_{config.itemName}_Fallback");
        if (createdItem != null)
        {
            block.itemInstance = createdItem;
        }
    }
    
    /// <summary>
    /// 创建道具几何体
    /// </summary>
    private GameObject CreateItemGeometry(ItemInteractionType interactionType)
    {
        GameObject itemObject;
        
        // 根据交互类型选择不同的几何体
        switch (interactionType)
        {
            case ItemInteractionType.Collectible:
                itemObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                itemObject.transform.localScale = Vector3.one * 0.3f;
                break;
                
            case ItemInteractionType.Interactable:
                itemObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                itemObject.transform.localScale = Vector3.one * 0.4f;
                break;
                
            case ItemInteractionType.Other:
            default:
                itemObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                itemObject.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);
                break;
        }
        
        return itemObject;
    }
    
    /// <summary>
    /// 设置道具外观（颜色、材质等）
    /// </summary>
    private void SetItemAppearance(GameObject itemObject, string name)
    {
        // 查找道具对象及其子物体中的所有Renderer组件
        Renderer[] renderers = itemObject.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"BlockManager: 道具 {itemObject.name} 中未找到Renderer组件，无法设置外观");
            return;
        }
        
        // 根据传入的name找到T_name.png，并传给子物体中material的maintex
        Texture2D itemTexture = LoadItemTexture(name);
        
        if (itemTexture != null)
        {
            // 应用纹理到所有Renderer的材质
            foreach (Renderer renderer in renderers)
            {
                if (renderer.material != null)
                {
                    renderer.material.mainTexture = itemTexture;
                    Debug.Log($"BlockManager: 为道具 {name} 应用纹理 T_{name}.png");
                }
            }
        }
        else
        {
            Debug.LogWarning($"BlockManager: 未找到道具 {name} 对应的纹理文件 T_{name}.png，使用默认外观");
            // 如果找不到纹理，设置默认颜色
            SetDefaultItemColor(renderers, name);
        }
    }
    
    /// <summary>
    /// 加载道具纹理文件
    /// </summary>
    private Texture2D LoadItemTexture(string itemName)
    {
        // 构造纹理文件名
        string textureName = $"T_{itemName}";
        
        // 尝试从多个可能的路径加载纹理
        Texture2D texture = null;
        
        // 方法1: 从Resources/Textures/加载
        texture = Resources.Load<Texture2D>($"Textures/{textureName}");
        
        if (texture == null)
        {
            // 方法2: 从Resources/根目录加载
            texture = Resources.Load<Texture2D>(textureName);
        }
        
        // 在编辑器模式下，可以尝试从Assets/Character2D/加载
        #if UNITY_EDITOR
        if (texture == null)
        {
            string assetPath = $"Assets/Character2D/{textureName}.png";
            texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            
            if (texture != null)
            {
                Debug.Log($"BlockManager: 在编辑器中从 {assetPath} 加载纹理成功");
            }
        }
        #endif
        
        if (texture != null)
        {
            Debug.Log($"BlockManager: 成功加载道具纹理 {textureName}");
        }
        else
        {
            Debug.LogWarning($"BlockManager: 无法加载道具纹理 {textureName}，请确保纹理文件存在于以下位置之一：" +
                           $"\n- Resources/Textures/{textureName}.png" +
                           $"\n- Resources/{textureName}.png" +
                           $"\n- Assets/Character2D/{textureName}.png");
        }
        
        return texture;
    }
    
    /// <summary>
    /// 设置默认道具颜色（当纹理加载失败时使用）
    /// </summary>
    private void SetDefaultItemColor(Renderer[] renderers, string itemName)
    {
        // 根据道具名称或交互类型设置默认颜色
        Color defaultColor = GetDefaultColorForItem(itemName);
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material != null)
            {
                renderer.material.color = defaultColor;
            }
        }
        
        Debug.Log($"BlockManager: 为道具 {itemName} 设置默认颜色 {defaultColor}");
    }
    
    /// <summary>
    /// 根据道具名称获取默认颜色
    /// </summary>
    private Color GetDefaultColorForItem(string itemName)
    {
        // 可以根据道具名称设置特定颜色
        switch (itemName.ToLower())
        {
            case "key":
            case "钥匙":
                return Color.yellow;
            case "gem":
            case "宝石":
                return Color.blue;
            case "coin":
            case "硬币":
                return Color.yellow;
            case "potion":
            case "药水":
                return Color.red;
            case "scroll":
            case "卷轴":
                return Color.white;
            default:
                // 根据首字母返回不同颜色
                char firstChar = itemName.Length > 0 ? itemName.ToUpper()[0] : 'A';
                float hue = (firstChar - 'A') / 26.0f;
                return Color.HSVToRGB(hue, 0.7f, 0.9f);
        }
    }
    
    /// <summary>
    /// 根据交互类型获取对应颜色
    /// </summary>
    private Color GetInteractionTypeColor(ItemInteractionType interactionType)
    {
        switch (interactionType)
        {
            case ItemInteractionType.Collectible:
                return Color.yellow;    // 可拾取道具用黄色
            case ItemInteractionType.Interactable:
                return Color.blue;      // 可交互道具用蓝色
            case ItemInteractionType.Other:
                return Color.gray;      // 其他道具用灰色
            default:
                return Color.white;     // 默认白色
        }
    }
    
    /// <summary>
    /// 添加道具动画效果
    /// </summary>
    private void AddItemAnimation(GameObject itemInstance)
    {
        // 添加浮动和旋转动画
        ItemAnimator animator = itemInstance.AddComponent<ItemAnimator>();
    }
    
    
    /// <summary>
    /// 解析交互类型字符串
    /// </summary>
    private ItemInteractionType ParseInteractionType(string interactionTypeString)
    {
        if (string.IsNullOrEmpty(interactionTypeString))
        {
            return ItemInteractionType.Other;
        }
        
        if (System.Enum.TryParse<ItemInteractionType>(interactionTypeString, out ItemInteractionType result))
        {
            return result;
        }
        
        Debug.LogWarning($"BlockManager: 未知的交互类型 {interactionTypeString}，默认为 Other");
        return ItemInteractionType.Other;
    }
    
    /// <summary>
    /// 收集/交互指定地块的道具
    /// </summary>
    public bool CollectItemFromBlock(int blockId)
    {
        if (blocks.Count <= blockId)
        {
            return false;
        }
        
        BlockData block = blocks[blockId].GetComponent<BlockController>().blockData;
        
        if (!block.hasItem || block.isItemCollected)
        {
            return false;
        }
        
        // 获取道具名称，用于查找所有相关地块
        string itemName = block.linkedItemName;
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = block.itemName;
        }
        
        // 查找所有属于同一道具的地块
        List<BlockData> relatedBlocks = FindBlocksByItemName(itemName);
        
        if (relatedBlocks.Count == 0)
        {
            return false;
        }
        
        // 判断交互类型
        ItemInteractionType interactionType = block.interactionType;
        bool canInteract = false;
        
        switch (interactionType)
        {
            case ItemInteractionType.Collectible:
                canInteract = true; // 可拾取道具总是可以收集
                break;
                
            case ItemInteractionType.Interactable:
                canInteract = true; // 可交互道具也可以交互
                break;
                
            case ItemInteractionType.Other:
                canInteract = false; // 装饰品等不能交互
                Debug.Log($"BlockManager: 道具 {itemName} 是装饰品，无法交互");
                break;
        }
        
        if (!canInteract)
        {
            return false;
        }
        
        // 处理所有相关地块
        bool success = false;
        foreach (BlockData relatedBlock in relatedBlocks)
        {
            if (!relatedBlock.isItemCollected)
            {
                // 标记为已收集/交互
                relatedBlock.isItemCollected = true;
                
                // 销毁3D模型（只有有模型的地块才需要销毁）
                if (relatedBlock.itemInstance != null)
                {
                    Destroy(relatedBlock.itemInstance);
                    relatedBlock.itemInstance = null;
                }
                
                success = true;
            }
        }
        
        if (success)
        {
            string actionName = interactionType == ItemInteractionType.Collectible ? "收集" : "交互";
            Debug.Log($"BlockManager: {actionName}了道具 {itemName} " +
                     $"(类型: {interactionType}, 占用 {relatedBlocks.Count} 个地块)");
        }
        
        return success;
    }
    
    /// <summary>
    /// 根据道具名称查找所有相关地块
    /// </summary>
    private List<BlockData> FindBlocksByItemName(string itemName)
    {
        List<BlockData> result = new List<BlockData>();

        
        foreach (var block in blocks)
        {
            BlockData blockData = block.GetComponent<BlockController>().blockData;
            if (blockData.hasItem && 
                (blockData.itemName == itemName || blockData.linkedItemName == itemName))
            {
                result.Add(blockData);
            }
        }
        
        return result;
    }

    /// <summary>
    /// 根据位置查找最近的地块
    /// </summary>
    // public BlockData FindNearestBlock(Vector3 position)
    // {
    //     BlockData nearest = null;
    //     float minDistance = float.MaxValue;
    //
    //     foreach (var block in allBlocks.Values)
    //     {
    //         float distance = Vector3.Distance(position, block.position);
    //         if (distance < minDistance)
    //         {
    //             minDistance = distance;
    //             nearest = block;
    //         }
    //     }
    //
    //     return nearest;
    // }

    /// <summary>
    /// 获取指定类型的所有地块
    /// </summary>
    // public List<BlockData> GetBlocksByType(BlockType blockType)
    // {
    //     List<BlockData> result = new List<BlockData>();
    //     foreach (var block in allBlocks.Values)
    //     {
    //         if (block.blockType == blockType)
    //         {
    //             result.Add(block);
    //         }
    //     }
    //     return result;
    // }

    // Unity编辑器中的调试显示
    // void OnDrawGizmos()
    // {
    //     if (!showDebugInfo || allBlocks == null) return;
    //
    //     foreach (var block in allBlocks.Values)
    //     {
    //         // 根据地块类型设置不同颜色
    //         switch (block.blockType)
    //         {
    //             case BlockType.Normal:
    //                 Gizmos.color = Color.white;
    //                 break;
    //             case BlockType.Speed:
    //                 Gizmos.color = Color.green;
    //                 break;
    //             case BlockType.Slow:
    //                 Gizmos.color = Color.yellow;
    //                 break;
    //             case BlockType.Teleport:
    //                 Gizmos.color = Color.blue;
    //                 break;
    //             case BlockType.Obstacle:
    //                 Gizmos.color = Color.red;
    //                 break;
    //         }
    //
    //         // 绘制地块位置
    //         Gizmos.DrawWireCube(block.position, Vector3.one * 0.5f);
    //     }
    //
    //     // 高亮显示玩家移动目标
    //     Gizmos.color = Color.magenta;
    //     foreach (int targetId in playerMoveTargets)
    //     {
    //         if (allBlocks.ContainsKey(targetId))
    //         {
    //             Gizmos.DrawWireSphere(allBlocks[targetId].position + Vector3.up, 0.8f);
    //         }
    //     }
    // }

    /// <summary>
    /// 获取指定位置相邻的所有interactable物体
    /// </summary>
    /// <param name="currentRow">当前行坐标</param>
    /// <param name="currentCol">当前列坐标</param>
    /// <returns>相邻的interactable物体名称列表</returns>
    public List<string> GetAdjacentInteractableItems(int currentRow, int currentCol)
    {
        List<string> interactableItems = new List<string>();
        HashSet<string> foundItems = new HashSet<string>(); // 避免重复添加同一个物体
        
        // 检查所有相邻方向
        Direction[] directions = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        
        foreach (Direction direction in directions)
        {
            BlockData adjacentBlock = GetAdjacentBlock(currentRow, currentCol, direction);
            
            if (adjacentBlock != null && adjacentBlock.hasItem && 
                adjacentBlock.interactionType == ItemInteractionType.Interactable &&
                !adjacentBlock.isItemCollected)
            {
                // 使用linkedItemName或itemName作为物体标识
                string itemIdentifier = !string.IsNullOrEmpty(adjacentBlock.linkedItemName) 
                    ? adjacentBlock.linkedItemName 
                    : adjacentBlock.itemName;
                
                if (!foundItems.Contains(itemIdentifier))
                {
                    foundItems.Add(itemIdentifier);
                    interactableItems.Add(itemIdentifier);
                }
            }
        }
        
        return interactableItems;
    }
}
