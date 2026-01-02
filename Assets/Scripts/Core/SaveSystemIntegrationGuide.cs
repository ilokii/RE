using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SaveSystem;

/// <summary>
/// 存档系统接入指南
/// 本文件包含如何在现有系统中集成 SaveManager 的示例代码
/// </summary>
public class SaveSystemIntegrationGuide : MonoBehaviour
{
    // ========================================
    // 第一部分：程序启动时的初始化
    // ========================================

    /// <summary>
    /// 示例：游戏启动时的初始化流程
    /// 建议在游戏的主入口场景（例如 MainMenu 或 GameBootstrap）中调用
    /// </summary>
    void GameStartupExample()
    {
        // 1. SaveManager 会在首次访问 Instance 时自动创建并加载 GlobalData
        // 你不需要手动调用任何初始化方法，只需确保在使用前访问一次 Instance
        SaveManager.Instance.SetAutoSaveEnabled(true);

        // 2. 获取全局数据（例如检查玩家是否已解锁某个结局）
        GlobalData globalData = SaveManager.Instance.GetGlobalData();
        
        if (globalData.UnlockedEndings.Contains("TrueEnding"))
        {
            Debug.Log("玩家已解锁真结局！");
            // 可以在主菜单显示特殊内容
        }

        // 3. 检查是否有已读对话（用于实现"跳过已读"功能）
        if (globalData.ReadDialogueIds.Contains(1001))
        {
            Debug.Log("对话 1001 已读过");
        }

        // 4. 检查全局标志（例如隐藏要素解锁）
        if (globalData.GlobalFlags.ContainsKey("SecretRoomUnlocked") && 
            globalData.GlobalFlags["SecretRoomUnlocked"])
        {
            Debug.Log("隐藏房间已解锁");
        }
    }

    // ========================================
    // 第二部分：Manager 注册到存档系统
    // ========================================

    /// <summary>
    /// 示例：DialogueManager 如何注册到存档系统
    /// 在 DialogueManager.cs 的 Start() 或 Awake() 中添加：
    /// </summary>
    void DialogueManagerRegistrationExample()
    {
        // 在 DialogueManager.cs 中添加：
        /*
        void Start()
        {
            // 注册到存档系统
            SaveManager.Instance.RegisterSavable(this);
            
            // ... 其他初始化代码
            LoadScript(startScript);
        }

        void OnDestroy()
        {
            // 销毁时注销
            SaveManager.Instance.UnregisterSavable(this);
        }
        */
    }

    // ========================================
    // 第三部分：场景切换时的自动存档
    // ========================================

    /// <summary>
    /// 示例：在场景切换时触发自动存档
    /// 可以在 SceneManager 或场景转换脚本中调用
    /// </summary>
    void OnSceneTransitionExample()
    {
        // 1. 设置游戏状态为转换中（防止定时自动存档干扰）
        SaveManager.Instance.SetGameState(GameState.InTransition);

        // 2. 在转换开始前触发自动存档（强制立即执行）
        SaveManager.Instance.TriggerAutoSave(AutoSaveTrigger.ChapterEnd, forceImmediate: true);

        // 3. 加载新场景
        // UnityEngine.SceneManagement.SceneManager.LoadScene("NextScene");
    }

    // ========================================
    // 第四部分：解密系统接入
    // ========================================

    /// <summary>
    /// 示例：解密完成时触发自动存档
    /// 在 PuzzleManager 或解密脚本中调用
    /// </summary>
    void OnPuzzleCompletedExample()
    {
        // 1. 解密完成后，恢复空闲状态
        SaveManager.Instance.SetGameState(GameState.Idle);

        // 2. 触发自动存档
        SaveManager.Instance.TriggerAutoSave(AutoSaveTrigger.PuzzleComplete);

        // 3. 更新全局数据（例如记录解密完成）
        GlobalData globalData = SaveManager.Instance.GetGlobalData();
        globalData.GlobalFlags["Puzzle_Room1_Completed"] = true;
        SaveManager.Instance.SaveGlobalData();
    }

    // ========================================
    // 第五部分：对话系统状态管理
    // ========================================

