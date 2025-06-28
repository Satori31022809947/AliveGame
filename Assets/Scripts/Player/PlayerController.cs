using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // 初始位置设置
    [Header("初始位置")]
    [SerializeField] private int initialRow = 8;
    [SerializeField] private int initialColumn = 2;
    
    // 当前所在的矩阵坐标
    private int currentRow = 0;
    private int currentColumn = 0;
    
    // 移动时的过渡时间
    [SerializeField] private float transitionTime = 0.5f;
    
    // 跳跃效果相关参数
    [Header("跳跃效果")]
    [SerializeField] private float jumpHeight = 2f; // 跳跃高度
    [SerializeField] private AnimationCurve jumpCurve; // 跳跃曲线
    [SerializeField] private bool enableJumpEffect = true; // 是否启用跳跃效果
    
    // 挤压拉伸效果相关参数
    [Header("挤压拉伸效果")]
    [SerializeField] private bool enableSquashStretch = true; // 是否启用挤压拉伸效果
    [SerializeField] private float squashAmount = 0.3f; // 挤压程度（0-1之间）
    [SerializeField] private AnimationCurve squashCurve; // 挤压曲线
    
    // 原始缩放值（用于恢复）
    private Vector3 originalScale;
    
    // 是否正在移动中（防止快速按键导致的问题）
    private bool isMoving = false;
    
    // 当前所在的地块数据
    private BlockData currentBlock;
    
    // 道具收集统计
    [Header("道具系统")]
    [SerializeField] private List<string> collectedItemNames = new List<string>();
    
    // 道具拾取音效和特效
    [Header("拾取反馈")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private GameObject pickupEffectPrefab;
    
    //是否开始判断危险
    public bool detectDangerous = false;
    
    // Billboard效果相关
    [Header("Billboard效果")]
    [SerializeField] private Transform planeTransform; // plane子物体的Transform
    [SerializeField] private Camera targetCamera; // 目标相机，如果为null则使用Camera.main
    [SerializeField] private bool billboardEnabled = true; // 是否启用Billboard效果
    [SerializeField] private bool onlyYAxis = false; // 是否只在Y轴上旋转
    
    //player外观
    [SerializeField] private Texture2D initialImg;
    [SerializeField] private Texture2D hairNoEye;
    [SerializeField] private Texture2D hairWithEye;
    [SerializeField] private Texture2D eyeNoHair;
    
    // 传送时的缩放参数
    [Header("传送效果")]
    [SerializeField] private float teleportShrinkFactor = 0.1f; // 传送时缩小的比例
    [SerializeField] private float teleportAnimationTime = 0.2f; // 传送动画的时间
    [SerializeField] private float teleportDelay = 0.5f; // 传送延迟
    
    [Header("平滑传送动画")]
    [SerializeField] private float teleportMoveTime = 0.5f; // 平滑传送的移动时间
    [SerializeField] private float teleportArcHeight = 1.0f; // 传送弧形路径的高度
    [SerializeField] private bool enableTeleportJumpEffect = true; // 是否在传送时启用跳跃效果
    [SerializeField] private bool enableTeleportSquashEffect = true; // 是否在传送时启用缩放效果
    [SerializeField] private AnimationCurve teleportCurve; // 传送移动曲线
    [SerializeField] private bool animateReturnTeleport = true; // 是否对传送回原位置也使用动画
    
    [Header("传送缩放动画")]
    [SerializeField] private bool enableTeleportScaleAnimation = true; // 是否启用传送缩放动画
    [SerializeField] private float minTeleportScale = 0.0f; // 传送时的最小缩放值
    [SerializeField] private AnimationCurve teleportScaleCurve; // 传送缩放曲线
    
    // 位置记忆系统
    private bool hasRememberedPosition = false; // 是否有记住的位置
    private int rememberedRow = -1; // 记住的行坐标
    private int rememberedColumn = -1; // 记住的列坐标
    private Vector3 rememberedWorldPosition = Vector3.zero; // 记住的世界坐标
    
    // 移动控制系统
    private bool isMovementDisabled = false; // 是否禁用WASD移动


    void Start()
    {
        // 保存原始缩放值
        originalScale = transform.localScale;
        
        // 订阅输入事件
        SubscribeToInputEvents();
        
        // 延迟一帧确保BlockManager已经初始化
        StartCoroutine(InitializePlayerPosition());
        
        // 初始化Billboard相关组件
        InitializeBillboard();
        
        // 初始化跳跃曲线
        InitializeJumpCurve();
        
        // 初始化挤压拉伸曲线
        InitializeSquashCurve();
        
        // 初始化传送曲线
        InitializeTeleportCurve();
    }

    void Update()
    {
        //检测危险
        if (detectDangerous)
        {
            if (currentBlock.isDangerous)
            {
                GameMgr.Instance.Lose();
                Debug.Log("player in dangerous!!!");
            }
        }
        
        // 胜利判断
        if (currentBlock != null)
        {
            foreach (Vector2Int pos in GameMgr.Instance.winPositions)
            {
                BlockData targetBlock = BlockManager.Instance.GetBlockAt(pos.x, pos.y);
                if (targetBlock != null && targetBlock.blockId == currentBlock.blockId)
                {
                    GameMgr.Instance.Win();
                    break;
                }
            }
        }

        // 更新Billboard效果
        UpdateBillboard();
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
        // 检查移动是否被禁用
        if (isMovementDisabled)
        {
            Debug.Log("PlayerController: WASD移动已被禁用，请先传送回原位置");
            return;
        }
        
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
            Debug.Log($"PlayerController: 当前状态 - hasRememberedPosition: {hasRememberedPosition}, isMovementDisabled: {isMovementDisabled}");
            Debug.Log($"PlayerController: 当前位置 - ({currentRow}, {currentColumn})");
            
            // 检查相邻位置是否有interactable物体
            List<string> adjacentInteractableItems = BlockManager.Instance.GetAdjacentInteractableItems(currentRow, currentColumn);
            
            if (adjacentInteractableItems.Count > 0)
            {
                // 找到相邻的interactable物体，进行交互
                Debug.Log("成功交互！");
                
                foreach (string itemName in adjacentInteractableItems)
                {
                    Debug.Log($"PlayerController: 与相邻的interactable物体交互: {itemName}");
                }
                
                // 位置记忆和双向传送逻辑
                if (!hasRememberedPosition)
                {
                    Debug.Log("PlayerController: 执行第一次交互逻辑（记住位置并传送）");
                    
                    // 第一次交互：记住当前位置，然后传送到最近的interactable物体中心
                    RememberCurrentPosition();
                    
                    Vector3 nearestCenter = BlockManager.Instance.GetNearestInteractableCenterPosition(currentRow, currentColumn);
                    if (nearestCenter != Vector3.zero)
                    {
                        TeleportToWorldPosition(nearestCenter);
                        
                        // 禁用WASD移动
                        isMovementDisabled = true;
                        
                        Debug.Log($"PlayerController: 记住原位置({rememberedRow}, {rememberedColumn})，传送到最近的interactable物体中心位置: {nearestCenter}");
                        Debug.Log("PlayerController: WASD移动已禁用，按空格返回原位置");
                        Debug.Log($"PlayerController: 传送后状态 - hasRememberedPosition: {hasRememberedPosition}, isMovementDisabled: {isMovementDisabled}");
                    }
                    else
                    {
                        Debug.LogWarning("PlayerController: 没有找到可传送的interactable物体中心位置");
                        // 如果找不到传送目标，清除记忆的位置
                        ClearRememberedPosition();
                        // 确保移动没有被禁用
                        isMovementDisabled = false;
                    }
                }
                else
                {
                    Debug.Log("PlayerController: 执行第二次交互逻辑（传送回原位置）");
                    Debug.Log($"PlayerController: 准备传送回记忆位置({rememberedRow}, {rememberedColumn})");
                    
                    // 第二次交互：传送回记忆的位置
                    TeleportBackToRememberedPosition();
                    
                    // 重新启用WASD移动
                    isMovementDisabled = false;
                    
                    Debug.Log($"PlayerController: 传送回记忆的位置({rememberedRow}, {rememberedColumn})");
                    Debug.Log("PlayerController: WASD移动已重新启用");
                    Debug.Log($"PlayerController: 传送回后状态 - hasRememberedPosition: {hasRememberedPosition}, isMovementDisabled: {isMovementDisabled}");
                }
            }
            else
            {
                Debug.Log("PlayerController: 附近没有可交互的物体");
                
                // 如果没有相邻的interactable物体，但玩家可能在interactable物体中心想要回去
                if (hasRememberedPosition)
                {
                    Debug.Log("PlayerController: 检测到在interactable物体中心，执行返回逻辑");
                    TeleportBackToRememberedPosition();
                    isMovementDisabled = false;
                    Debug.Log("PlayerController: 已返回原位置，WASD移动已重新启用");
                }
            }
            
            // 保留原有的当前地块交互逻辑
            if (currentBlock != null)
            {
                Debug.Log($"与地块 {currentBlock.blockName} 交互");
                
                // 根据地块类型执行不同的交互
                // switch (currentBlock.blockType)
                // {
                //     case BlockType.Teleport:
                //         Debug.Log("激活传送地块！");
                //         // 可以在这里实现传送逻辑
                //         break;
                //         
                //     default:
                //         Debug.Log("这个地块没有特殊交互功能");
                //         break;
                // }
                
                //todo: 
            }
        }
    }

    public bool HasNearInteractableItem()
    {
        List<string> adjacentInteractableItems = BlockManager.Instance.GetAdjacentInteractableItems(currentRow, currentColumn);
        return adjacentInteractableItems.Count > 0;
    }
    
    /// <summary>
    /// 初始化玩家位置到指定矩阵位置
    /// </summary>
    private IEnumerator InitializePlayerPosition()
    {
        // 等待一帧确保BlockManager已经发现所有地块
        yield return null;
        
        // 将玩家设置到配置的初始位置
        SetPlayerToMatrixPosition(initialRow, initialColumn);
        
        Debug.Log($"PlayerController: 玩家初始位置设置为 ({initialRow}, {initialColumn})");
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
        // 检查BlockManager是否存在
        if (BlockManager.Instance == null)
        {
            Debug.LogError($"PlayerController: BlockManager不存在，无法设置玩家到位置 ({row}, {col})");
            return;
        }
        
        BlockData targetBlock = BlockManager.Instance.GetBlockAt(row, col);
        
        if (targetBlock != null)
        {
            // 检查地块是否可行走
            if (!targetBlock.isWalkable)
            {
                Debug.LogWarning($"PlayerController: 位置 ({row}, {col}) 的地块不可行走！尝试寻找附近可行走的地块...");
                
                // 尝试寻找附近的可行走地块
                BlockData walkableBlock = FindNearbyWalkableBlock(row, col);
                if (walkableBlock != null)
                {
                    targetBlock = walkableBlock;
                    row = walkableBlock.row;
                    col = walkableBlock.column;
                    Debug.Log($"PlayerController: 找到附近可行走地块 ({row}, {col})");
                }
                else
                {
                    Debug.LogError($"PlayerController: 位置 ({row}, {col}) 附近没有找到可行走的地块！");
                    return;
                }
            }
            
            currentRow = row;
            currentColumn = col;
            currentBlock = targetBlock;
            
            Vector3 targetPosition = targetBlock.position;
            // 设置Y坐标稍微高一点，避免嵌入地块
            targetPosition.y += 4.15f;
            targetPosition.z += 3.79f;
            transform.position = targetPosition;
            HandleTips();
            
            Debug.Log($"玩家移动到矩阵位置: ({row}, {col}) - {targetBlock.blockName}");
        }
        else
        {
            Debug.LogError($"PlayerController: 矩阵位置 ({row}, {col}) 没有找到地块！请检查坐标是否正确。");
        }
    }
    
    /// <summary>
    /// 寻找指定位置附近的可行走地块
    /// </summary>
    /// <param name="centerRow">中心行</param>
    /// <param name="centerCol">中心列</param>
    /// <returns>找到的可行走地块，如果没找到则返回null</returns>
    private BlockData FindNearbyWalkableBlock(int centerRow, int centerCol)
    {
        // 搜索半径
        int searchRadius = 3;
        
        for (int radius = 1; radius <= searchRadius; radius++)
        {
            for (int row = centerRow - radius; row <= centerRow + radius; row++)
            {
                for (int col = centerCol - radius; col <= centerCol + radius; col++)
                {
                    // 只检查当前半径边界上的点
                    if (Mathf.Abs(row - centerRow) == radius || Mathf.Abs(col - centerCol) == radius)
                    {
                        BlockData block = BlockManager.Instance.GetBlockAt(row, col);
                        if (block != null && block.isWalkable)
                        {
                            return block;
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 平滑移动到目标地块的协程
    /// </summary>
    /// <param name="targetBlock">目标地块数据</param>
    private IEnumerator MoveToBlock(BlockData targetBlock)
    {
        isMoving = true;
        
        // 先更新当前坐标和地块
        currentRow = targetBlock.row;
        currentColumn = targetBlock.column;
        currentBlock = targetBlock;
        
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = GetFinalPosition(currentBlock);
        
        // 记录基础Y坐标（用于跳跃计算）
        float startY = startPosition.y;
        float targetY = targetPosition.y;
        
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
            float progress = Mathf.Clamp01(elapsedTime / actualTransitionTime);
            
            // 计算基础位置（x, z轴的线性插值）
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            
            // 如果启用跳跃效果，计算跳跃高度
            if (enableJumpEffect)
            {
                // 使用动画曲线计算跳跃偏移
                float jumpOffset = jumpCurve.Evaluate(progress) * jumpHeight;
                
                // 应用跳跃偏移到Y坐标
                currentPosition.y = Mathf.Lerp(startY, targetY, progress) + jumpOffset;
            }
            
            // 如果启用挤压拉伸效果，计算缩放
            if (enableSquashStretch)
            {
                float squashValue = squashCurve.Evaluate(progress);
                float scaleY = 1f - (squashValue * squashAmount);
                
                // 应用缩放，保持原始的X和Z缩放
                Vector3 newScale = originalScale;
                newScale.y = originalScale.y * scaleY;
                transform.localScale = newScale;
            }
            
            transform.position = currentPosition;
            HandleTips();
            
            // 如果已经完成移动，直接跳出循环
            if (progress >= 1f)
            {
                break;
            }
            
            yield return null;
        }
        
        // 处理传送门
        BlockData portalEnd;
        if (BlockManager.Instance.TryGetPortalEnd(currentBlock.blockId, out portalEnd))
        {
            BlockData portalEndBlock = BlockManager.Instance.GetBlockAt(portalEnd.row, portalEnd.column);
            if (portalEndBlock != null)
            {
                yield return StartCoroutine(HandleTeleportation(portalEndBlock));
            }
        }
        else
        {
            // 确保最终位置准确
            transform.position = targetPosition;
            HandleTips();
        }
        
        
        // 确保缩放恢复到原始值
        if (enableSquashStretch)
        {
            transform.localScale = originalScale;
        }
        
        // 处理特殊地块效果
        HandleBlockEffect(targetBlock);
        
        // 检查并拾取道具
        CheckAndCollectItem(targetBlock);
        
        isMoving = false;
        
        Debug.Log($"玩家到达矩阵位置: ({currentRow}, {currentColumn}) - {targetBlock.blockName} (类型: {targetBlock.blockType})");
    }

    /// <summary>
    /// 处理传送逻辑，包含缩放动画
    /// </summary>
    /// <param name="targetBlock">传送目标地块</param>
    private IEnumerator HandleTeleportation(BlockData targetBlock)
    {
        // 变小动画
        float elapsedTime = 0f;
        while (elapsedTime < teleportAnimationTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / teleportAnimationTime);
            Vector3 newScale = Vector3.Lerp(originalScale, originalScale * teleportShrinkFactor, progress);
            transform.localScale = newScale;
            yield return null;
        }

        // 换位置
        currentRow = targetBlock.row;
        currentColumn = targetBlock.column;
        currentBlock = targetBlock;
        transform.position = GetFinalPosition(currentBlock);
        HandleTips();

        // 变大动画
        elapsedTime = 0f;
        while (elapsedTime < teleportAnimationTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / teleportAnimationTime);
            Vector3 newScale = Vector3.Lerp(originalScale * teleportShrinkFactor, originalScale, progress);
            transform.localScale = newScale;
            yield return null;
        }
    }

    private Vector3 GetFinalPosition(BlockData block)
    {
        Vector3 targetPosition = block.position;
        // 设置Y坐标稍微高一点，避免嵌入地块
        targetPosition.y += 4.15f;
        targetPosition.z += 3.79f;
        return targetPosition;
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
                UpdateItemInventory(block.interactionType, block.itemName);
                
                // 播放拾取反馈
                PlayPickupFeedback(block);
                
                string actionName = block.interactionType == ItemInteractionType.Collectible ? "拾取" : "交互";
                Debug.Log($"PlayerController: {actionName}了 {block.itemName} (类型: {block.interactionType})");
            }
        }
    }
    
    /// <summary>
    /// 更新玩家道具库存
    /// </summary>
    private void UpdateItemInventory(ItemInteractionType interactionType, string itemName)
    {
        // 添加到收集清单
        if (interactionType == ItemInteractionType.Collectible)
        {
            collectedItemNames.Add(itemName);
        }
        
        
        // 根据交互类型打印不同信息
        switch (interactionType)
        {
            case ItemInteractionType.Collectible:
                Debug.Log($"收集了道具: {itemName}");
                UpdateCollection(itemName);
                break;
                
            case ItemInteractionType.Interactable:
                Debug.Log($"交互了物品: {itemName}");
                break;
                
            case ItemInteractionType.Other:
                Debug.Log($"发现了装饰品: {itemName}");
                break;
        }
    }

    private void UpdateCollection(string name)
    {
        // 获取Character子物体的Renderer组件
        Transform characterTransform = transform.Find("Character");
        Renderer characterRenderer = characterTransform.GetComponent<Renderer>();
        //收集到物品后更新player外观
        switch (name)
        {
            case "Hair":
                if (characterRenderer != null && characterRenderer.material != null)
                {
                    if (collectedItemNames.Contains("Eye"))
                    {
                        // 更换材质的主纹理为hairWithEye
                        characterRenderer.material.mainTexture = hairWithEye;
                    }
                    else
                    {
                        // 更换材质的主纹理为hairNoEye
                        characterRenderer.material.mainTexture = hairNoEye;
                    }
                }
                else
                {
                    Debug.LogWarning("PlayerController: 无法更换角色外观");
                }
                break;
            case "Eye":
                if (characterRenderer != null && characterRenderer.material != null)
                {
                    if (collectedItemNames.Contains("Hair"))
                    {
                        // 更换材质的主纹理为hairWithEye
                        characterRenderer.material.mainTexture = hairWithEye;
                    }
                    else
                    {
                        // 更换材质的主纹理为eyeNoHair
                        characterRenderer.material.mainTexture = eyeNoHair;
                    }
                }
                else
                {
                    Debug.LogWarning("PlayerController: 无法更换角色外观");
                }
                break;
            case "LeftArm":
                // 找到LeftArm子物体并激活它
                Transform leftArmTransform = transform.Find("LeftArm");
                if (leftArmTransform != null)
                {
                    leftArmTransform.gameObject.SetActive(true);
                    Debug.Log("PlayerController: 已激活LeftArm子物体");
                }
                else
                {
                    Debug.LogWarning("PlayerController: 找不到LeftArm子物体");
                }
                break;
            case "RightArm":
                // 找到RightArm子物体并激活它
                Transform rightArmTransform = transform.Find("RightArm");
                if (rightArmTransform != null)
                {
                    rightArmTransform.gameObject.SetActive(true);
                    Debug.Log("PlayerController: 已激活RightArm子物体");
                }
                else
                {
                    Debug.LogWarning("PlayerController: 找不到RightArm子物体");
                }
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
        collectedItemNames.Clear();
        
        Debug.Log("PlayerController: 道具统计已重置");
    }
    
    /// <summary>
    /// 检查是否拥有特定数量的道具
    /// </summary>
    public bool HasItem()
    {
        //TODO:item 收集判断
        // switch (itemType)
        // {
        //     case ItemType.Eye:
        //         return totalCoins >= requiredAmount;
        //     case ItemType.Gem:
        //         return totalGems >= requiredAmount;
        //     case ItemType.Key:
        //         return totalKeys >= requiredAmount;
        //     case ItemType.Potion:
        //         return totalPotions >= requiredAmount;
        //     default:
        //         return false;
        // }
        return true;
    }
    
    
    
    /// <summary>
    /// 用于存储完美结局所需物品的列表，可根据需求在编辑器中配置
    /// </summary>
    [SerializeField] private List<string> requiredItemsForPerfectEnding = new List<string>();
    
    /// <summary>
    /// 判断玩家是否收集齐了完美结局必须的物品
    /// </summary>
    /// <returns>如果收集齐了返回 true，否则返回 false</returns>
    public bool HasCollectedAllRequiredItemsForPerfectEnding()
    {
        foreach (string item in requiredItemsForPerfectEnding)
        {
            if (!collectedItemNames.Contains(item))
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// 初始化跳跃曲线
    /// </summary>
    private void InitializeJumpCurve()
    {
        // 如果没有设置跳跃曲线，创建一个默认的抛物线曲线
        if (jumpCurve == null || jumpCurve.keys.Length == 0)
        {
            jumpCurve = new AnimationCurve();
            
            // 创建抛物线轨迹：开始0，中间最高，结束0
            var key1 = new Keyframe(0f, 0f);
            var key2 = new Keyframe(0.5f, 1f);
            var key3 = new Keyframe(1f, 0f);
            
            // 设置切线为0，确保起点和终点平滑
            key1.inTangent = 0f;
            key1.outTangent = 2f;
            key3.inTangent = -2f;
            key3.outTangent = 0f;
            
            jumpCurve.keys = new Keyframe[] { key1, key2, key3 };
            
            Debug.Log("PlayerController: 已创建默认跳跃曲线");
        }
        
        // 验证曲线的起始和结束点
        float startValue = jumpCurve.Evaluate(0f);
        float endValue = jumpCurve.Evaluate(1f);
        
        if (Mathf.Abs(startValue) > 0.01f || Mathf.Abs(endValue) > 0.01f)
        {
            Debug.LogWarning($"PlayerController: 跳跃曲线的起始点({startValue:F3})或结束点({endValue:F3})不为0，可能导致位置偏移");
        }
        else
        {
            Debug.Log("PlayerController: 跳跃曲线验证通过，起点和终点都为0");
        }
    }
    
    /// <summary>
    /// 初始化挤压拉伸曲线
    /// </summary>
    private void InitializeSquashCurve()
    {
        // 如果没有设置挤压曲线，创建一个默认的挤压效果曲线
        if (squashCurve == null || squashCurve.keys.Length == 0)
        {
            squashCurve = new AnimationCurve();
            
            // 创建挤压效果轨迹：
            // 起跳时压缩 -> 跳跃中拉伸回原状 -> 落地时压缩 -> 最后回原状
            var key1 = new Keyframe(0f, 1f);      // 起跳瞬间压缩
            var key2 = new Keyframe(0.15f, 0f);   // 起跳后快速回到正常
            var key3 = new Keyframe(0.5f, 0f);    // 跳跃中保持正常
            var key4 = new Keyframe(0.85f, 0f);   // 落地前保持正常
            var key5 = new Keyframe(0.95f, 1f);   // 落地瞬间压缩
            var key6 = new Keyframe(1f, 0f);      // 最终回到正常
            
            // 设置平滑的切线
            key1.inTangent = 0f;
            key1.outTangent = -10f;
            key2.inTangent = -5f;
            key2.outTangent = 0f;
            key3.inTangent = 0f;
            key3.outTangent = 0f;
            key4.inTangent = 0f;
            key4.outTangent = 0f;
            key5.inTangent = 0f;
            key5.outTangent = -10f;
            key6.inTangent = -5f;
            key6.outTangent = 0f;
            
            squashCurve.keys = new Keyframe[] { key1, key2, key3, key4, key5, key6 };
            
            Debug.Log("PlayerController: 已创建默认挤压拉伸曲线");
        }
        
        Debug.Log("PlayerController: 挤压拉伸效果初始化完成");
    }
    
    /// <summary>
    /// 初始化传送曲线
    /// </summary>
    private void InitializeTeleportCurve()
    {
        // 如果没有设置传送曲线，创建一个默认的传送曲线
        if (teleportCurve == null || teleportCurve.keys.Length == 0)
        {
            teleportCurve = new AnimationCurve();
            
            // 创建传送曲线轨迹：
            // 起跳时压缩 -> 跳跃中拉伸回原状 -> 落地时压缩 -> 最后回原状
            var key1 = new Keyframe(0f, 1f);      // 起跳瞬间压缩
            var key2 = new Keyframe(0.15f, 0f);   // 起跳后快速回到正常
            var key3 = new Keyframe(0.5f, 0f);    // 跳跃中保持正常
            var key4 = new Keyframe(0.85f, 0f);   // 落地前保持正常
            var key5 = new Keyframe(0.95f, 1f);   // 落地瞬间压缩
            var key6 = new Keyframe(1f, 0f);      // 最终回到正常
            
            // 设置平滑的切线
            key1.inTangent = 0f;
            key1.outTangent = -10f;
            key2.inTangent = -5f;
            key2.outTangent = 0f;
            key3.inTangent = 0f;
            key3.outTangent = 0f;
            key4.inTangent = 0f;
            key4.outTangent = 0f;
            key5.inTangent = 0f;
            key5.outTangent = -10f;
            key6.inTangent = -5f;
            key6.outTangent = 0f;
            
            teleportCurve.keys = new Keyframe[] { key1, key2, key3, key4, key5, key6 };
            
            Debug.Log("PlayerController: 已创建默认传送曲线");
        }
        
        // 如果没有设置传送缩放曲线，创建一个默认的缩放曲线
        if (teleportScaleCurve == null || teleportScaleCurve.keys.Length == 0)
        {
            teleportScaleCurve = new AnimationCurve();
            
            // 创建传送缩放曲线：
            // 传送到中心：1.0 -> 0.0 (缩小到消失)
            // 传送回原位置：0.0 -> 1.0 (从消失放大到正常)
            var scaleKey1 = new Keyframe(0f, 1f);    // 起始正常大小
            var scaleKey2 = new Keyframe(0.5f, 0f);   // 中点完全缩小
            var scaleKey3 = new Keyframe(1f, 1f);    // 结束正常大小
            
            // 设置平滑的切线
            scaleKey1.inTangent = 0f;
            scaleKey1.outTangent = -2f;
            scaleKey2.inTangent = -2f;
            scaleKey2.outTangent = 2f;
            scaleKey3.inTangent = 2f;
            scaleKey3.outTangent = 0f;
            
            teleportScaleCurve.keys = new Keyframe[] { scaleKey1, scaleKey2, scaleKey3 };
            
            Debug.Log("PlayerController: 已创建默认传送缩放曲线");
        }
        
        Debug.Log("PlayerController: 传送曲线初始化完成");
    }
    
    /// <summary>
    /// 初始化Billboard相关组件
    /// </summary>
    private void InitializeBillboard()
    {
        // 如果没有手动指定plane，直接获取第一个子物体
        if (planeTransform == null)
        {
            if (transform.childCount > 0)
            {
                planeTransform = transform.GetChild(0);
                Debug.Log("PlayerController: 自动获取第一个子物体作为plane");
            }
            else
            {
                Debug.LogWarning("PlayerController: 没有找到子物体");
            }
        }
        
        // 如果没有指定相机，使用主相机
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogWarning("PlayerController: 未找到主相机，Billboard效果可能无法正常工作");
            }
        }
    }
    
    /// <summary>
    /// 更新Billboard效果，让plane垂直于相机
    /// </summary>
    private void UpdateBillboard()
    {
        if (!billboardEnabled || planeTransform == null || targetCamera == null)
            return;
            
        Vector3 cameraForward = targetCamera.transform.forward;
        
        if (onlyYAxis)
        {
            // 只在Y轴上旋转（常用于UI元素或地面物体）
            cameraForward.y = 0;
            cameraForward.Normalize();
        }
        
        if (cameraForward != Vector3.zero)
        {
            // 让plane的up向量与相机的forward向量反向对齐，使plane正面垂直于相机视线
            planeTransform.up = -cameraForward;
        }
    }
    
    /// <summary>
    /// 设置Billboard效果的启用状态
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetBillboardEnabled(bool enabled)
    {
        billboardEnabled = enabled;
    }
    
    /// <summary>
    /// 设置目标相机
    /// </summary>
    /// <param name="camera">目标相机</param>
    public void SetTargetCamera(Camera camera)
    {
        targetCamera = camera;
    }
    
    /// <summary>
    /// 设置跳跃效果的启用状态
    /// </summary>
    /// <param name="enabled">是否启用跳跃效果</param>
    public void SetJumpEffectEnabled(bool enabled)
    {
        enableJumpEffect = enabled;
        Debug.Log($"PlayerController: 跳跃效果已{(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 设置跳跃高度
    /// </summary>
    /// <param name="height">跳跃高度</param>
    public void SetJumpHeight(float height)
    {
        jumpHeight = Mathf.Max(0f, height); // 确保高度不为负数
        Debug.Log($"PlayerController: 跳跃高度设置为 {jumpHeight}");
    }
    
    /// <summary>
    /// 设置移动过渡时间
    /// </summary>
    /// <param name="time">过渡时间</param>
    public void SetTransitionTime(float time)
    {
        transitionTime = Mathf.Max(0.1f, time); // 确保时间不会太小
        Debug.Log($"PlayerController: 移动过渡时间设置为 {transitionTime}");
    }
    
    /// <summary>
    /// 获取当前跳跃效果设置
    /// </summary>
    public (bool enabled, float height, float transitionTime) GetJumpSettings()
    {
        return (enableJumpEffect, jumpHeight, transitionTime);
    }
    
    /// <summary>
    /// 重置跳跃曲线为默认抛物线
    /// </summary>
    public void ResetJumpCurveToDefault()
    {
        jumpCurve = new AnimationCurve();
        
        var key1 = new Keyframe(0f, 0f);
        var key2 = new Keyframe(0.5f, 1f);
        var key3 = new Keyframe(1f, 0f);
        
        // 设置切线为0，确保起点和终点平滑
        key1.inTangent = 0f;
        key1.outTangent = 2f;
        key3.inTangent = -2f;
        key3.outTangent = 0f;
        
        jumpCurve.keys = new Keyframe[] { key1, key2, key3 };
        
        Debug.Log("PlayerController: 跳跃曲线已重置为默认抛物线");
    }
    
    /// <summary>
    /// 设置挤压拉伸效果的启用状态
    /// </summary>
    /// <param name="enabled">是否启用挤压拉伸效果</param>
    public void SetSquashStretchEnabled(bool enabled)
    {
        enableSquashStretch = enabled;
        
        // 如果禁用效果，立即恢复原始缩放
        if (!enabled)
        {
            transform.localScale = originalScale;
        }
        
        Debug.Log($"PlayerController: 挤压拉伸效果已{(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 设置挤压程度
    /// </summary>
    /// <param name="amount">挤压程度（0-1之间）</param>
    public void SetSquashAmount(float amount)
    {
        squashAmount = Mathf.Clamp01(amount);
        Debug.Log($"PlayerController: 挤压程度设置为 {squashAmount:F2}");
    }
    
    /// <summary>
    /// 重置挤压拉伸曲线为默认效果
    /// </summary>
    public void ResetSquashCurveToDefault()
    {
        squashCurve = null; // 清空现有曲线
        InitializeSquashCurve(); // 重新创建默认曲线
        Debug.Log("PlayerController: 挤压拉伸曲线已重置为默认效果");
    }
    
    /// <summary>
    /// 获取当前挤压拉伸效果设置
    /// </summary>
    public (bool enabled, float amount) GetSquashStretchSettings()
    {
        return (enableSquashStretch, squashAmount);
    }
    
    /// <summary>
    /// 设置初始位置（下次游戏开始时生效）
    /// </summary>
    /// <param name="row">初始行位置</param>
    /// <param name="column">初始列位置</param>
    public void SetInitialPosition(int row, int column)
    {
        initialRow = row;
        initialColumn = column;
        Debug.Log($"PlayerController: 初始位置已设置为 ({row}, {column})，下次游戏开始时生效");
    }
    
    /// <summary>
    /// 获取当前设置的初始位置
    /// </summary>
    /// <returns>初始位置的行和列</returns>
    public (int row, int column) GetInitialPosition()
    {
        return (initialRow, initialColumn);
    }
    
    /// <summary>
    /// 重置玩家到初始位置
    /// </summary>
    public void ResetToInitialPosition()
    {
        SetPlayerToMatrixPosition(initialRow, initialColumn);
        
        // 重置时清除位置记忆
        ClearRememberedPosition();
        
        Debug.Log($"PlayerController: 玩家已重置到初始位置 ({initialRow}, {initialColumn})，并清除位置记忆");
    }
    
    /// <summary>
    /// 传送到指定的世界坐标位置
    /// </summary>
    /// <param name="worldPosition">目标世界坐标</param>
    public void TeleportToWorldPosition(Vector3 worldPosition)
    {
        if (isMoving) return;
        
        Debug.Log($"PlayerController: 开始传送到世界坐标: {worldPosition}");
        Debug.Log($"PlayerController: 传送前位置 - 矩阵坐标: ({currentRow}, {currentColumn}), 世界坐标: {transform.position}");
        
        // 开始平滑移动到目标位置（传送到可交互中心）
        StartCoroutine(SmoothMoveToWorldPosition(worldPosition, true));
    }
    
    /// <summary>
    /// 平滑移动到世界坐标位置
    /// </summary>
    /// <param name="targetWorldPosition">目标世界位置</param>
    /// <param name="toInteractableCenter">是否是传送到可交互物体中心</param>
    private IEnumerator SmoothMoveToWorldPosition(Vector3 targetWorldPosition, bool toInteractableCenter = true)
    {
        isMoving = true;
        
        Vector3 startWorldPosition = transform.position;
        float elapsedTime = 0f;
        
        // 记录初始缩放 - 使用启动时的原始缩放，避免在传送过程中缩放值异常
        Vector3 originalScale;
        if (toInteractableCenter)
        {
            // 传送到中心时，使用当前缩放作为基准（应该是正常的1.0）
            originalScale = transform.localScale;
        }
        else
        {
            // 传送回原位置时，使用启动时的原始缩放作为基准
            originalScale = this.originalScale; // 使用类成员变量中存储的原始缩放
        }
        
        // 计算目标位置（需要调整Y和Z坐标以匹配玩家的正确位置）
        Vector3 targetPosition = targetWorldPosition;
        targetPosition.y += 4.15f;  // 玩家相对于地块的Y偏移
        targetPosition.z += 3.79f;  // 玩家相对于地块的Z偏移
        
        Debug.Log($"PlayerController: 开始平滑传送 - 从 {startWorldPosition} 到 {targetPosition}，方向：{(toInteractableCenter ? "到可交互中心" : "回到原位置")}");
        Debug.Log($"PlayerController: 传送缩放设置 - 启用: {enableTeleportScaleAnimation}, 最小缩放: {minTeleportScale}, 基准缩放: {originalScale}");
        
        while (elapsedTime < teleportMoveTime)
        {
            float normalizedTime = elapsedTime / teleportMoveTime;
            
            // 计算贝塞尔曲线路径
            Vector3 currentPosition = CalculateBezierPoint(normalizedTime, startWorldPosition, targetPosition);
            
            // 应用位置
            transform.position = currentPosition;
            
            // 先计算基础缩放值
            Vector3 baseScale = originalScale;
            
            // 处理缩放动画
            if (enableTeleportScaleAnimation)
            {
                float scaleValue;
                
                if (toInteractableCenter)
                {
                    // 传送到可交互中心：从1.0缩放到minTeleportScale（通常是0）
                    scaleValue = Mathf.Lerp(1.0f, minTeleportScale, teleportScaleCurve.Evaluate(normalizedTime));
                }
                else
                {
                    // 传送回原位置：从minTeleportScale（通常是0）缩放到1.0
                    scaleValue = Mathf.Lerp(minTeleportScale, 1.0f, teleportScaleCurve.Evaluate(normalizedTime));
                }
                
                baseScale = originalScale * scaleValue;
            }
            
            // 应用跳跃效果（如果启用）
            if (enableTeleportJumpEffect)
            {
                // 使用y位置的偏移来创建跳跃效果，注意这个偏移会叠加在贝塞尔曲线上
                // 这里我们不额外添加偏移，因为贝塞尔曲线已经包含了弧形路径
            }
            
            // 应用压缩效果（如果启用）- 在基础缩放之上应用压缩
            if (enableTeleportSquashEffect)
            {
                float squashValue = teleportCurve.Evaluate(normalizedTime);
                Vector3 squashScale = new Vector3(baseScale.x, baseScale.y + squashValue * 0.2f, baseScale.z);
                transform.localScale = squashScale;
            }
            else
            {
                // 如果没有压缩效果，直接应用基础缩放
                transform.localScale = baseScale;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 确保最终位置和缩放正确
        transform.position = targetPosition;
        
        if (enableTeleportScaleAnimation)
        {
            if (toInteractableCenter)
            {
                // 传送到中心完成，缩放为minTeleportScale
                transform.localScale = originalScale * minTeleportScale;
                Debug.Log($"PlayerController: 传送到中心完成，最终缩放设置为: {transform.localScale} (原始: {originalScale} x {minTeleportScale})");
            }
            else
            {
                // 传送回原位置完成，恢复正常缩放
                transform.localScale = originalScale;
                Debug.Log($"PlayerController: 传送回原位置完成，最终缩放恢复为: {transform.localScale}");
            }
        }
        else
        {
            // 如果没有启用缩放动画，确保缩放正确
            transform.localScale = originalScale;
            Debug.Log($"PlayerController: 缩放动画未启用，最终缩放设置为: {transform.localScale}");
        }
        
        // 更新玩家的矩阵坐标
        UpdatePlayerMatrixPositionFromWorldPosition(targetWorldPosition);
        
        isMoving = false;
        
        Debug.Log($"PlayerController: 平滑传送完成 - 最终位置: {transform.position}，最终缩放: {transform.localScale}");
        Debug.Log($"PlayerController: 传送后矩阵坐标: ({currentRow}, {currentColumn})");
    }
    
    /// <summary>
    /// 计算贝塞尔曲线上的点
    /// </summary>
    /// <param name="t">时间参数 (0-1)</param>
    /// <param name="startPos">起始位置</param>
    /// <param name="endPos">结束位置</param>
    /// <returns>贝塞尔曲线上的点</returns>
    private Vector3 CalculateBezierPoint(float t, Vector3 startPos, Vector3 endPos)
    {
        // 计算中点并添加弧形高度
        Vector3 midPoint = (startPos + endPos) / 2f;
        midPoint.y += teleportArcHeight;
        
        // 使用二次贝塞尔曲线公式: B(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
        float oneMinusT = 1f - t;
        Vector3 point = (oneMinusT * oneMinusT * startPos) + 
                       (2f * oneMinusT * t * midPoint) + 
                       (t * t * endPos);
        
        return point;
    }
    
    /// <summary>
    /// 记住当前玩家位置
    /// </summary>
    private void RememberCurrentPosition()
    {
        Debug.Log($"PlayerController: 开始记住当前位置");
        Debug.Log($"PlayerController: 当前矩阵坐标: ({currentRow}, {currentColumn})");
        Debug.Log($"PlayerController: 当前地块: {(currentBlock != null ? currentBlock.blockName : "null")}");
        
        rememberedRow = currentRow;
        rememberedColumn = currentColumn;
        if (currentBlock != null)
        {
            rememberedWorldPosition = currentBlock.position;
        }
        hasRememberedPosition = true;
        HandleTips();
        
        Debug.Log($"PlayerController: 记住当前位置完成 - 矩阵坐标: ({rememberedRow}, {rememberedColumn}), 世界坐标: {rememberedWorldPosition}");
        Debug.Log($"PlayerController: hasRememberedPosition设置为: {hasRememberedPosition}");
    }
    
    /// <summary>
    /// 清除记忆的位置
    /// </summary>
    private void ClearRememberedPosition()
    {
        Debug.Log($"PlayerController: 开始清除记忆位置");
        Debug.Log($"PlayerController: 清除前状态 - hasRememberedPosition: {hasRememberedPosition}, rememberedPos: ({rememberedRow}, {rememberedColumn})");
        
        hasRememberedPosition = false;
        rememberedRow = -1;
        rememberedColumn = -1;
        rememberedWorldPosition = Vector3.zero;
        HandleTips();
        
        // 清除记忆时重新启用移动
        isMovementDisabled = false;
        
        Debug.Log("PlayerController: 清除记忆的位置完成，WASD移动已重新启用");
        Debug.Log($"PlayerController: 清除后状态 - hasRememberedPosition: {hasRememberedPosition}, isMovementDisabled: {isMovementDisabled}");
    }
    
    /// <summary>
    /// 传送回记忆的位置
    /// </summary>
    private void TeleportBackToRememberedPosition()
    {
        Debug.Log($"PlayerController: 开始传送回记忆位置");
        Debug.Log($"PlayerController: 记忆状态 - hasRememberedPosition: {hasRememberedPosition}");
        Debug.Log($"PlayerController: 记忆位置 - ({rememberedRow}, {rememberedColumn}), 世界坐标: {rememberedWorldPosition}");
        
        if (!hasRememberedPosition)
        {
            Debug.LogWarning("PlayerController: 没有记忆的位置可以传送回去");
            return;
        }
        
        // 检查记忆的位置是否仍然有效
        BlockData rememberedBlock = BlockManager.Instance.GetBlockAt(rememberedRow, rememberedColumn);
        if (rememberedBlock == null)
        {
            Debug.LogWarning($"PlayerController: 记忆的位置({rememberedRow}, {rememberedColumn})不再有效");
            ClearRememberedPosition();
            return;
        }
        
        Debug.Log($"PlayerController: 记忆地块有效 - {rememberedBlock.blockName}, 可行走: {rememberedBlock.isWalkable}");
        
        // 检查记忆的地块是否可行走
        if (!rememberedBlock.isWalkable)
        {
            Debug.LogWarning($"PlayerController: 记忆的位置({rememberedRow}, {rememberedColumn})不可行走，寻找附近的可行走地块");
            
            // 尝试找到附近的可行走地块
            BlockData walkableBlock = FindNearbyWalkableBlock(rememberedRow, rememberedColumn);
            if (walkableBlock != null)
            {
                // 更新记忆位置为可行走的地块
                Debug.Log($"PlayerController: 原记忆位置: ({rememberedRow}, {rememberedColumn})");
                rememberedRow = walkableBlock.row;
                rememberedColumn = walkableBlock.column;
                rememberedWorldPosition = walkableBlock.position;
                rememberedBlock = walkableBlock;
                Debug.Log($"PlayerController: 找到附近可行走地块，更新记忆位置为({rememberedRow}, {rememberedColumn})");
            }
            else
            {
                Debug.LogError("PlayerController: 记忆位置附近没有找到可行走的地块");
                ClearRememberedPosition();
                return;
            }
        }
        
        Debug.Log($"PlayerController: 准备传送到记忆位置({rememberedRow}, {rememberedColumn})");
        
        // 传送到记忆的位置
        if (animateReturnTeleport)
        {
            // 使用动画传送回原位置
            Debug.Log("PlayerController: 使用动画传送回原位置");
            StartCoroutine(SmoothMoveToWorldPosition(rememberedWorldPosition, false)); // 传送回原位置
        }
        else
        {
            // 瞬间传送回原位置
            Debug.Log("PlayerController: 瞬间传送回原位置");
            SetPlayerToMatrixPosition(rememberedRow, rememberedColumn);
        }
        
        Debug.Log($"PlayerController: 传送到记忆位置完成，当前位置: ({currentRow}, {currentColumn})");
        
        // 清除记忆的位置，下次交互将重新开始记忆过程
        ClearRememberedPosition();
        
        Debug.Log($"PlayerController: 已传送回记忆的位置({currentRow}, {currentColumn})");
    }
    
    /// <summary>
    /// 获取当前是否有记忆的位置（用于调试或UI显示）
    /// </summary>
    public bool HasRememberedPosition()
    {
        return hasRememberedPosition;
    }
    
    /// <summary>
    /// 获取记忆的位置信息（用于调试或UI显示）
    /// </summary>
    public (int row, int column, Vector3 worldPos) GetRememberedPosition()
    {
        return (rememberedRow, rememberedColumn, rememberedWorldPosition);
    }
    
    /// <summary>
    /// 手动重置位置记忆系统（用于调试或特殊情况）
    /// </summary>
    public void ResetPositionMemory()
    {
        ClearRememberedPosition();
        Debug.Log("PlayerController: 手动重置位置记忆系统");
    }
    
    /// <summary>
    /// 在玩家死亡或重置时清除位置记忆
    /// </summary>
    public void OnPlayerReset()
    {
        ClearRememberedPosition();
        Debug.Log("PlayerController: 玩家重置，清除位置记忆");
    }
    
    /// <summary>
    /// 获取当前移动是否被禁用（用于调试或UI显示）
    /// </summary>
    public bool IsMovementDisabled()
    {
        return isMovementDisabled;
    }
    
    /// <summary>
    /// 手动启用或禁用移动（用于调试或特殊情况）
    /// </summary>
    /// <param name="disabled">是否禁用移动</param>
    public void SetMovementDisabled(bool disabled)
    {
        isMovementDisabled = disabled;
        string status = disabled ? "禁用" : "启用";
        Debug.Log($"PlayerController: 手动{status}WASD移动");
    }
    
    /// <summary>
    /// 强制启用移动（紧急情况使用）
    /// </summary>
    public void ForceEnableMovement()
    {
        isMovementDisabled = false;
        Debug.Log("PlayerController: 强制启用WASD移动");
    }
    
    /// <summary>
    /// 设置传送移动时间
    /// </summary>
    /// <param name="time">移动时间（秒）</param>
    public void SetTeleportMoveTime(float time)
    {
        teleportMoveTime = Mathf.Max(0.1f, time);
        Debug.Log($"PlayerController: 传送移动时间设置为 {teleportMoveTime}秒");
    }
    
    /// <summary>
    /// 设置传送弧形高度
    /// </summary>
    /// <param name="height">弧形高度</param>
    public void SetTeleportArcHeight(float height)
    {
        teleportArcHeight = Mathf.Max(0f, height);
        Debug.Log($"PlayerController: 传送弧形高度设置为 {teleportArcHeight}");
    }
    
    /// <summary>
    /// 启用或禁用传送跳跃效果
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetTeleportJumpEffectEnabled(bool enabled)
    {
        enableTeleportJumpEffect = enabled;
        Debug.Log($"PlayerController: 传送跳跃效果已{(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 启用或禁用传送缩放效果
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetTeleportSquashEffectEnabled(bool enabled)
    {
        enableTeleportSquashEffect = enabled;
        Debug.Log($"PlayerController: 传送缩放效果已{(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 启用或禁用传送回原位置的动画
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetReturnTeleportAnimationEnabled(bool enabled)
    {
        animateReturnTeleport = enabled;
        Debug.Log($"PlayerController: 传送回原位置动画已{(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 获取传送动画设置
    /// </summary>
    /// <returns>传送动画设置的元组</returns>
    public (float moveTime, float arcHeight, bool jumpEffect, bool squashEffect) GetTeleportSettings()
    {
        return (teleportMoveTime, teleportArcHeight, enableTeleportJumpEffect, enableTeleportSquashEffect);
    }
    
    /// <summary>
    /// 获取完整的传送设置
    /// </summary>
    /// <returns>完整的传送设置元组</returns>
    public (float moveTime, float arcHeight, bool jumpEffect, bool squashEffect, bool animateReturn) GetFullTeleportSettings()
    {
        return (teleportMoveTime, teleportArcHeight, enableTeleportJumpEffect, enableTeleportSquashEffect, animateReturnTeleport);
    }
    
    /// <summary>
    /// 重置传送曲线为默认设置
    /// </summary>
    public void ResetTeleportCurveToDefault()
    {
        teleportCurve = null;
        InitializeTeleportCurve();
        Debug.Log("PlayerController: 传送曲线已重置为默认设置");
    }
    
    /// <summary>
    /// 根据世界坐标更新玩家的矩阵坐标
    /// </summary>
    /// <param name="worldPosition">世界坐标</param>
    private void UpdatePlayerMatrixPositionFromWorldPosition(Vector3 worldPosition)
    {
        Debug.Log($"PlayerController: 开始更新矩阵坐标，基于世界坐标: {worldPosition}");
        
        float minDistance = float.MaxValue;
        BlockData nearestBlock = null;
        
        // 遍历所有地块找到最近的
        var (rows, columns) = BlockManager.Instance.GetMatrixSize();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                BlockData block = BlockManager.Instance.GetBlockAt(row, col);
                if (block != null)
                {
                    // 计算平面距离（忽略Y轴）
                    Vector3 blockPos2D = new Vector3(block.position.x, 0, block.position.z);
                    Vector3 worldPos2D = new Vector3(worldPosition.x, 0, worldPosition.z);
                    float distance = Vector3.Distance(blockPos2D, worldPos2D);
                    
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestBlock = block;
                    }
                }
            }
        }
        
        // 更新玩家的矩阵坐标
        if (nearestBlock != null)
        {
            Debug.Log($"PlayerController: 找到最近地块 - 距离: {minDistance:F2}");
            Debug.Log($"PlayerController: 更新前矩阵坐标: ({currentRow}, {currentColumn})");
            
            currentRow = nearestBlock.row;
            currentColumn = nearestBlock.column;
            currentBlock = nearestBlock;
            
            Debug.Log($"PlayerController: 更新后矩阵坐标: ({currentRow}, {currentColumn}) - {nearestBlock.blockName}");
        }
        else
        {
            Debug.LogWarning("PlayerController: 无法找到最近的地块来更新矩阵坐标");
        }
    }

    public void HandleTips()
    {
        if (hasRememberedPosition)
        {
            GameMgr.Instance.firstInteractTipUI.SetActive(false);
            GameMgr.Instance.secondInteractTipUI.SetActive(true);
        }
        else if (HasNearInteractableItem())
        {
            GameMgr.Instance.firstInteractTipUI.SetActive(true);
            GameMgr.Instance.secondInteractTipUI.SetActive(false);
        }
        else
        {
            GameMgr.Instance.firstInteractTipUI.SetActive(false);
            GameMgr.Instance.secondInteractTipUI.SetActive(false);
        }

    }
    
    void OnDestroy()
    {
        // 组件销毁时取消事件订阅，防止内存泄漏
        UnsubscribeFromInputEvents();
    }
}
