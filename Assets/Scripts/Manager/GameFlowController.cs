using UnityEngine;
using SaveSystem;

/// <summary>
/// 游戏流程控制器
/// 负责游戏场景的初始化，根据GameEntryManager的指令决定是新游戏还是读档
/// </summary>
public class GameFlowController : MonoBehaviour
{
    [Header("管理器引用")]
    [SerializeField] private DialogueManager dialogueManager;

    [Header("新游戏设置")]
    [SerializeField] private string defaultStartScript = "TestScript";
    [SerializeField] private string defaultBackground;
    [SerializeField] private string defaultBGM;

    /// <summary>
    /// 是否已经初始化
    /// </summary>
    private bool isInitialized = false;

    private void Start()
    {
        // 确保只初始化一次
        if (isInitialized)
        {
            Debug.LogWarning("[GameFlowController] 已经初始化过，跳过");
            return;
        }

        // 【重要】先确保DialogueManager注册到SaveManager
        if (dialogueManager != null)
        {
            dialogueManager.SetControlledByGameFlow(true);
            
            // 强制DialogueManager先注册（如果还没注册）
            SaveManager.Instance.RegisterSavable(dialogueManager);
            Debug.Log("[GameFlowController] 已确保DialogueManager注册到SaveManager");
        }

        InitializeGame();
        isInitialized = true;
    }

    /// <summary>
    /// 初始化游戏
    /// 根据GameEntryManager的状态决定是新游戏还是读档
    /// </summary>
    private void InitializeGame()
    {
        // 检查GameEntryManager是否存在
        if (GameEntryManager.Instance == null)
        {
            Debug.LogWarning("[GameFlowController] GameEntryManager不存在，默认启动新游戏");
            StartNewGame();
            return;
        }

        // 检查是否从存档加载
        if (GameEntryManager.Instance.IsLoadingFromSave)
        {
            LoadFromSave();
        }
        else
        {
            StartNewGame();
        }
    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    private void StartNewGame()
    {
        Debug.Log("[GameFlowController] 开始新游戏");

        // 重置游戏状态
        ResetGameState();

        // 获取启动脚本名称
        string startScript = defaultStartScript;
        if (GameEntryManager.Instance != null && !string.IsNullOrEmpty(GameEntryManager.Instance.DefaultStartScript))
        {
            startScript = GameEntryManager.Instance.DefaultStartScript;
        }

        Debug.Log($"[GameFlowController] 加载启动脚本: {startScript}");

        // 设置初始背景和音乐（可选）
        SetupInitialScene();

        // 启动对话系统
        if (dialogueManager != null)
        {
            dialogueManager.LoadScript(startScript);
        }
        else
        {
            Debug.LogError("[GameFlowController] DialogueManager引用为空！");
        }
    }

    /// <summary>
    /// 从存档加载游戏
    /// </summary>
    private void LoadFromSave()
    {
        int slotId = GameEntryManager.Instance.TargetSaveSlotId;
        Debug.Log($"[GameFlowController] 从存档加载游戏，槽位ID: {slotId}");

        // 调用SaveManager加载存档
        bool success = SaveManager.Instance.LoadGame(slotId);

        if (success)
        {
            Debug.Log("[GameFlowController] 存档加载成功");

            // 存档加载成功后，DialogueManager会通过ISavable接口自动恢复状态
            // 包括：当前脚本文件、当前行号、背景、BGM等
            // 不需要手动调用LoadScript，因为RestoreState会处理

            // 如果需要额外的初始化逻辑，可以在这里添加
        }
        else
        {
            Debug.LogError($"[GameFlowController] 存档加载失败，槽位ID: {slotId}");
            Debug.LogWarning("[GameFlowController] 回退到新游戏模式");

            // 加载失败，回退到新游戏
            StartNewGame();
        }
    }

    /// <summary>
    /// 重置游戏状态
    /// 清除当前周目的数据，但保留全局数据（结局、CG等）
    /// </summary>
    private void ResetGameState()
    {
        Debug.Log("[GameFlowController] 重置游戏状态");

        // 获取全局数据（保留跨周目数据）
        GlobalData globalData = SaveManager.Instance.GetGlobalData();

        // 注意：这里不清除全局数据，因为它是跨周目的
        // 只需要确保当前周目的变量被重置
        // 这些变量通常在各个Manager中管理，这里不需要特别处理

        // 如果有需要重置的全局标志，可以在这里处理
        // 例如：globalData.GlobalFlags["某个临时标志"] = false;

        // 清空屏幕上的角色（如果有PortraitManager）
        if (PortraitManager.Instance != null)
        {
            PortraitManager.Instance.HideAllCharacters();
        }

        Debug.Log("[GameFlowController] 游戏状态重置完成");
    }

    /// <summary>
    /// 设置初始场景（背景和音乐）
    /// </summary>
    private void SetupInitialScene()
    {
        // 这里可以设置默认的背景和音乐
        // 或者让对话脚本自己通过CMD_BG和CMD_BGM来设置

        // 示例：如果需要预设背景和音乐
        /*
        if (dialogueManager != null)
        {
            if (!string.IsNullOrEmpty(defaultBackground))
            {
                // 通过反射或公共方法调用ChangeBackground
                // dialogueManager.ChangeBackground(defaultBackground);
            }

            if (!string.IsNullOrEmpty(defaultBGM))
            {
                // dialogueManager.PlayBGM(defaultBGM);
            }
        }
        */

        Debug.Log("[GameFlowController] 初始场景设置完成");
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("[GameFlowController] 返回主菜单");

        // 重置GameEntryManager状态
        if (GameEntryManager.Instance != null)
        {
            GameEntryManager.Instance.ResetState();
        }

        // 加载主菜单场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }

    /// <summary>
    /// 快速保存
    /// </summary>
    public async void QuickSave()
    {
        Debug.Log("[GameFlowController] 执行快速保存");

        // 获取当前章节名称（可以从DialogueManager或其他地方获取）
        string chapterName = "快速保存";

        bool success = await SaveManager.Instance.QuickSaveAsync(chapterName);

        if (success)
        {
            Debug.Log("[GameFlowController] 快速保存成功");
            // 可以显示UI提示
        }
        else
        {
            Debug.LogError("[GameFlowController] 快速保存失败");
        }
    }

    /// <summary>
    /// 快速读取
    /// </summary>
    public void QuickLoad()
    {
        Debug.Log("[GameFlowController] 执行快速读取");

        bool success = SaveManager.Instance.QuickLoad();

        if (success)
        {
            Debug.Log("[GameFlowController] 快速读取成功");
        }
        else
        {
            Debug.LogError("[GameFlowController] 快速读取失败");
        }
    }
}
