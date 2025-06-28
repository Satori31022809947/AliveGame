using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

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
                Debug.Log($"BlockManager: 读取JSON内容长度: {jsonContent.Length}");
                Debug.Log($"BlockManager: JSON内容预览: {jsonContent.Substring(0, Mathf.Min(200, jsonContent.Length))}...");
                
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
        Debug.Log($"BlockManager: 开始处理道具 {config.itemName}");
        Debug.Log($"BlockManager: 道具名称: {config.itemName}");
        Debug.Log($"BlockManager: 交互类型: {config.interactionType}");
        
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
            Debug.Log($"BlockManager: 处理第 {i+1} 个坐标");
            CoordinateData coord = config.coordinates[i];
            
            // 验证坐标格式
            if (coord == null)
            {
                Debug.LogError($"BlockManager: 道具 {config.itemName} 第 {i+1} 个坐标为null");
                continue;
            }
            
            int row = coord.row;
            int col = coord.col;
            
            Debug.Log($"BlockManager: 坐标 [{i+1}]: ({row}, {col})");
            
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
                Debug.Log($"BlockManager: 坐标 ({row}, {col}) 找到有效地块，ID: {block.blockId}");
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
            
            // 只在第一个地块创建3D模型（主要显示位置）
            if (i == 0)
            {
                CreateSimpleItemInstance(block, config);
            }
            
            Debug.Log($"BlockManager: 在坐标 ({block.row}, {block.column}) 设置道具 {config.itemName} " +
                     $"(类型: {interactionType}, {i + 1}/{validBlocks.Count})");
        }
        
        Debug.Log($"BlockManager: 道具 {config.itemName} 创建完成，占用 {validBlocks.Count} 个地块");
    }
    
    /// <summary>
    /// 创建简化的道具3D模型实例
    /// </summary>
    private void CreateSimpleItemInstance(BlockData block, ItemConfig config)
    {
        // 创建基础几何体作为道具显示
        GameObject itemObject = CreateItemGeometry(block.interactionType);
        
        // 设置位置
        Vector3 itemPosition = block.position + block.itemOffset;
        itemObject.transform.position = itemPosition;
        
        // 设置名称
        itemObject.name = $"Item_{config.itemName}";
        
        // TODO:设置美术素材
        //SetItemAppearance(itemObject, block.interactionType, block.itemType);
        
        // 保存实例引用
        block.itemInstance = itemObject;
        
        // 添加动画效果
        AddItemAnimation(itemObject);
        
        Debug.Log($"BlockManager: 创建道具模型 {config.itemName} (交互类型: {block.interactionType})");
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
    private void SetItemAppearance(GameObject itemObject, ItemInteractionType interactionType)
    {
        Renderer renderer = itemObject.GetComponent<Renderer>();
        if (renderer == null) return;
        
        // 根据交互类型设置基础颜色
        Color baseColor = Color.white;
        switch (interactionType)
        {
            case ItemInteractionType.Collectible:
                baseColor = Color.yellow;  // 可拾取道具用黄色
                break;
            case ItemInteractionType.Interactable:
                baseColor = Color.blue;    // 可交互道具用蓝色
                break;
            case ItemInteractionType.Other:
                baseColor = Color.gray;    // 其他道具用灰色
                break;
        }
        
        
        renderer.material.color = baseColor;
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
        if (!allBlocks.ContainsKey(blockId))
        {
            return false;
        }
        
        BlockData block = allBlocks[blockId];
        
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
        
        foreach (var block in allBlocks.Values)
        {
            if (block.hasItem && 
                (block.itemName == itemName || block.linkedItemName == itemName))
            {
                result.Add(block);
            }
        }
        
        return result;
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
