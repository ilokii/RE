using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 通用确认对话框组件
/// 用于显示需要用户确认的操作（如删除存档）
/// </summary>
public class ConfirmDialog : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("按钮文本")]
    [SerializeField] private string confirmButtonText = "确认";
    [SerializeField] private string cancelButtonText = "取消";

    /// <summary>
    /// 确认回调
    /// </summary>
    private Action onConfirm;

    /// <summary>
    /// 取消回调
    /// </summary>
    private Action onCancel;

    private void Awake()
    {
        // 绑定按钮事件
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            
            // 设置按钮文本
            TextMeshProUGUI confirmText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (confirmText != null)
                confirmText.text = confirmButtonText;
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
            
            // 设置按钮文本
            TextMeshProUGUI cancelText = cancelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (cancelText != null)
                cancelText.text = cancelButtonText;
        }

        // 【重要】不在代码中隐藏，而是在Unity编辑器中手动设置为不激活
        // 因为在Awake中调用SetActive(false)会导致对象无法再被激活
        Debug.Log("[ConfirmDialog] Awake完成，等待Show()调用");
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    /// <param name="confirmCallback">确认回调</param>
    /// <param name="cancelCallback">取消回调（可选）</param>
    /// <param name="detail">详细信息（可选）</param>
    public void Show(string title, string message, Action confirmCallback, Action cancelCallback = null, string detail = null)
    {
        Debug.Log($"[ConfirmDialog] Show() 被调用: title={title}, dialogPanel={(dialogPanel != null ? "已配置" : "未配置")}");

        // 设置文本
        if (titleText != null)
        {
            titleText.text = title;
            Debug.Log($"[ConfirmDialog] 设置标题: {title}");
        }
        else
        {
            Debug.LogWarning("[ConfirmDialog] titleText 未配置！");
        }

        if (messageText != null)
        {
            messageText.text = message;
            Debug.Log($"[ConfirmDialog] 设置消息: {message}");
        }
        else
        {
            Debug.LogWarning("[ConfirmDialog] messageText 未配置！");
        }

        if (detailText != null)
        {
            if (!string.IsNullOrEmpty(detail))
            {
                detailText.text = detail;
                detailText.gameObject.SetActive(true);
                Debug.Log($"[ConfirmDialog] 设置详情: {detail}");
            }
            else
            {
                detailText.gameObject.SetActive(false);
            }
        }

        // 设置回调
        onConfirm = confirmCallback;
        onCancel = cancelCallback;

        // 【修复】同时激活GameObject和dialogPanel
        gameObject.SetActive(true);
        
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
            Debug.Log($"[ConfirmDialog] dialogPanel 已激活: {dialogPanel.activeSelf}");
        }
        
        Debug.Log($"[ConfirmDialog] ConfirmDialog GameObject 已激活: {gameObject.activeSelf}");
    }

    /// <summary>
    /// 隐藏确认对话框
    /// </summary>
    public void Hide()
    {
        // 【修复】隐藏整个ConfirmDialog对象
        gameObject.SetActive(false);

        // 清除回调
        onConfirm = null;
        onCancel = null;

        Debug.Log("[ConfirmDialog] 隐藏对话框");
    }

    /// <summary>
    /// 确认按钮点击事件
    /// </summary>
    private void OnConfirmClicked()
    {
        Debug.Log("[ConfirmDialog] 用户点击确认");

        // 调用确认回调
        onConfirm?.Invoke();

        // 隐藏对话框
        Hide();
    }

    /// <summary>
    /// 取消按钮点击事件
    /// </summary>
    private void OnCancelClicked()
    {
        Debug.Log("[ConfirmDialog] 用户点击取消");

        // 调用取消回调
        onCancel?.Invoke();

        // 隐藏对话框
        Hide();
    }

    /// <summary>
    /// 设置按钮文本
    /// </summary>
    /// <param name="confirmText">确认按钮文本</param>
    /// <param name="cancelText">取消按钮文本</param>
    public void SetButtonTexts(string confirmText, string cancelText)
    {
        confirmButtonText = confirmText;
        cancelButtonText = cancelText;

        // 立即更新按钮文本
        if (confirmButton != null)
        {
            TextMeshProUGUI textComponent = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
                textComponent.text = confirmText;
        }

        if (cancelButton != null)
        {
            TextMeshProUGUI textComponent = cancelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
                textComponent.text = cancelText;
        }
    }

    /// <summary>
    /// 检查对话框是否正在显示
    /// </summary>
    public bool IsShowing()
    {
        return dialogPanel != null && dialogPanel.activeSelf;
    }

    private void Update()
    {
        // 支持ESC键取消
        if (IsShowing() && Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancelClicked();
        }
    }
}
