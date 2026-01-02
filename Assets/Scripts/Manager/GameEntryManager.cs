using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏入口管理器 - 单例模式
/// 用于在主页和游戏场景之间传递游戏启动信息
/// 使用 DontDestroyOnLoad 确保在场景切换时不被销毁
/// </summary>
public class GameEntryManager : MonoBehaviour
{
    private static GameEntryManager _instance;

    /// <summary>
    /// 单例实例
    /// </summary>
    public static GameEntryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 尝试在场景中查找现有实例
                _instance = FindObjectOfType<GameEntryManager>();

                // 如果场景中不存在，创建新实例
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameEntryManager");
                    _instance = go.AddComponent<GameEntryManager>();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 标记是否从存档加载
    /// true: 从存档加载
    /// false: 新游戏
    /// </summary>
    public bool IsLoadingFromSave { get; private set; }

    /// <summary>
    /// 目标存档槽位ID
    /// 仅在 IsLoadingFromSave 为 true 时有效
    /// </summary>
    public int TargetSaveSlotId { get; private set; }

    /// <summary>
    /// 默认启动脚本文件名
    /// 用于新游戏时加载的对话文件
    /// </summary>
    public string DefaultStartScript { get; private set; } = "TestScript";

    private void Awake()
    {
        // 确保只有一个实例存在
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 开始新游戏
    /// 设置为新游戏模式并加载游戏场景
    /// </summary>
    /// <param name="startScript">可选：指定启动脚本文件名，如果不指定则使用默认值</param>
    public void StartNewGame(string startScript = null)
    {
        IsLoadingFromSave = false;
        TargetSaveSlotId = -1;

        if (!string.IsNullOrEmpty(startScript))
        {
            DefaultStartScript = startScript;
        }

        Debug.Log($"[GameEntryManager] 开始新游戏 - 启动脚本: {DefaultStartScript}");
        SceneManager.LoadScene("GameplayScene");
    }

    /// <summary>
    /// 从存档加载游戏
    /// 设置为读档模式，记录槽位ID并加载游戏场景
    /// </summary>
    /// <param name="slotId">存档槽位ID</param>
    public void LoadFromSave(int slotId)
    {
        IsLoadingFromSave = true;
        TargetSaveSlotId = slotId;

        Debug.Log($"[GameEntryManager] 从存档加载 - 槽位ID: {slotId}");
        SceneManager.LoadScene("GameplayScene");
    }

    /// <summary>
    /// 重置管理器状态
    /// 可用于返回主菜单时清理状态
    /// </summary>
    public void ResetState()
    {
        IsLoadingFromSave = false;
        TargetSaveSlotId = -1;
        DefaultStartScript = "TestScript";
        Debug.Log("[GameEntryManager] 状态已重置");
    }
}
