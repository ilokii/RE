using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SaveSystem;

/// <summary>
/// 存档面板模式枚举
/// </summary>
public enum PanelMode
{
    Save,  // 存档模式：点击槽位保存游戏
    Load   // 读档模式：点击槽位加载游戏
}

/// <summary>
/// 存档/读档面板控制器
/// 支持双模式：存档模式和读档模式
/// </summary>
public class SaveLoadPanelController : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button buttonBack;
    [SerializeField] private SaveSlotUI autoSaveSlot;
    [SerializeField] private SaveSlotUI[] manualSaveSlots = new SaveSlotUI[3];

    [Header("确认对话框")]
    [SerializeField] private ConfirmDialog confirmDialog;

    [Header("文本配置")]
    [SerializeField] private string saveModeTitle = "保存进度";
    [SerializeField] private string loadModeTitle = "读取存档";

    /// <summary>
    /// 当前面板模式
    /// </summary>
    private PanelMode currentMode;

    /// <summary>
    /// 是否在游戏场景中（而不是主菜单）
    /// </summary>
    private bool isInGameScene = false;

    /// <summary>
    /// 当前待保存的槽位ID（用于覆盖确认）
    /// </summary>
    private int currentSavingSlotId = -1;

    /// <summary>
    /// 当前章节名称（用于保存时的元数据）
    /// </summary>
    private string currentChapterName = "未知章节";

    /// <summary>
    /// 面板关闭回调
    /// </summary>
    private System.Action onPanelClosed;

    private bool isInitialized = false;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        // 确保在对象被激活时也进行初始化
        Initialize();
    }

    /// <summary>
    /// 初始化面板（确保只执行一次）
    /// </summary>
    private void Initialize()
    {
        if (isInitialized)
            return;

        // 绑定返回按钮
        if (buttonBack != null)
        {
            buttonBack.onClick.RemoveAllListeners();
            buttonBack.onClick.AddListener(OnBackClicked);
        }

        isInitialized = true;
        Debug.Log("[SaveLoadPanelController] 初始化完成");
    }

    /// <summary>
    /// 打开存档/读档面板
    /// </summary>
    /// <param name="mode">面板模式（Save或Load）</param>
    /// <param name="chapterName">当前章节名称（仅Save模式需要）</param>
    /// <param name="closeCallback">面板关闭回调（可选）</param>
    public void Open(PanelMode mode, string chapterName = null, System.Action closeCallback = null)
    {
        currentMode = mode;
        currentChapterName = chapterName ?? "未知章节";
        onPanelClosed = closeCallback;

        // 判断是否在游戏场景中
        isInGameScene = (GameEntryManager.Instance != null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenuScene");

        Debug.Log($"[SaveLoadPanelController] 打开面板: 模式={mode}, 场景={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        // 更新标题
        UpdateTitle();

        // 刷新槽位显示
        RefreshSlots();

        // 显示面板
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    /// <summary>
    /// 关闭面板
    /// </summary>
    public void Close()
    {
        Debug.Log("[SaveLoadPanelController] 关闭面板");

        if (panelRoot != null)
            panelRoot.SetActive(false);

        // 调用关闭回调
        onPanelClosed?.Invoke();
    }

    /// <summary>
    /// 更新标题文本
    /// </summary>
    private void UpdateTitle()
    {
        if (titleText != null)
        {
            titleText.text = currentMode == PanelMode.Save ? saveModeTitle : loadModeTitle;
        }
    }

    /// <summary>
    /// 刷新所有槽位显示
    /// </summary>
    private void RefreshSlots()
    {
        Debug.Log($"[SaveLoadPanelController] 刷新槽位，模式={currentMode}");

        // 刷新自动存档槽位
        if (autoSaveSlot != null)
        {
            SaveSlotMeta autoMeta = SaveManager.Instance.GetSlotMetadata(-1);

            if (currentMode == PanelMode.Save)
            {
                // 存档模式：自动存档槽位不可点击（只有系统能写）
                autoSaveSlot.Initialize(autoMeta, null, OnDeleteSlotRequested);
                autoSaveSlot.SetInteractable(false);
                Debug.Log("[SaveLoadPanelController] 自动存档槽位：Save模式，不可点击");
            }
            else
            {
                // 读档模式：自动存档可以读取
                autoSaveSlot.Initialize(autoMeta, OnSlotClicked, OnDeleteSlotRequested);
                Debug.Log("[SaveLoadPanelController] 自动存档槽位：Load模式，可点击");
            }
        }
        else
        {
            Debug.LogWarning("[SaveLoadPanelController] autoSaveSlot 未配置！");
        }

        // 刷新手动存档槽位
        for (int i = 0; i < manualSaveSlots.Length; i++)
        {
            if (manualSaveSlots[i] != null)
            {
                SaveSlotMeta meta = SaveManager.Instance.GetSlotMetadata(i);
                // 【重要】无论Save还是Load模式，手动存档都应该可以点击
                manualSaveSlots[i].Initialize(meta, OnSlotClicked, OnDeleteSlotRequested);
                Debug.Log($"[SaveLoadPanelController] 手动存档槽位 {i}：已初始化，模式={currentMode}");
            }
            else
            {
                Debug.LogWarning($"[SaveLoadPanelController] manualSaveSlots[{i}] 未配置！");
            }
        }

        Debug.Log("[SaveLoadPanelController] 槽位刷新完成");
    }

    /// <summary>
    /// 槽位点击回调
    /// </summary>
    /// <param name="slotId">槽位ID</param>
    private void OnSlotClicked(int slotId)
    {
        Debug.Log($"[SaveLoadPanelController] 槽位 {slotId} 被点击，模式={currentMode}");

        if (currentMode == PanelMode.Save)
        {
            // 存档模式
            HandleSaveSlotClicked(slotId);
        }
        else
        {
            // 读档模式
            HandleLoadSlotClicked(slotId);
        }
    }

    /// <summary>
    /// 处理存档模式下的槽位点击
    /// </summary>
    /// <param name="slotId">槽位ID</param>
    private void HandleSaveSlotClicked(int slotId)
    {
        // 检查槽位是否为空
        bool isEmpty = SaveManager.Instance.IsSlotEmpty(slotId);

        if (isEmpty)
        {
            // 空槽位：直接保存
            Debug.Log($"[SaveLoadPanelController] 保存到空槽位 {slotId}");
            SaveToSlot(slotId);
        }
        else
        {
            // 非空槽位：显示覆盖确认对话框
            ShowOverwriteConfirmDialog(slotId);
        }
    }

    /// <summary>
    /// 处理读档模式下的槽位点击
    /// </summary>
    /// <param name="slotId">槽位ID</param>
    private void HandleLoadSlotClicked(int slotId)
    {
        // 检查槽位是否为空
        if (SaveManager.Instance.IsSlotEmpty(slotId))
        {
            Debug.LogWarning($"[SaveLoadPanelController] 槽位 {slotId} 为空，无法加载");
            return;
        }

        Debug.Log($"[SaveLoadPanelController] 从槽位 {slotId} 加载游戏");

        if (isInGameScene)
        {
            // 游戏内读档：直接调用SaveManager
            bool success = SaveManager.Instance.LoadGame(slotId);

            if (success)
            {
                Debug.Log("[SaveLoadPanelController] 读档成功");
                Close();
            }
            else
            {
                Debug.LogError("[SaveLoadPanelController] 读档失败");
            }
        }
        else
        {
            // 主菜单读档：调用GameEntryManager
            GameEntryManager.Instance.LoadFromSave(slotId);
        }
    }

    /// <summary>
    /// 显示覆盖存档确认对话框
    /// </summary>
    /// <param name="slotId">槽位ID</param>
    private void ShowOverwriteConfirmDialog(int slotId)
    {
        if (confirmDialog == null)
        {
            Debug.LogError("[SaveLoadPanelController] ConfirmDialog 引用为空！");
            return;
        }

        // 获取存档元数据
        SaveSlotMeta metadata = SaveManager.Instance.GetSlotMetadata(slotId);

        // 构建详细信息
        string slotName = slotId == -1 ? "自动存档" : $"存档 {slotId + 1}";
        string detail = $"{slotName}\n{metadata.ChapterName}\n{metadata.SaveTime}";

        // 记录待保存的槽位ID
        currentSavingSlotId = slotId;

        // 显示确认对话框
        confirmDialog.Show(
            "覆盖存档",
            "此槽位已有存档，确定要覆盖吗？",
            OnConfirmOverwrite,
            OnCancelOverwrite,
            detail
        );

        Debug.Log($"[SaveLoadPanelController] 显示覆盖确认对话框: 槽位 {slotId}");
    }

    /// <summary>
    /// 确认覆盖回调
    /// </summary>
    private void OnConfirmOverwrite()
    {
        Debug.Log($"[SaveLoadPanelController] 确认覆盖槽位 {currentSavingSlotId}");
        SaveToSlot(currentSavingSlotId);
        currentSavingSlotId = -1;
    }

    /// <summary>
    /// 取消覆盖回调
    /// </summary>
    private void OnCancelOverwrite()
    {
        Debug.Log($"[SaveLoadPanelController] 取消覆盖槽位 {currentSavingSlotId}");
        currentSavingSlotId = -1;
    }

    /// <summary>
    /// 保存到指定槽位
    /// </summary>
    /// <param name="slotId">槽位ID</param>
    private async void SaveToSlot(int slotId)
    {
        Debug.Log($"[SaveLoadPanelController] 开始保存到槽位 {slotId}");

        // 调用SaveManager保存
        bool success = await SaveManager.Instance.SaveGameAsync(slotId, currentChapterName);

        if (success)
        {
            Debug.Log($"[SaveLoadPanelController] 槽位 {slotId} 保存成功");

            // 刷新UI显示
            RefreshSlots();

            // 可以显示保存成功提示
            // ShowSaveSuccessNotification();
        }
        else
        {
            Debug.LogError($"[SaveLoadPanelController] 槽位 {slotId} 保存失败");
        }
    }

    /// <summary>
    /// 删除存档请求回调
    /// </summary>
    /// <param name="slotId">槽位ID</param>
    private void OnDeleteSlotRequested(int slotId)
    {
        // 检查槽位是否为空
        if (SaveManager.Instance.IsSlotEmpty(slotId))
        {
            Debug.LogWarning($"[SaveLoadPanelController] 槽位 {slotId} 为空，无需删除");
            return;
        }

        // 显示删除确认对话框
        ShowDeleteConfirmDialog(slotId);
    }

    /// <summary>
    /// 显示删除确认对话框
    /// </summary>
    /// <param name="slotId">槽位ID</param>
    private void ShowDeleteConfirmDialog(int slotId)
    {
        if (confirmDialog == null)
        {
            Debug.LogError("[SaveLoadPanelController] ConfirmDialog 引用为空！");
            return;
        }

        // 获取存档元数据
        SaveSlotMeta metadata = SaveManager.Instance.GetSlotMetadata(slotId);

        // 构建详细信息
        string slotName = slotId == -1 ? "自动存档" : $"存档 {slotId + 1}";
        string detail = $"{slotName}\n{metadata.ChapterName}\n{metadata.SaveTime}";

        // 显示确认对话框
        confirmDialog.Show(
            "确认删除",
            "确定要删除此存档吗？\n此操作无法撤销。",
            () => OnConfirmDelete(slotId),
            null,
            detail
        );

        Debug.Log($"[SaveLoadPanelController] 显示删除确认对话框: 槽位 {slotId}");
    }

    /// <summary>
    /// 确认删除回调
    /// </summary>
    /// <param name="slotId">槽位ID</param>
    private void OnConfirmDelete(int slotId)
    {
        Debug.Log($"[SaveLoadPanelController] 确认删除槽位 {slotId}");

        // 调用SaveManager删除存档
        bool success = SaveManager.Instance.DeleteSave(slotId);

        if (success)
        {
            Debug.Log($"[SaveLoadPanelController] 槽位 {slotId} 删除成功");

            // 刷新UI显示
            RefreshSlots();
        }
        else
        {
            Debug.LogError($"[SaveLoadPanelController] 槽位 {slotId} 删除失败");
        }
    }

    /// <summary>
    /// 返回按钮点击事件
    /// </summary>
    private void OnBackClicked()
    {
        Debug.Log("[SaveLoadPanelController] 点击返回按钮");
        Close();
    }

    /// <summary>
    /// 设置当前章节名称（用于保存时的元数据）
    /// </summary>
    /// <param name="chapterName">章节名称</param>
    public void SetChapterName(string chapterName)
    {
        currentChapterName = chapterName;
    }
}
