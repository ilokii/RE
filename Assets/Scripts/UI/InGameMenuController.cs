using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏内系统菜单控制器
/// 处理暂停、存档、读档、返回主菜单等功能
/// </summary>
public class InGameMenuController : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button buttonResume;
    [SerializeField] private Button buttonSave;
    [SerializeField] private Button buttonLoad;
    [SerializeField] private Button buttonBackToTitle;
    [SerializeField] private Button buttonMenuToggle; // 右上角的Menu按钮（可选）

    [Header("面板引用")]
    [SerializeField] private SaveLoadPanelController saveLoadPanelController;
    [SerializeField] private ExitConfirmPanel exitConfirmPanel;
    [SerializeField] private ConfirmDialog confirmDialog; // 保留用于其他确认

    [Header("设置")]
    [SerializeField] private KeyCode menuToggleKey = KeyCode.Escape;
    [SerializeField] private bool pauseGameWhenOpen = true;

    /// <summary>
    /// 菜单是否打开
    /// </summary>
    private bool isMenuOpen = false;

    /// <summary>
    /// 当前章节名称（用于保存时的元数据）
    /// </summary>
    private string currentChapterName = "游戏进度";

    /// <summary>
    /// 预捕获的截图（在打开菜单时截取）
    /// </summary>
    private Texture2D precapturedScreenshot = null;

    private void Awake()
    {
        // 绑定按钮事件
        BindButtonEvents();

        // 默认隐藏菜单
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    private void Update()
    {
        // 监听菜单切换键
        if (Input.GetKeyDown(menuToggleKey))
        {
            ToggleMenu();
        }
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtonEvents()
    {
        if (buttonResume != null)
            buttonResume.onClick.AddListener(OnResumeClicked);

        if (buttonSave != null)
            buttonSave.onClick.AddListener(OnSaveClicked);

        if (buttonLoad != null)
            buttonLoad.onClick.AddListener(OnLoadClicked);

        if (buttonBackToTitle != null)
            buttonBackToTitle.onClick.AddListener(OnBackToTitleClicked);

        if (buttonMenuToggle != null)
            buttonMenuToggle.onClick.AddListener(ToggleMenu);
    }

    /// <summary>
    /// 切换菜单显示/隐藏
    /// </summary>
    public void ToggleMenu()
    {
        if (isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    /// <summary>
    /// 打开菜单
    /// </summary>
    public void OpenMenu()
    {
        if (isMenuOpen)
            return;

        Debug.Log("[InGameMenuController] 打开系统菜单");

        // 【重要】使用协程：先截图，再显示菜单
        StartCoroutine(OpenMenuWithScreenshotCoroutine());
    }

    /// <summary>
    /// 打开菜单协程（先截图再显示菜单）
    /// </summary>
    private System.Collections.IEnumerator OpenMenuWithScreenshotCoroutine()
    {
        // 等待帧结束（确保当前帧渲染完成，此时还没有菜单）
        yield return new WaitForEndOfFrame();

        // 清理旧的截图
        if (precapturedScreenshot != null)
        {
            Destroy(precapturedScreenshot);
            precapturedScreenshot = null;
        }

        // 截取当前屏幕（此时菜单还没显示）
        precapturedScreenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        precapturedScreenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        precapturedScreenshot.Apply();

        Debug.Log($"[InGameMenuController] 已捕获截图: {Screen.width}x{Screen.height}");

        // 然后才显示菜单
        isMenuOpen = true;

        if (menuPanel != null)
            menuPanel.SetActive(true);

        // 暂停游戏
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
            Debug.Log("[InGameMenuController] 游戏已暂停 (Time.timeScale = 0)");
        }
    }

    /// <summary>
    /// 关闭菜单
    /// </summary>
    public void CloseMenu()
    {
        if (!isMenuOpen)
            return;

        Debug.Log("[InGameMenuController] 关闭系统菜单");

        isMenuOpen = false;

        // 隐藏菜单面板
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // 恢复游戏
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
            Debug.Log("[InGameMenuController] 游戏已恢复 (Time.timeScale = 1)");
        }
    }

    /// <summary>
    /// Resume按钮点击事件
    /// </summary>
    private void OnResumeClicked()
    {
        Debug.Log("[InGameMenuController] 点击Resume按钮");
        CloseMenu();
    }

    /// <summary>
    /// Save按钮点击事件
    /// </summary>
    private void OnSaveClicked()
    {
        Debug.Log("[InGameMenuController] 点击Save按钮");

        if (saveLoadPanelController == null)
        {
            Debug.LogError("[InGameMenuController] SaveLoadPanelController 引用为空！");
            return;
        }

        // 【重要】将预捕获的截图传递给SaveManager
        if (precapturedScreenshot != null)
        {
            SaveManager.Instance.SetPrecapturedScreenshot(precapturedScreenshot);
            Debug.Log("[InGameMenuController] 已设置预捕获截图");
        }

        // 隐藏系统菜单
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // 打开存档面板（存档模式）
        saveLoadPanelController.Open(PanelMode.Save, currentChapterName, OnSaveLoadPanelClosed);
    }

    /// <summary>
    /// Load按钮点击事件
    /// </summary>
    private void OnLoadClicked()
    {
        Debug.Log("[InGameMenuController] 点击Load按钮");

        if (saveLoadPanelController == null)
        {
            Debug.LogError("[InGameMenuController] SaveLoadPanelController 引用为空！");
            return;
        }

        // 隐藏系统菜单
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // 打开存档面板（读档模式）
        saveLoadPanelController.Open(PanelMode.Load, null, OnSaveLoadPanelClosed);
    }

    /// <summary>
    /// Back to Title按钮点击事件
    /// </summary>
    private void OnBackToTitleClicked()
    {
        Debug.Log("[InGameMenuController] 点击Back to Title按钮");

        if (exitConfirmPanel == null)
        {
            Debug.LogWarning("[InGameMenuController] ExitConfirmPanel 引用为空，使用简单确认");
            
            // 降级方案：使用ConfirmDialog
            if (confirmDialog != null)
            {
                confirmDialog.transform.SetAsLastSibling();
                confirmDialog.Show(
                    "返回主菜单",
                    "确定要返回主菜单吗？\n未保存的进度将会丢失。",
                    OnDirectExit,
                    null
                );
            }
            else
            {
                OnDirectExit();
            }
            return;
        }

        // 隐藏系统菜单
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // 显示退出确认面板
        exitConfirmPanel.Show(
            OnSaveAndExit,
            OnDirectExit,
            OnCancelExit
        );

        Debug.Log("[InGameMenuController] 退出确认面板已显示");
    }

    /// <summary>
    /// 保存并退出回调
    /// </summary>
    private async void OnSaveAndExit()
    {
        Debug.Log("[InGameMenuController] 执行：保存并退出");

        // 保存到自动存档槽位
        bool success = await SaveManager.Instance.QuickSaveAsync(currentChapterName);

        if (success)
        {
            Debug.Log("[InGameMenuController] 快速保存成功，返回主菜单");
        }
        else
        {
            Debug.LogWarning("[InGameMenuController] 快速保存失败，仍然返回主菜单");
        }

        // 无论保存成功与否，都返回主菜单
        ReturnToMainMenu();
    }

    /// <summary>
    /// 直接退出回调
    /// </summary>
    private void OnDirectExit()
    {
        Debug.Log("[InGameMenuController] 执行：直接退出");
        ReturnToMainMenu();
    }

    /// <summary>
    /// 取消退出回调
    /// </summary>
    private void OnCancelExit()
    {
        Debug.Log("[InGameMenuController] 用户取消退出");

        // 重新显示系统菜单
        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    private void ReturnToMainMenu()
    {
        // 恢复时间缩放
        Time.timeScale = 1f;

        // 调用GameFlowController的返回主菜单方法
        GameFlowController gameFlowController = FindObjectOfType<GameFlowController>();
        if (gameFlowController != null)
        {
            gameFlowController.ReturnToMainMenu();
        }
        else
        {
            // 如果没有GameFlowController，直接加载主菜单场景
            Debug.LogWarning("[InGameMenuController] 未找到GameFlowController，直接加载主菜单");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }
    }

    /// <summary>
    /// 存档/读档面板关闭回调
    /// </summary>
    private void OnSaveLoadPanelClosed()
    {
        // 重新显示系统菜单
        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

    /// <summary>
    /// 设置当前章节名称（用于保存时的元数据）
    /// </summary>
    /// <param name="chapterName">章节名称</param>
    public void SetChapterName(string chapterName)
    {
        currentChapterName = chapterName;
        Debug.Log($"[InGameMenuController] 设置章节名称: {chapterName}");
    }


    /// <summary>
    /// 检查菜单是否打开
    /// </summary>
    /// <returns>菜单是否打开</returns>
    public bool IsMenuOpen()
    {
        return isMenuOpen;
    }

    private void OnDestroy()
    {
        // 清理截图资源
        if (precapturedScreenshot != null)
        {
            Destroy(precapturedScreenshot);
            precapturedScreenshot = null;
        }
    }
}
