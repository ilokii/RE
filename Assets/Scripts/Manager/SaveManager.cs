using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SaveSystem;

/// <summary>
/// 存档管理器：实现异步无感存档，支持防坏档机制
/// </summary>
public class SaveManager : MonoBehaviour
{
    #region 单例模式
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SaveManager");
                _instance = go.AddComponent<SaveManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region 配置常量
    /// <summary>
    /// 手动存档槽位数量
    /// </summary>
    private const int MANUAL_SLOT_COUNT = 3;

    /// <summary>
    /// 自动存档槽位ID（特殊标识）
    /// </summary>
    private const int AUTO_SAVE_SLOT = -1;

    /// <summary>
    /// 自动存档时间间隔（秒）
    /// </summary>
    private const float AUTO_SAVE_INTERVAL = 600f; // 10分钟

    /// <summary>
    /// 全局数据文件名
    /// </summary>
    private const string GLOBAL_DATA_FILE = "global_data.json";

    /// <summary>
    /// 存档根目录
    /// </summary>
    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

    /// <summary>
    /// 截图根目录
    /// </summary>
    private string ScreenshotDirectory => Path.Combine(Application.persistentDataPath, "Screenshots");
    #endregion

    #region 数据管理
    /// <summary>
    /// 全局数据（跨周目）
    /// </summary>
    private GlobalData globalData;

    /// <summary>
    /// 已注册的可保存实体列表
    /// </summary>
    private List<ISavable> savableEntities = new List<ISavable>();

    /// <summary>
    /// 游戏开始时间（用于计算游玩时长）
    /// </summary>
    private DateTime gameStartTime;

    /// <summary>
    /// 累计游玩时长（秒）
    /// </summary>
    private long accumulatedPlayTime = 0;

    /// <summary>
    /// 当前游戏状态
    /// </summary>
    private GameState currentGameState = GameState.Idle;

    /// <summary>
    /// 自动存档计时器
    /// </summary>
    private float autoSaveTimer = 0f;

    /// <summary>
    /// 是否启用自动存档
    /// </summary>
    private bool autoSaveEnabled = true;

    /// <summary>
    /// 是否正在执行存档操作
    /// </summary>
    private bool isSaving = false;

    /// <summary>
    /// 预捕获的截图（用于游戏内存档，避免截到菜单）
    /// </summary>
    private Texture2D precapturedScreenshot = null;
    #endregion

    #region 初始化
    private void Start()
    {
        // 确保目录存在
        if (!Directory.Exists(SaveDirectory))
            Directory.CreateDirectory(SaveDirectory);
        if (!Directory.Exists(ScreenshotDirectory))
            Directory.CreateDirectory(ScreenshotDirectory);

        // 加载全局数据
        LoadGlobalData();

        // 记录游戏开始时间
        gameStartTime = DateTime.Now;
    }

    private void Update()
    {
        // 自动存档逻辑
        if (autoSaveEnabled && !isSaving)
        {
            autoSaveTimer += Time.deltaTime;

            // 当计时器达到间隔时间 且 游戏处于空闲状态
            if (autoSaveTimer >= AUTO_SAVE_INTERVAL && currentGameState == GameState.Idle)
            {
                Debug.Log("[SaveManager] 触发定时自动存档");
                TriggerAutoSave(AutoSaveTrigger.TimeInterval);
            }
        }
    }
    #endregion

    #region 实体注册
    /// <summary>
    /// 注册可保存实体（通常在 Manager 的 Awake/Start 中调用）
    /// </summary>
    public void RegisterSavable(ISavable savable)
    {
        if (!savableEntities.Contains(savable))
        {
            savableEntities.Add(savable);
            Debug.Log($"[SaveManager] 注册可保存实体: {savable.GetType().Name}");
        }
    }

    /// <summary>
    /// 注销可保存实体
    /// </summary>
    public void UnregisterSavable(ISavable savable)
    {
        savableEntities.Remove(savable);
    }
    #endregion