    /// <summary>
    /// 示例：在 DialogueManager 中管理游戏状态
    /// </summary>
    void DialogueStateManagementExample()
    {
        // 在 DialogueManager.cs 的 ProcessCurrentLine() 中添加：
        /*
        void ProcessCurrentLine()
        {
            if (currentIndex >= currentLines.Count) return;

            DialogueLine line = currentLines[currentIndex];

            // 设置游戏状态为对话中
            SaveManager.Instance.SetGameState(GameState.InDialogue);

            switch (line.type)
            {
                case DialogueType.DIALOG:
                    UpdateDialogueUI(line);
                    
                    // 记录已读对话（用于跳过功能）
                    GlobalData globalData = SaveManager.Instance.GetGlobalData();
                    globalData.ReadDialogueIds.Add(line.id);
                    break;

                case DialogueType.CMD_CHOOSE:
                    // 关键选择时触发自动存档
                    SaveManager.Instance.TriggerAutoSave(AutoSaveTrigger.DecisionMade);
                    StartCoroutine(GenerateChoices(line.content));
                    break;

                // ... 其他逻辑
            }
        }

        // 在对话结束或玩家退出对话时：
        void OnDialogueEnd()
        {
            SaveManager.Instance.SetGameState(GameState.Idle);
        }
        */
    }

    // ========================================
    // 第六部分：UI 集成 - 存档槽位显示
    // ========================================

    [Header("UI 组件引用（示例）")]
    public Transform saveSlotContainer; // 存档槽位的父容器
    public GameObject saveSlotPrefab;   // 存档槽位的 Prefab

