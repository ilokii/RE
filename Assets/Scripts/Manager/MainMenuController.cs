using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using SaveSystem;

/// <summary>
/// 主菜单控制器
/// 管理主菜单UI交互和场景切换逻辑
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("主面板引用")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button buttonNewGame;
    [SerializeField] private Button buttonContinue;
    [SerializeField] private Button buttonLoad;
    [SerializeField] private Button buttonQuit;

    [Header("存档/读档面板引用")]
    [SerializeField] private SaveLoadPanelController saveLoadPanelController;
    [SerializeField] private GameObject saveLoadPanel;
    [SerializeField] private Button buttonBack;
    [SerializeField] private SaveSlotUI autoSaveSlot;
    [SerializeField] private SaveSlotUI[] manualSaveSlots = new SaveSlotUI[3];

    [Header("确认对话框")]
    [SerializeField] private ConfirmDialog confirmDialog;

    [Header("UI文本")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("删除存档设置")]
    [SerializeField] private bool allowDeleteAutoSave = false; // 是否允许删除自动存档
    [SerializeField] private bool protectLastSave = false;     // 是否保护最后一个存档

    /// <summary>
    /// 最新存档的槽位ID（用于Continue功能）
    /// </summary>
    private int latestSaveSlotId = -1;

    /// <summary>
    /// 是否存在任何存档
    /// </summary>
    private bool hasAnySave = false;

    /// <summary>
    /// 当前待删除的槽位ID
    /// </summary>
    private int currentDeletingSlotId = -1;

    private void Start()
    {
        // 初始化UI状态
        InitializeUI();

        // 检查存档状态
        CheckSaveStatus();

        // 绑定按钮事件
        BindButtonEvents();
    }

    /// <summary>
    /// 初始化UI状态
    /// </summary>
    private void InitializeUI()
    {
        // 默认显示主面板，隐藏存档面板
        if (mainPanel != null)
            mainPanel.SetActive(true);

        if (saveLoadPanel != null)
            saveLoadPanel.SetActive(false);
    }

    /// <summary>
    /// 检查存档状态，确定Continue按钮是否可用
    /// </summary>
    private void CheckSaveStatus()
    {
        // 获取自动存档元数据
        SaveSlotMeta autoSaveMeta = SaveManager.Instance.GetSlotMetadata(-1);

        // 获取所有手动存档元数据
        List<SaveSlotMeta> manualSlots = SaveManager.Instance.GetAllManualSlots();

        // 收集所有非空存档
        List<SaveSlotMeta> allSaves = new List<SaveSlotMeta>();

        if (!autoSaveMeta.IsEmpty)
        {
            allSaves.Add(autoSaveMeta);
        }

        foreach (var slot in manualSlots)
        {
            if (!slot.IsEmpty)
            {
                allSaves.Add(slot);
            }
        }

        // 判断是否有存档
        hasAnySave = allSaves.Count > 0;

        if (hasAnySave)
        {
            // 找到时间戳最新的存档
            SaveSlotMeta latestSave = allSaves.OrderByDescending(s => s.SaveTime).First();
            latestSaveSlotId = latestSave.SlotId;

            Debug.Log($"[MainMenuController] 找到最新存档: 槽位 {latestSaveSlotId}, 时间 {latestSave.SaveTime}");

            // 启用Continue按钮
            if (buttonContinue != null)
            {
                buttonContinue.interactable = true;
            }
        }
        else
        {
            Debug.Log("[MainMenuController] 未找到任何存档");

            // 禁用Continue按钮
            if (buttonContinue != null)
            {
                buttonContinue.interactable = false;
            }
        }
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtonEvents()
    {
        if (buttonNewGame != null)
            buttonNewGame.onClick.AddListener(OnNewGameClicked);

        if (buttonContinue != null)
            buttonContinue.onClick.AddListener(OnContinueClicked);

        if (buttonLoad != null)
            buttonLoad.onClick.AddListener(OnLoadClicked);

        if (buttonQuit != null)
            buttonQuit.onClick.AddListener(OnQuitClicked);

        if (buttonBack != null)
            buttonBack.onClick.AddListener(OnBackClicked);
    }

    /// <summary>
    /// 新游戏按钮点击事件
    /// </summary>
    private void OnNewGameClicked()
    {
        Debug.Log("[MainMenuController] 开始新游戏");

        // 调用GameEntryManager开始新游戏
        GameEntryManager.Instance.StartNewGame();
    }

    /// <summary>
    /// 继续游戏按钮点击事件
    /// </summary>
    private void OnContinueClicked()
    {
        if (!hasAnySave)
        {
            Debug.LogWarning("[MainMenuController] 没有可继续的存档");
            return;
        }

        Debug.Log($"[MainMenuController] 继续游戏，加载槽位 {latestSaveSlotId}");

        // 调用GameEntryManager从最新存档加载
        GameEntryManager.Instance.LoadFromSave(latestSaveSlotId);
    }

    /// <summary>
    /// 读档按钮点击事件
    /// </summary>
    private void OnLoadClicked()
    {
        Debug.Log("[MainMenuController] 打开读档面板");

        // 隐藏主面板，显示存档面板
        if (mainPanel != null)
            mainPanel.SetActive(false);

        if (saveLoadPanel != null)
            saveLoadPanel.SetActive(true);

        // 刷新存档槽位显示
        RefreshSaveSlots();
    }

    /// <summary>
    /// 退出按钮点击事件
    /// </summary>
    private void OnQuitClicked()
    {
        Debug.Log("[MainMenuController] 退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 返回按钮点击事件
    /// </summary>
    private void OnBackClicked()
    {
        Debug.Log("[MainMenuController] 返回主菜单");

        // 显示主面板，隐藏存档面板
        if (mainPanel != null)
            mainPanel.SetActive(true);

        if (saveLoadPanel != null)
            saveLoadPanel.SetActive(false);
    }

    /// <summary>
    /// 刷新存档槽位显示
    /// </summary>
    private void RefreshSaveSlots()
    {
        // 刷新自动存档槽位
        if (autoSaveSlot != null)
        {
            SaveSlotMeta autoMeta = SaveManager.Instance.GetSlotMetadata(-1);
            
            // 根据设置决定是否允许删除自动存档
            if (allowDeleteAutoSave)
            {
                autoSaveSlot.Initialize(autoMeta, OnSaveSlotClicked, OnDeleteSlotRequested);
            }
            else
            {
                autoSaveSlot.Initialize(autoMeta, OnSaveSlotClicked);
                autoSaveSlot.SetShowDeleteButton(false); // 禁用自动存档的删除按钮
            }
        }

        // 刷新手动存档槽位
        for (int i = 0; i < manualSaveSlots.Length; i++)
        {
            if (manualSaveSlots[i] != null)
            {
                SaveSlotMeta meta = SaveManager.Instance.GetSlotMetadata(i);
                manualSaveSlots[i].Initialize(meta, OnSaveSlotClicked, OnDeleteSlotRequested);
            }
        }
    }

    /// <summary>
    /// 存档槽位点击回调
    /// </summary>
    /// <param name="slotId">槽位ID</param>
    private void OnSaveSlotClicked(int slotId)
    {
        // 检查槽位是否为空
        if (SaveManager.Instance.IsSlotEmpty(slotId))
        {
            Debug.LogWarning($"[MainMenuController] 槽位 {slotId} 为空，无法加载");
            return;
        }

        Debug.Log($"[MainMenuController] 从槽位 {slotId} 加载游戏");

        // 调用GameEntryManager加载存档
        GameEntryManager.Instance.LoadFromSave(slotId);
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
            Debug.LogWarning($"[MainMenuController] 槽位 {slotId} 为空，无需删除");
            return;
        }

        // 检查是否保护最后一个存档
        if (protectLastSave && CountNonEmptySaves() <= 1)
        {
            Debug.LogWarning("[MainMenuController] 这是最后一个存档，受保护无法删除");
            
            // 可以显示提示信息
            if (confirmDialog != null)
            {
                confirmDialog.Show(
                    "无法删除",
                    "这是最后一个存档，无法删除。",
                    null,
                    null
                );
            }
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
            Debug.LogError("[MainMenuController] ConfirmDialog 引用为空！");
            return;
        }

        // 获取存档元数据
        SaveSlotMeta metadata = SaveManager.Instance.GetSlotMetadata(slotId);

        // 构建详细信息
        string slotName = slotId == -1 ? "自动存档" : $"存档 {slotId + 1}";
        string detail = $"{slotName}\n{metadata.ChapterName}\n{metadata.SaveTime}";

        // 记录待删除的槽位ID
        currentDeletingSlotId = slotId;

        // 显示确认对话框
        confirmDialog.Show(
            "确认删除",
            "确定要删除此存档吗？\n此操作无法撤销。",
            OnConfirmDelete,
            OnCancelDelete,
            detail
        );

        Debug.Log($"[MainMenuController] 显示删除确认对话框: 槽位 {slotId}");
    }

    /// <summary>
    /// 确认删除回调
    /// </summary>
    private void OnConfirmDelete()
    {
        if (currentDeletingSlotId == -1 && !allowDeleteAutoSave)
        {
            Debug.LogWarning("[MainMenuController] 不允许删除自动存档");
            currentDeletingSlotId = -1;
            return;
        }

        Debug.Log($"[MainMenuController] 确认删除槽位 {currentDeletingSlotId}");

        // 调用SaveManager删除存档
        bool success = SaveManager.Instance.DeleteSave(currentDeletingSlotId);

        if (success)
        {
            Debug.Log($"[MainMenuController] 槽位 {currentDeletingSlotId} 删除成功");

            // 刷新UI显示
            RefreshSaveSlots();

            // 重新检查存档状态（更新Continue按钮）
            CheckSaveStatus();
        }
        else
        {
            Debug.LogError($"[MainMenuController] 槽位 {currentDeletingSlotId} 删除失败");
        }

        // 重置待删除槽位ID
        currentDeletingSlotId = -1;
    }

    /// <summary>
    /// 取消删除回调
    /// </summary>
    private void OnCancelDelete()
    {
        Debug.Log($"[MainMenuController] 取消删除槽位 {currentDeletingSlotId}");
        currentDeletingSlotId = -1;
    }

    /// <summary>
    /// 统计非空存档数量
    /// </summary>
    /// <returns>非空存档数量</returns>
    private int CountNonEmptySaves()
    {
        int count = 0;

        // 检查自动存档
        if (!SaveManager.Instance.IsSlotEmpty(-1))
            count++;

        // 检查手动存档
        for (int i = 0; i < 3; i++)
        {
            if (!SaveManager.Instance.IsSlotEmpty(i))
                count++;
        }

        return count;
    }
}
