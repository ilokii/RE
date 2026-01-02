using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using SaveSystem;

/// <summary>
/// 存档槽位UI组件
/// 用于显示单个存档槽位的信息（章节名、时间、截图等）
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private Button slotButton;
    [SerializeField] private Button deleteButton; // 【新增】删除按钮
    [SerializeField] private Image screenshotImage;
    [SerializeField] private TextMeshProUGUI chapterNameText;
    [SerializeField] private TextMeshProUGUI saveTimeText;
    [SerializeField] private TextMeshProUGUI playTimeText;
    [SerializeField] private GameObject emptySlotIndicator;
    [SerializeField] private TextMeshProUGUI slotIdText;

    [Header("空槽位显示")]
    [SerializeField] private string emptySlotText = "空槽位";
    [SerializeField] private Sprite defaultScreenshot;

    [Header("删除功能设置")]
    [SerializeField] private bool showDeleteButton = true; // 是否显示删除按钮

    /// <summary>
    /// 当前槽位ID
    /// </summary>
    private int currentSlotId;

    /// <summary>
    /// 槽位是否为空
    /// </summary>
    private bool isEmpty;

    /// <summary>
    /// 点击回调
    /// </summary>
    private Action<int> onSlotClicked;

    /// <summary>
    /// 删除回调
    /// </summary>
    private Action<int> onDeleteClicked;

    /// <summary>
    /// 初始化存档槽位UI
    /// </summary>
    /// <param name="metadata">存档元数据</param>
    /// <param name="clickCallback">点击回调函数</param>
    /// <param name="deleteCallback">删除回调函数（可选）</param>
    public void Initialize(SaveSlotMeta metadata, Action<int> clickCallback, Action<int> deleteCallback = null)
    {
        if (metadata == null)
        {
            Debug.LogError("[SaveSlotUI] 元数据为空");
            return;
        }

        currentSlotId = metadata.SlotId;
        isEmpty = metadata.IsEmpty;
        onSlotClicked = clickCallback;
        onDeleteClicked = deleteCallback;

        Debug.Log($"[SaveSlotUI] 初始化槽位 {currentSlotId}: isEmpty={isEmpty}, hasClickCallback={clickCallback != null}, hasDeleteCallback={deleteCallback != null}");

        // 更新UI显示
        UpdateUI(metadata);

        // 绑定按钮事件
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnButtonClicked);
            Debug.Log($"[SaveSlotUI] 槽位 {currentSlotId} 主按钮事件已绑定");
        }
        else
        {
            Debug.LogWarning($"[SaveSlotUI] 槽位 {currentSlotId} 的 slotButton 未配置！");
        }

        // 绑定删除按钮事件
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    /// <param name="metadata">存档元数据</param>
    private void UpdateUI(SaveSlotMeta metadata)
    {
        // 显示槽位ID
        if (slotIdText != null)
        {
            string slotName = metadata.SlotId == -1 ? "自动存档" : $"存档 {metadata.SlotId + 1}";
            slotIdText.text = slotName;
        }

        if (metadata.IsEmpty)
        {
            // 空槽位显示
            ShowEmptySlot();
        }
        else
        {
            // 有存档显示
            ShowSaveData(metadata);
        }
    }

    /// <summary>
    /// 显示空槽位状态
    /// </summary>
    private void ShowEmptySlot()
    {
        // 显示空槽位指示器
        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(true);

        // 隐藏或清空其他UI元素
        if (chapterNameText != null)
        {
            chapterNameText.text = emptySlotText;
            chapterNameText.gameObject.SetActive(true);
        }

        if (saveTimeText != null)
            saveTimeText.gameObject.SetActive(false);

        if (playTimeText != null)
            playTimeText.gameObject.SetActive(false);

        if (screenshotImage != null)
        {
            screenshotImage.sprite = defaultScreenshot;
            screenshotImage.color = new Color(1f, 1f, 1f, 0.3f); // 半透明
        }

        // 【修复】空槽位的可交互性取决于是否有点击回调
        // 如果有回调（Save模式），应该可以点击
        // 如果没有回调（Load模式的空槽位），不可点击
        if (slotButton != null)
        {
            slotButton.interactable = (onSlotClicked != null);
            Debug.Log($"[SaveSlotUI] 空槽位 {currentSlotId} 按钮可交互性: {slotButton.interactable}");
        }

        // 【新增】隐藏删除按钮（空槽位无需删除）
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示存档数据
    /// </summary>
    /// <param name="metadata">存档元数据</param>
    private void ShowSaveData(SaveSlotMeta metadata)
    {
        // 隐藏空槽位指示器
        if (emptySlotIndicator != null)
            emptySlotIndicator.SetActive(false);

        // 显示章节名称
        if (chapterNameText != null)
        {
            chapterNameText.text = metadata.ChapterName;
            chapterNameText.gameObject.SetActive(true);
        }

        // 显示保存时间
        if (saveTimeText != null)
        {
            saveTimeText.text = metadata.SaveTime;
            saveTimeText.gameObject.SetActive(true);
        }

        // 显示游玩时长
        if (playTimeText != null)
        {
            playTimeText.text = $"游玩时长: {metadata.GetFormattedPlayTime()}";
            playTimeText.gameObject.SetActive(true);
        }

        // 加载截图
        LoadScreenshot(metadata.ScreenshotPath);

        // 有存档的槽位可以点击
        if (slotButton != null)
            slotButton.interactable = true;

        // 【新增】显示删除按钮（如果启用）
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(showDeleteButton && onDeleteClicked != null);
    }

    /// <summary>
    /// 加载截图
    /// </summary>
    /// <param name="screenshotPath">截图文件名</param>
    private void LoadScreenshot(string screenshotPath)
    {
        if (screenshotImage == null)
            return;

        if (string.IsNullOrEmpty(screenshotPath))
        {
            screenshotImage.sprite = defaultScreenshot;
            screenshotImage.color = Color.white;
            return;
        }

        // 构建完整路径
        string fullPath = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots", screenshotPath);

        if (System.IO.File.Exists(fullPath))
        {
            // 加载截图
            byte[] fileData = System.IO.File.ReadAllBytes(fullPath);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(fileData))
            {
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                screenshotImage.sprite = sprite;
                screenshotImage.color = Color.white;
            }
            else
            {
                Debug.LogWarning($"[SaveSlotUI] 无法加载截图: {fullPath}");
                screenshotImage.sprite = defaultScreenshot;
                screenshotImage.color = Color.white;
            }
        }
        else
        {
            Debug.LogWarning($"[SaveSlotUI] 截图文件不存在: {fullPath}");
            screenshotImage.sprite = defaultScreenshot;
            screenshotImage.color = Color.white;
        }
    }

    /// <summary>
    /// 按钮点击事件
    /// </summary>
    private void OnButtonClicked()
    {
        Debug.Log($"[SaveSlotUI] OnButtonClicked 被调用: 槽位={currentSlotId}, isEmpty={isEmpty}, hasCallback={onSlotClicked != null}");

        // 【修复】在Save模式下，空槽位也应该可以点击
        // 所以移除isEmpty检查，让回调函数自己决定如何处理
        
        // 调用回调函数
        if (onSlotClicked != null)
        {
            Debug.Log($"[SaveSlotUI] 调用槽位点击回调: {currentSlotId}");
            onSlotClicked.Invoke(currentSlotId);
        }
        else
        {
            Debug.LogWarning($"[SaveSlotUI] 槽位 {currentSlotId} 没有点击回调函数！");
        }
    }

    /// <summary>
    /// 删除按钮点击事件
    /// </summary>
    private void OnDeleteButtonClicked()
    {
        if (isEmpty)
        {
            Debug.LogWarning($"[SaveSlotUI] 槽位 {currentSlotId} 为空，无法删除");
            return;
        }

        Debug.Log($"[SaveSlotUI] 请求删除槽位 {currentSlotId}");

        // 调用删除回调
        onDeleteClicked?.Invoke(currentSlotId);
    }

    /// <summary>
    /// 设置按钮可交互状态
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetInteractable(bool interactable)
    {
        if (slotButton != null)
            slotButton.interactable = interactable && !isEmpty;
    }

    /// <summary>
    /// 设置是否显示删除按钮
    /// </summary>
    /// <param name="show">是否显示</param>
    public void SetShowDeleteButton(bool show)
    {
        showDeleteButton = show;
        
        // 立即更新删除按钮显示状态
        if (deleteButton != null && !isEmpty)
        {
            deleteButton.gameObject.SetActive(show && onDeleteClicked != null);
        }
    }
}