    /// <summary>
    /// 示例：在"读取存档"面板中显示所有存档槽位
    /// </summary>
    void DisplaySaveSlots()
    {
        // 1. 清空现有槽位
        foreach (Transform child in saveSlotContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. 获取所有手动槽位的元数据
        List<SaveSlotMeta> slots = SaveManager.Instance.GetAllManualSlots();

        // 3. 遍历并显示每个槽位
        for (int i = 0; i < slots.Count; i++)
        {
            SaveSlotMeta meta = slots[i];

            // 实例化槽位 UI
            GameObject slotObj = Instantiate(saveSlotPrefab, saveSlotContainer);
            
            // 获取 UI 组件（假设 Prefab 中有这些组件）
            TextMeshProUGUI slotNumberText = slotObj.transform.Find("SlotNumber").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI chapterNameText = slotObj.transform.Find("ChapterName").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI saveTimeText = slotObj.transform.Find("SaveTime").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI playTimeText = slotObj.transform.Find("PlayTime").GetComponent<TextMeshProUGUI>();
            Image screenshotImage = slotObj.transform.Find("Screenshot").GetComponent<Image>();
            Button loadButton = slotObj.GetComponent<Button>();

            // 设置槽位编号
            slotNumberText.text = $"存档 {i + 1}";

            if (meta.IsEmpty)
            {
                // 空槽位显示
                chapterNameText.text = "空槽位";
                saveTimeText.text = "";
                playTimeText.text = "";
                screenshotImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // 半透明灰色
                loadButton.interactable = false; // 禁用加载按钮
            }
            else
            {
                // 有存档的槽位
                chapterNameText.text = meta.ChapterName;
                saveTimeText.text = meta.SaveTime;
                playTimeText.text = $"游玩时长: {meta.GetFormattedPlayTime()}";

                // 加载截图（如果存在）
                string screenshotPath = System.IO.Path.Combine(
                    Application.persistentDataPath, 
                    "Screenshots", 
                    meta.ScreenshotPath
                );

                if (System.IO.File.Exists(screenshotPath))
                {
                    byte[] imageData = System.IO.File.ReadAllBytes(screenshotPath);
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(imageData);
                    screenshotImage.sprite = Sprite.Create(
                        texture, 
                        new Rect(0, 0, texture.width, texture.height), 
                        new Vector2(0.5f, 0.5f)
                    );
                }

                // 绑定加载按钮事件
                int slotId = i; // 捕获当前索引
                loadButton.onClick.AddListener(() => OnLoadButtonClicked(slotId));
            }
        }

        // 4. 同样处理自动存档槽位（可选）
        SaveSlotMeta autoSaveMeta = SaveManager.Instance.GetSlotMetadata(-1);
        if (!autoSaveMeta.IsEmpty)
        {
            // 显示自动存档槽位...
            Debug.Log($"自动存档: {autoSaveMeta.ChapterName} - {autoSaveMeta.SaveTime}");
        }
    }

    /// <summary>
    /// 加载按钮点击事件
    /// </summary>
    void OnLoadButtonClicked(int slotId)
    {
        Debug.Log($"加载存档槽位: {slotId}");
        
        bool success = SaveManager.Instance.LoadGame(slotId);
        
        if (success)
        {
            Debug.Log("存档加载成功！");
            // 切换到游戏场景
            // UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
        }
        else
        {
            Debug.LogError("存档加载失败！");
            // 显示错误提示
        }
    }

    // ========================================
    // 第七部分：手动保存功能
    // ========================================

    /// <summary>
    /// 示例：玩家点击"保存"按钮时
    /// </summary>
    public async void OnSaveButtonClicked(int slotId)
    {
        // 1. 检查游戏状态是否允许保存
        if (SaveManager.Instance.GetGameState() != GameState.Idle)
        {
            Debug.LogWarning("当前状态不允许保存！");
            // 显示提示："请在安全的时候保存游戏"
            return;
        }

        // 2. 获取当前章节名（可以从 DialogueManager 或其他地方获取）
        string currentChapter = "第一章"; // 示例

        // 3. 执行异步保存
        bool success = await SaveManager.Instance.SaveGameAsync(slotId, currentChapter);

        // 4. 显示结果
        if (success)
        {
            Debug.Log($"游戏已保存到槽位 {slotId}");
            // 显示成功提示
        }
        else
        {
            Debug.LogError("保存失败！");
            // 显示错误提示
        }
    }

    // ========================================
    // 第八部分：结局达成时的全局数据更新
    // ========================================

    /// <summary>
    /// 示例：玩家达成某个结局时
    /// </summary>
    void OnEndingReached(string endingId)
    {
        // 1. 获取全局数据
        GlobalData globalData = SaveManager.Instance.GetGlobalData();

        // 2. 添加结局到已解锁列表
        if (!globalData.UnlockedEndings.Contains(endingId))
        {
            globalData.UnlockedEndings.Add(endingId);
            Debug.Log($"解锁新结局: {endingId}");
        }

        // 3. 保存全局数据
        SaveManager.Instance.SaveGlobalData();

        // 4. 可选：触发自动存档
        SaveManager.Instance.TriggerAutoSave(AutoSaveTrigger.ChapterEnd, forceImmediate: true);
    }

    // ========================================
    // 第九部分：快速保存/加载（F5/F9）
    // ========================================

    void Update()
    {
        // 快速保存（F5）
        if (Input.GetKeyDown(KeyCode.F5))
        {
            QuickSave();
        }

        // 快速加载（F9）
        if (Input.GetKeyDown(KeyCode.F9))
        {
            QuickLoad();
        }
    }

    async void QuickSave()
    {
        Debug.Log("执行快速保存...");
        bool success = await SaveManager.Instance.QuickSaveAsync("快速保存");
        
        if (success)
        {
            // 显示提示："游戏已快速保存"
            Debug.Log("快速保存成功！");
        }
    }

    void QuickLoad()
    {
        Debug.Log("执行快速加载...");
        bool success = SaveManager.Instance.QuickLoad();
        
        if (success)
        {
            Debug.Log("快速加载成功！");
        }
        else
        {
            Debug.LogWarning("没有快速存档！");
        }
    }

    // ========================================
    // 第十部分：订阅自动存档完成事件
    // ========================================

    void Start()
    {
        // 订阅自动存档完成事件
        SaveManager.Instance.OnAutoSaveCompleted += OnAutoSaveCompleted;
    }

    void OnDestroy()
    {
        // 取消订阅
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnAutoSaveCompleted -= OnAutoSaveCompleted;
        }
    }

    void OnAutoSaveCompleted(AutoSaveTrigger trigger, bool success)
    {
        if (success)
        {
            // 显示 UI 提示："游戏已自动保存"
            Debug.Log($"自动存档成功: {trigger}");
            // 例如：显示一个淡入淡出的文字提示
            // ShowNotification("游戏已自动保存");
        }
        else
        {
            Debug.LogWarning($"自动存档失败: {trigger}");
        }
    }
}
