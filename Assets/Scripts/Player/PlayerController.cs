using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // 当前所在的矩阵坐标
    private int currentRow = 0;
    private int currentColumn = 0;
    
    // 移动时的过渡时间
    [SerializeField] private float transitionTime = 0.5f;
    
    // 是否正在移动中（防止快速按键导致的问题）
    private bool isMoving = false;
    
    // 当前所在的地块数据
    private BlockData currentBlock;
    
    // 道具收集统计
    [Header("道具系统")]
    [SerializeField] private int totalCoins = 0;
    [SerializeField] private int totalGems = 0;
    [SerializeField] private int totalKeys = 0;
    [SerializeField] private int totalPotions = 0;
    [SerializeField] private List<string> collectedItemNames = new List<string>();
    
    // 道具拾取音效和特效
    [Header("拾取反馈")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private GameObject pickupEffectPrefab;
    
    void Start()
    {
        // 订阅输入事件
        SubscribeToInputEvents();
        
        // 延迟一帧确保BlockManager已经初始化
        StartCoroutine(InitializePlayerPosition());
    }

    void Update()
    {
        // 不再在Update中检测输入，改为使用事件系统
        // 输入检测现在由InputMgr处理
    }
    
    /// <summary>
    /// 订阅InputMgr的输入事件
    /// </summary>
    private void SubscribeToInputEvents()
    {
        // 订阅具体方向的移动事件
        InputMgr.OnMoveUp += () => HandleMoveInput(Direction.Up);
        InputMgr.OnMoveDown += () => HandleMoveInput(Direction.Down);
        InputMgr.OnMoveLeft += () => HandleMoveInput(Direction.Left);
        InputMgr.OnMoveRight += () => HandleMoveInput(Direction.Right);
        
        // 订阅交互事件（可选）
        InputMgr.OnInteract += HandleInteractInput;
        
        Debug.Log("PlayerController: 已订阅输入事件");
    }
    
    /// <summary>
    /// 取消订阅输入事件
    /// </summary>
    private void UnsubscribeFromInputEvents()
    {
        InputMgr.OnMoveUp -= () => HandleMoveInput(Direction.Up);
        InputMgr.OnMoveDown -= () => HandleMoveInput(Direction.Down);
        InputMgr.OnMoveLeft -= () => HandleMoveInput(Direction.Left);
        InputMgr.OnMoveRight -= () => HandleMoveInput(Direction.Right);
        InputMgr.OnInteract -= HandleInteractInput;
        
        Debug.Log("PlayerController: 已取消订阅输入事件");
    }
    
    /// <summary>
    /// 处理移动输入
    /// </summary>
    /// <param name="direction">移动方向</param>
    private void HandleMoveInput(Direction direction)
    {
        // 只有在不移动时才处理输入
        if (!isMoving)
        {
            MoveInDirection(direction);
        }
    }
    
    /// <summary>
    /// 处理交互输入
    /// </summary>
    private void HandleInteractInput()
    {
        if (!isMoving)
        {
            Debug.Log("PlayerController: 处理交互输入");
            // 在这里可以添加交互逻辑，比如：
            // - 与当前地块交互
            // - 触发特殊效果
            // - 打开菜单等
            
            if (currentBlock != null)
            {
                Debug.Log($"与地块 {currentBlock.blockName} 交互");
                
                // 根据地块类型执行不同的交互
                switch (currentBlock.blockType)
                {
                    case BlockType.Teleport:
                        Debug.Log("激活传送地块！");
                        // 可以在这里实现传送逻辑
                        break;
                        
                    default:
                        Debug.Log("这个地块没有特殊交互功能");
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// 初始化玩家位置到矩阵的(0,0)位置
    /// </summary>
    private IEnumerator InitializePlayerPosition()
    {
        // 等待一帧确保BlockManager已经发现所有地块
        yield return null;
        
        // 将玩家设置到矩阵的(0,0)位置
        SetPlayerToMatrixPosition(0, 0);
    }
    
    /// <summary>
    /// 向指定方向移动
    /// </summary>
    /// <param name="direction">移动方向</param>
    private void MoveInDirection(Direction direction)
    {
        // 获取目标地块
        BlockData targetBlock = BlockManager.Instance.GetAdjacentBlock(currentRow, currentColumn, direction);
        
        if (targetBlock != null && targetBlock.isWalkable)
        {
            // 开始移动协程
            StartCoroutine(MoveToBlock(targetBlock));
        }
        else
        {
            // 无法移动的提示
            string directionName = direction.ToString();
            if (targetBlock == null)
            {
                Debug.Log($"无法向{directionName}移动：已到达边界");
            }
            else if (!targetBlock.isWalkable)
            {
                Debug.Log($"无法向{directionName}移动：目标地块不可行走");
            }
        }
    }
    
    /// <summary>
    /// 设置玩家到指定的矩阵坐标
    /// </summary>
    /// <param name="row">目标行</param>
    /// <param name="col">目标列</param>
    private void SetPlayerToMatrixPosition(int row, int col)
    {
        BlockData targetBlock = BlockManager.Instance.GetBlockAt(row, col);
        
        if (targetBlock != null)
        {
            currentRow = row;
            currentColumn = col;
            currentBlock = targetBlock;
            
            Vector3 targetPosition = targetBlock.position;
            // 设置Y坐标稍微高一点，避免嵌入地块
            targetPosition.y += 1f;
            transform.position = targetPosition;
            
            Debug.Log($"玩家移动到矩阵位置: ({row}, {col}) - {targetBlock.blockName}");
        }
        else
        {
            Debug.LogWarning($"PlayerController: 矩阵位置 ({row}, {col}) 没有找到地块！");
        }
    }
    
    /// <summary>
    /// 平滑移动到目标地块的协程
    /// </summary>
    /// <param name="targetBlock">目标地块数据</param>
    private IEnumerator MoveToBlock(BlockData targetBlock)
    {
        isMoving = true;
        
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = targetBlock.position;
        // 设置Y坐标稍微高一点，避免嵌入地块
        targetPosition.y += 1f;
        
        float elapsedTime = 0f;
        float actualTransitionTime = transitionTime;
        
        // 根据当前地块类型调整移动速度
        if (currentBlock != null)
        {
            actualTransitionTime *= currentBlock.movementSpeed;
        }
        
        while (elapsedTime < actualTransitionTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / actualTransitionTime;
            
            // 使用平滑插值
            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            
            yield return null;
        }
        
        // 确保最终位置准确
        transform.position = targetPosition;
        
        // 更新当前坐标和地块
        currentRow = targetBlock.row;
        currentColumn = targetBlock.column;
        currentBlock = targetBlock;
        
        // 处理特殊地块效果
        HandleBlockEffect(targetBlock);
        
        // 检查并拾取道具
        CheckAndCollectItem(targetBlock);
        
        isMoving = false;
        
        Debug.Log($"玩家到达矩阵位置: ({currentRow}, {currentColumn}) - {targetBlock.blockName} (类型: {targetBlock.blockType})");
    }
    
    /// <summary>
    /// 处理地块特殊效果
    /// </summary>
    /// <param name="block">地块数据</param>
    private void HandleBlockEffect(BlockData block)
    {
        switch (block.blockType)
        {
            case BlockType.Speed:
                Debug.Log("进入加速地块！移动速度提升");
                break;
                
            case BlockType.Slow:
                Debug.Log("进入减速地块！移动速度降低");
                break;
                
            case BlockType.Teleport:
                Debug.Log("进入传送地块！按空格键激活传送");
                break;
                
            case BlockType.Obstacle:
                Debug.Log("这是障碍物地块！");
                break;
                
            default:
                break;
        }
    }
    
    /// <summary>
    /// 获取当前所在的地块
    /// </summary>
    public BlockData GetCurrentBlock()
    {
        return currentBlock;
    }
    
    /// <summary>
    /// 获取当前矩阵坐标
    /// </summary>
    public (int row, int column) GetCurrentMatrixPosition()
    {
        return (currentRow, currentColumn);
    }
    
    /// <summary>
    /// 直接传送到指定矩阵坐标（用于调试或特殊需求）
    /// </summary>
    public void TeleportToMatrixPosition(int row, int col)
    {
        if (isMoving) return;
        
        BlockData targetBlock = BlockManager.Instance.GetBlockAt(row, col);
        if (targetBlock != null && targetBlock.isWalkable)
        {
            StartCoroutine(MoveToBlock(targetBlock));
        }
    }
    
    /// <summary>
    /// 直接设置到指定矩阵坐标（瞬间移动，无动画）
    /// </summary>
    public void SetToMatrixPosition(int row, int col)
    {
        SetPlayerToMatrixPosition(row, col);
    }
    
    /// <summary>
    /// 启用/禁用玩家输入
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        if (enabled)
        {
            InputMgr.Instance.Enable();
        }
        else
        {
            InputMgr.Instance.Disable();
        }
    }
    
    /// <summary>
    /// 检查并拾取当前地块的道具
    /// </summary>
    private void CheckAndCollectItem(BlockData block)
    {
        if (block.hasItem && !block.isItemCollected)
        {
            // 通过BlockManager收集道具
            bool collected = BlockManager.Instance.CollectItemFromBlock(block.blockId);
            
            if (collected)
            {
                // 更新玩家道具统计
                UpdateItemInventory(block.itemType, block.itemValue, block.itemName);
                
                // 播放拾取反馈
                PlayPickupFeedback(block);
                
                Debug.Log($"PlayerController: 拾取了 {block.itemName} (价值: {block.itemValue})");
            }
        }
    }
    
    /// <summary>
    /// 更新玩家道具库存
    /// </summary>
    private void UpdateItemInventory(ItemType itemType, int itemValue, string itemName)
    {
        // 添加到收集清单
        collectedItemNames.Add(itemName);
        
        // 根据道具类型更新对应计数
        switch (itemType)
        {
            case ItemType.Coin:
                totalCoins += itemValue;
                Debug.Log($"金币 +{itemValue}，总数: {totalCoins}");
                break;
                
            case ItemType.Gem:
                totalGems += itemValue;
                Debug.Log($"宝石 +{itemValue}，总数: {totalGems}");
                break;
                
            case ItemType.Key:
                totalKeys += itemValue;
                Debug.Log($"钥匙 +{itemValue}，总数: {totalKeys}");
                break;
                
            case ItemType.Potion:
                totalPotions += itemValue;
                Debug.Log($"药水 +{itemValue}，总数: {totalPotions}");
                break;
        }
    }
    
    /// <summary>
    /// 播放拾取反馈效果
    /// </summary>
    private void PlayPickupFeedback(BlockData block)
    {
        // 播放音效
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // 播放特效
        if (pickupEffectPrefab != null)
        {
            Vector3 effectPosition = block.position + Vector3.up;
            GameObject effect = Instantiate(pickupEffectPrefab, effectPosition, Quaternion.identity);
            
            // 3秒后销毁特效
            Destroy(effect, 3f);
        }
        
        // 可以在这里添加更多反馈，比如：
        // - UI提示
        // - 屏幕震动
        // - 分数飘字等
    }
    
    /// <summary>
    /// 获取玩家道具统计
    /// </summary>
    public (int coins, int gems, int keys, int potions) GetItemStats()
    {
        return (totalCoins, totalGems, totalKeys, totalPotions);
    }
    
    /// <summary>
    /// 获取已收集道具列表
    /// </summary>
    public List<string> GetCollectedItems()
    {
        return new List<string>(collectedItemNames);
    }
    
    /// <summary>
    /// 重置道具统计（用于新游戏）
    /// </summary>
    public void ResetItemStats()
    {
        totalCoins = 0;
        totalGems = 0;
        totalKeys = 0;
        totalPotions = 0;
        collectedItemNames.Clear();
        
        Debug.Log("PlayerController: 道具统计已重置");
    }
    
    /// <summary>
    /// 检查是否拥有特定数量的道具
    /// </summary>
    public bool HasItem(ItemType itemType, int requiredAmount = 1)
    {
        switch (itemType)
        {
            case ItemType.Coin:
                return totalCoins >= requiredAmount;
            case ItemType.Gem:
                return totalGems >= requiredAmount;
            case ItemType.Key:
                return totalKeys >= requiredAmount;
            case ItemType.Potion:
                return totalPotions >= requiredAmount;
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 消耗道具
    /// </summary>
    public bool ConsumeItem(ItemType itemType, int amount = 1)
    {
        if (!HasItem(itemType, amount)) return false;
        
        switch (itemType)
        {
            case ItemType.Coin:
                totalCoins -= amount;
                Debug.Log($"消耗了 {amount} 个金币，剩余: {totalCoins}");
                return true;
            case ItemType.Gem:
                totalGems -= amount;
                Debug.Log($"消耗了 {amount} 个宝石，剩余: {totalGems}");
                return true;
            case ItemType.Key:
                totalKeys -= amount;
                Debug.Log($"消耗了 {amount} 个钥匙，剩余: {totalKeys}");
                return true;
            case ItemType.Potion:
                totalPotions -= amount;
                Debug.Log($"消耗了 {amount} 个药水，剩余: {totalPotions}");
                return true;
            default:
                return false;
        }
    }
    
    void OnDestroy()
    {
        // 组件销毁时取消事件订阅，防止内存泄漏
        UnsubscribeFromInputEvents();
    }
}