    #region 核心存档功能
    /// <summary>
    /// 异步保存游戏到指定槽位
    /// </summary>
    /// <param name="slotId">槽位ID（0-2为手动槽，-1为自动槽）</param>
    /// <param name="chapterName">当前章节名称</param>
    public async Task<bool> SaveGameAsync(int slotId, string chapterName = "未知章节")
    {
        try
        {
            Debug.Log($"[SaveManager] 开始保存游戏到槽位 {slotId}...");

            // === Step A: 主线程数据收集 ===
            SaveData saveData = new SaveData();

            // 1. 收集元数据
            saveData.Metadata.SlotId = slotId;
            saveData.Metadata.SaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            saveData.Metadata.ChapterName = chapterName;
            saveData.Metadata.PlayTimeSeconds = GetTotalPlayTime();
            saveData.Metadata.IsEmpty = false;

            // 2. 截图路径
            string screenshotFileName = GetScreenshotFileName(slotId);
            saveData.Metadata.ScreenshotPath = screenshotFileName;

            // 3. 收集游戏状态（从所有注册的 ISavable）
            saveData.ArchiveData = CaptureGameState();
            saveData.ArchiveData.CurrentChapter = chapterName;
            saveData.ArchiveData.SaveTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            saveData.ArchiveData.PlayTimeSeconds = saveData.Metadata.PlayTimeSeconds;

            // 4. 截图（主线程）
            string screenshotPath = Path.Combine(ScreenshotDirectory, screenshotFileName);
            
            // 【修改】如果有预捕获的截图，使用它；否则实时截图
            if (precapturedScreenshot != null)
            {
                // 使用预捕获的截图
                byte[] bytes = precapturedScreenshot.EncodeToPNG();
                File.WriteAllBytes(screenshotPath, bytes);
                Debug.Log($"[SaveManager] 使用预捕获截图保存: {screenshotPath}");
                
                // 清理预捕获的截图
                Destroy(precapturedScreenshot);
                precapturedScreenshot = null;
            }
            else
            {
                // 实时截图（用于自动存档等场景）
                ScreenCapture.CaptureScreenshot(screenshotPath);
                Debug.Log($"[SaveManager] 实时截图已保存: {screenshotPath}");
            }

            // 深拷贝数据（防止后台线程写入时主线程修改数据）
            string jsonData = JsonUtility.ToJson(saveData, true);

            // ⚠️ 重要：在主线程中获取文件路径（因为后台线程不能访问 Unity API）
            string saveFilePath = GetSaveFilePath(slotId);
            string tempFilePath = saveFilePath + ".tmp";

            // === Step B: 后台线程序列化与写入 ===
            bool success = await Task.Run(() =>
            {
                try
                {

                    // 写入临时文件
                    File.WriteAllText(tempFilePath, jsonData);

                    // === Step C: 原子操作 ===
                    // 删除旧文件（如果存在）
                    if (File.Exists(saveFilePath))
                    {
                        File.Delete(saveFilePath);
                    }

                    // 重命名临时文件为正式文件
                    File.Move(tempFilePath, saveFilePath);

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveManager] 后台写入失败: {ex.Message}");
                    return false;
                }
            });

            if (success)
            {
                Debug.Log($"[SaveManager] 游戏已成功保存到槽位 {slotId}");
            }

            return success;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 保存游戏失败: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 同步加载游戏
    /// </summary>
    public bool LoadGame(int slotId)
    {
        try
        {
            string saveFilePath = GetSaveFilePath(slotId);

            if (!File.Exists(saveFilePath))
            {
                Debug.LogWarning($"[SaveManager] 存档文件不存在: {saveFilePath}");
                return false;
            }

            // 读取并反序列化
            string jsonData = File.ReadAllText(saveFilePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);

            if (saveData == null || saveData.ArchiveData == null)
            {
                Debug.LogError("[SaveManager] 存档数据损坏或格式错误");
                return false;
            }

            // 恢复游戏状态
            RestoreGameState(saveData.ArchiveData);

            // 重置游玩时间计数器
            accumulatedPlayTime = saveData.ArchiveData.PlayTimeSeconds;
            gameStartTime = DateTime.Now;

            Debug.Log($"[SaveManager] 游戏已从槽位 {slotId} 加载成功");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 加载游戏失败: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 获取存档槽位元数据（用于UI显示，不加载完整存档）
    /// </summary>
    public SaveSlotMeta GetSlotMetadata(int slotId)
    {
        try
        {
            string saveFilePath = GetSaveFilePath(slotId);

            if (!File.Exists(saveFilePath))
            {
                // 返回空槽位
                return new SaveSlotMeta
                {
                    SlotId = slotId,
                    IsEmpty = true
                };
            }

            // 只读取元数据部分（这里简化处理，实际可以优化为只读取部分JSON）
            string jsonData = File.ReadAllText(saveFilePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);

            return saveData.Metadata;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 读取槽位元数据失败: {ex.Message}");
            return new SaveSlotMeta { SlotId = slotId, IsEmpty = true };
        }
    }

    /// <summary>
    /// 删除存档槽位
    /// </summary>
    public bool DeleteSave(int slotId)
    {
        try
        {
            string saveFilePath = GetSaveFilePath(slotId);
            string screenshotPath = Path.Combine(ScreenshotDirectory, GetScreenshotFileName(slotId));

            if (File.Exists(saveFilePath))
                File.Delete(saveFilePath);

            if (File.Exists(screenshotPath))
                File.Delete(screenshotPath);

            Debug.Log($"[SaveManager] 已删除槽位 {slotId} 的存档");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 删除存档失败: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region 全局数据管理
    /// <summary>
    /// 保存全局数据（结局、CG、已读文本等）
    /// </summary>
    public void SaveGlobalData()
    {
        try
        {
            string filePath = Path.Combine(SaveDirectory, GLOBAL_DATA_FILE);
            string tempPath = filePath + ".tmp";

            // 序列化（注意：HashSet 需要特殊处理，这里简化为 List）
            string jsonData = JsonUtility.ToJson(globalData, true);

            // 原子写入
            File.WriteAllText(tempPath, jsonData);
            if (File.Exists(filePath))
                File.Delete(filePath);
            File.Move(tempPath, filePath);

            Debug.Log("[SaveManager] 全局数据已保存");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 保存全局数据失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载全局数据
    /// </summary>
    private void LoadGlobalData()
    {
        try
        {
            string filePath = Path.Combine(SaveDirectory, GLOBAL_DATA_FILE);

            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                globalData = JsonUtility.FromJson<GlobalData>(jsonData);
                Debug.Log("[SaveManager] 全局数据已加载");
            }
            else
            {
                // 首次运行，创建新的全局数据
                globalData = new GlobalData();
                Debug.Log("[SaveManager] 创建新的全局数据");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 加载全局数据失败: {ex.Message}");
            globalData = new GlobalData();
        }
    }

    /// <summary>
    /// 获取全局数据引用
    /// </summary>
    public GlobalData GetGlobalData()
    {
        return globalData;
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 捕获当前游戏状态（从所有 ISavable 实体）
    /// </summary>
    private ArchiveData CaptureGameState()
    {
        ArchiveData archiveData = new ArchiveData();

        foreach (var savable in savableEntities)
        {
            try
            {
                object state = savable.CaptureState();

                // 根据实体类型分配到对应字段
                // 这里需要根据具体的 Manager 类型来处理
                // 示例：如果是 DialogueManager，则提取对话状态
                if (savable is DialogueManager dialogueManager && state is DialogueManager.DialogueManagerState dialogueState)
                {
                    archiveData.CurrentScriptFile = dialogueState.CurrentScriptFile;
                    archiveData.CurrentLineIndex = dialogueState.CurrentLineIndex;
                    archiveData.BackgroundImageId = dialogueState.CurrentBackground;
                    archiveData.BGMName = dialogueState.CurrentBGM;
                    archiveData.BGMVolume = dialogueState.BGMVolume;
                }
                // 【新增】处理 PortraitManager 的角色状态
                else if (savable is PortraitManager && state is List<CharacterState> characterStates)
                {
                    archiveData.Characters = characterStates;
                    Debug.Log($"[SaveManager] 捕获了 {characterStates.Count} 个角色状态");
                }
                // 可以继续添加其他 Manager 的处理逻辑
                // 例如：PuzzleManager、InventoryManager 等
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] 捕获状态失败 ({savable.GetType().Name}): {ex.Message}");
            }
        }

        return archiveData;
    }

    /// <summary>
    /// 恢复游戏状态（到所有 ISavable 实体）
    /// </summary>
    private void RestoreGameState(ArchiveData archiveData)
    {
        // 【重要】分两个阶段恢复，确保正确的执行顺序
        
        // === 阶段1：先恢复PortraitManager（角色立绘）===
        foreach (var savable in savableEntities)
        {
            if (savable is PortraitManager)
            {
                try
                {
                    if (archiveData.Characters != null && archiveData.Characters.Count > 0)
                    {
                        savable.RestoreState(archiveData.Characters);
                        Debug.Log($"[SaveManager] [阶段1] 恢复了 {archiveData.Characters.Count} 个角色状态");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveManager] 恢复PortraitManager失败: {ex.Message}");
                }
            }
        }

        // === 阶段2：再恢复DialogueManager（对话状态）===
        foreach (var savable in savableEntities)
        {
            try
            {
                // 跳过PortraitManager（已在阶段1处理）
                if (savable is PortraitManager)
                    continue;

                // 恢复DialogueManager
                if (savable is DialogueManager)
                {
                    DialogueManager.DialogueManagerState state = new DialogueManager.DialogueManagerState
                    {
                        CurrentScriptFile = archiveData.CurrentScriptFile,
                        CurrentLineIndex = archiveData.CurrentLineIndex,
                        CurrentBackground = archiveData.BackgroundImageId,
                        CurrentBGM = archiveData.BGMName,
                        BGMVolume = archiveData.BGMVolume
                    };
                    savable.RestoreState(state);
                    Debug.Log("[SaveManager] [阶段2] 恢复了DialogueManager状态");
                }
                // 继续添加其他 Manager 的恢复逻辑
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] 恢复状态失败 ({savable.GetType().Name}): {ex.Message}");
            }
        }

        Debug.Log("[SaveManager] 游戏状态恢复完成");
    }

    /// <summary>
    /// 获取存档文件路径
    /// </summary>
    private string GetSaveFilePath(int slotId)
    {
        string fileName = slotId == AUTO_SAVE_SLOT ? "auto_save.json" : $"save_{slotId}.json";
        return Path.Combine(SaveDirectory, fileName);
    }

    /// <summary>
    /// 获取截图文件名
    /// </summary>
    private string GetScreenshotFileName(int slotId)
    {
        return slotId == AUTO_SAVE_SLOT ? "auto_save.png" : $"save_{slotId}.png";
    }

    /// <summary>
    /// 获取总游玩时长（秒）
    /// </summary>
    private long GetTotalPlayTime()
    {
        long currentSessionTime = (long)(DateTime.Now - gameStartTime).TotalSeconds;
        return accumulatedPlayTime + currentSessionTime;
    }
    #endregion

    #region 便捷API
    /// <summary>
    /// 快速保存到自动槽位
    /// </summary>
    public async Task<bool> QuickSaveAsync(string chapterName = "快速保存")
    {
        return await SaveGameAsync(AUTO_SAVE_SLOT, chapterName);
    }

    /// <summary>
    /// 快速加载自动槽位
    /// </summary>
    public bool QuickLoad()
    {
        return LoadGame(AUTO_SAVE_SLOT);
    }

    /// <summary>
    /// 检查槽位是否为空
    /// </summary>
    public bool IsSlotEmpty(int slotId)
    {
        return !File.Exists(GetSaveFilePath(slotId));
    }

    /// <summary>
    /// 获取所有手动槽位的元数据
    /// </summary>
    public List<SaveSlotMeta> GetAllManualSlots()
    {
        List<SaveSlotMeta> slots = new List<SaveSlotMeta>();
        for (int i = 0; i < MANUAL_SLOT_COUNT; i++)
        {
            slots.Add(GetSlotMetadata(i));
        }
        return slots;
    }

    /// <summary>
    /// 设置当前游戏状态（用于判断是否可以自动存档）
    /// </summary>
    public void SetGameState(GameState state)
    {
        currentGameState = state;
        Debug.Log($"[SaveManager] 游戏状态切换: {state}");
    }

    /// <summary>
    /// 获取当前游戏状态
    /// </summary>
    public GameState GetGameState()
    {
        return currentGameState;
    }

    /// <summary>
    /// 启用/禁用自动存档
    /// </summary>
    public void SetAutoSaveEnabled(bool enabled)
    {
        autoSaveEnabled = enabled;
        Debug.Log($"[SaveManager] 自动存档已{(enabled ? "启用" : "禁用")}");
    }

    /// <summary>
    /// 设置预捕获的截图（用于游戏内存档）
    /// </summary>
    /// <param name="screenshot">预捕获的截图</param>
    public void SetPrecapturedScreenshot(Texture2D screenshot)
    {
        // 清理旧的截图
        if (precapturedScreenshot != null)
        {
            Destroy(precapturedScreenshot);
        }

        precapturedScreenshot = screenshot;
        Debug.Log($"[SaveManager] 已设置预捕获截图: {(screenshot != null ? $"{screenshot.width}x{screenshot.height}" : "null")}");
    }
    #endregion

    #region 自动存档系统
    /// <summary>
    /// 触发自动存档（可由外部事件调用）
    /// </summary>
    /// <param name="trigger">触发原因</param>
    /// <param name="forceImmediate">是否强制立即存档（忽略Idle检查）</param>
    public async void TriggerAutoSave(AutoSaveTrigger trigger, bool forceImmediate = false)
    {
        // 如果自动存档被禁用，则跳过
        if (!autoSaveEnabled && !forceImmediate)
        {
            Debug.Log($"[SaveManager] 自动存档已禁用，跳过触发: {trigger}");
            return;
        }

        // 如果正在存档，则跳过
        if (isSaving)
        {
            Debug.Log($"[SaveManager] 正在存档中，跳过触发: {trigger}");
            return;
        }

        // 检查游戏状态（除非强制立即存档）
        if (!forceImmediate && currentGameState != GameState.Idle)
        {
            Debug.Log($"[SaveManager] 游戏状态不是Idle ({currentGameState})，延迟自动存档: {trigger}");
            // 可以选择在这里设置一个标志，等待状态变为Idle时再存档
            return;
        }

        Debug.Log($"[SaveManager] 执行自动存档，触发原因: {trigger}");

        // 设置存档标志
        isSaving = true;

        // 根据触发类型生成章节名
        string chapterName = GetChapterNameByTrigger(trigger);

        // 执行异步存档
        bool success = await SaveGameAsync(AUTO_SAVE_SLOT, chapterName);

        // 重置标志和计时器
        isSaving = false;
        autoSaveTimer = 0f;

        if (success)
        {
            Debug.Log($"[SaveManager] 自动存档成功: {trigger}");
            // 可以在这里触发UI提示（例如显示"游戏已自动保存"）
            OnAutoSaveCompleted?.Invoke(trigger, true);
        }
        else
        {
            Debug.LogWarning($"[SaveManager] 自动存档失败: {trigger}");
            OnAutoSaveCompleted?.Invoke(trigger, false);
        }
    }

    /// <summary>
    /// 根据触发类型获取章节名称
    /// </summary>
    private string GetChapterNameByTrigger(AutoSaveTrigger trigger)
    {
        switch (trigger)
        {
            case AutoSaveTrigger.TimeInterval:
                return "自动存档";
            case AutoSaveTrigger.ChapterEnd:
                return "章节结束";
            case AutoSaveTrigger.PuzzleComplete:
                return "解密完成";
            case AutoSaveTrigger.DecisionMade:
                return "关键决策";
            default:
                return "自动存档";
        }
    }

    /// <summary>
    /// 自动存档完成事件（参数：触发类型，是否成功）
    /// </summary>
    public event System.Action<AutoSaveTrigger, bool> OnAutoSaveCompleted;
    #endregion
}
