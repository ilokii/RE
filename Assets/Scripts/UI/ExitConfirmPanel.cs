using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 退出确认面板
/// 提供"保存并退出"、"直接退出"、"取消"三个选项
/// </summary>
public class ExitConfirmPanel : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button buttonSaveAndExit;
    [SerializeField] private Button buttonDirectExit;
    [SerializeField] private Button buttonCancel;

    [Header("文本配置")]
    [SerializeField] private string titleString = "返回主菜单";
    [SerializeField] private string messageString = "即将返回主标题，未保存的进度将会丢失。";

    /// <summary>
    /// 保存并退出回调
    /// </summary>
    private Action onSaveAndExit;

    /// <summary>
    /// 直接退出回调
    /// </summary>
    private Action onDirectExit;

    /// <summary>
    /// 取消回调
    /// </summary>
    private Action onCancel;

    private void Awake()
    {
        // 绑定按钮事件
        if (buttonSaveAndExit != null)
            buttonSaveAndExit.onClick.AddListener(OnSaveAndExitClicked);

        if (buttonDirectExit != null)
            buttonDirectExit.onClick.AddListener(OnDirectExitClicked);

        if (buttonCancel != null)
            buttonCancel.onClick.AddListener(OnCancelClicked);

        // 默认隐藏面板（在Unity中手动设置为不激活）
        Debug.Log("[ExitConfirmPanel] Awake完成");
    }

    /// <summary>
    /// 显示退出确认面板
    /// </summary>
    /// <param name="saveAndExitCallback">保存并退出回调</param>
    /// <param name="directExitCallback">直接退出回调</param>
    /// <param name="cancelCallback">取消回调（可选）</param>
    public void Show(Action saveAndExitCallback, Action directExitCallback, Action cancelCallback = null)
    {
        // 设置文本
        if (titleText != null)
            titleText.text = titleString;

        if (messageText != null)
            messageText.text = messageString;

        // 设置回调
        onSaveAndExit = saveAndExitCallback;
        onDirectExit = directExitCallback;
        onCancel = cancelCallback;

        // 显示面板
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            Debug.Log("[ExitConfirmPanel] 面板已显示");
        }
        else
        {
            // 如果panelRoot未配置，激活自身
            gameObject.SetActive(true);
            Debug.Log("[ExitConfirmPanel] 面板已显示（使用gameObject）");
        }
    }

    /// <summary>
    /// 隐藏退出确认面板
    /// </summary>
    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        // 清除回调
        onSaveAndExit = null;
        onDirectExit = null;
        onCancel = null;

        Debug.Log("[ExitConfirmPanel] 面板已隐藏");
    }

    /// <summary>
    /// 保存并退出按钮点击事件
    /// </summary>
    private void OnSaveAndExitClicked()
    {
        Debug.Log("[ExitConfirmPanel] 用户选择：保存并退出");

        // 调用回调
        onSaveAndExit?.Invoke();

        // 隐藏面板
        Hide();
    }

    /// <summary>
    /// 直接退出按钮点击事件
    /// </summary>
    private void OnDirectExitClicked()
    {
        Debug.Log("[ExitConfirmPanel] 用户选择：直接退出");

        // 调用回调
        onDirectExit?.Invoke();

        // 隐藏面板
        Hide();
    }

    /// <summary>
    /// 取消按钮点击事件
    /// </summary>
    private void OnCancelClicked()
    {
        Debug.Log("[ExitConfirmPanel] 用户选择：取消");

        // 调用回调
        onCancel?.Invoke();

        // 隐藏面板
        Hide();
    }

    /// <summary>
    /// 设置提示文本
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="message">消息</param>
    public void SetTexts(string title, string message)
    {
        titleString = title;
        messageString = message;

        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;
    }

    private void Update()
    {
        // 支持ESC键取消
        if ((panelRoot != null && panelRoot.activeSelf) || gameObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnCancelClicked();
            }
        }
    }
}
