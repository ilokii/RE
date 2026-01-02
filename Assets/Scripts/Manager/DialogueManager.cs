using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public partial class DialogueManager : MonoBehaviour
{
    [Header("UI 组件")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contentText;
    public Image backgroundImage;
    public GameObject waitingCursor; // 【新增】等待光标 (例如一个闪烁的小箭头)

    [Header("音效组件")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;    // 【新增】音效播放器 (用于打字声)
    public AudioClip typingClip;     // 【新增】打字音效文件 (beep)

    [Header("选项组件")]
    public Transform choicePanel;
    public GameObject choiceBtnPrefab;

    [Header("设置")]
    public string startScript = "TestScript";

    // --- 内部变量 ---
    private List<DialogueLine> currentLines = new List<DialogueLine>();
    private Dictionary<int, int> idToIndexMap = new Dictionary<int, int>();
    private int currentIndex = 0;
    private string currentScriptName = ""; // 【新增】当前脚本名称（用于存档）
    
    // 状态控制
    private bool isWaitingForChoice = false;
    private bool isTyping = false;           // 【新增】是否正在打字中
    private bool isControlledByGameFlow = false; // 【新增】是否由 GameFlowController 控制

    // 协程引用 (用于强行停止打字)
    private Coroutine typingCoroutine;

    // 【新增】缓存InGameMenuController引用，避免每帧查找
    private InGameMenuController cachedMenuController;

    void Start()
    {
        // 初始隐藏光标
        if (waitingCursor) waitingCursor.SetActive(false);
        
        // 注册到 SaveManager
        SaveManager.Instance.RegisterSavable(this);
        
        // 【修改】不再自动加载脚本，改由 GameFlowController 控制
        // 如果没有被 GameFlowController 控制（例如直接测试这个场景），则使用默认脚本
        // 延迟一帧检查，给 GameFlowController 时间设置标志
        StartCoroutine(DelayedAutoStart());
    }
    
    /// <summary>
    /// 延迟自动启动（如果没有被 GameFlowController 控制）
    /// </summary>
    private System.Collections.IEnumerator DelayedAutoStart()
    {
        yield return null; // 等待一帧
        
        if (!isControlledByGameFlow)
        {
            Debug.LogWarning("[DialogueManager] 未被 GameFlowController 控制，使用默认脚本启动");
            LoadScript(startScript);
        }
    }
    
    /// <summary>
    /// 设置为由 GameFlowController 控制
    /// 此方法应由 GameFlowController 在加载脚本前调用
    /// </summary>
    public void SetControlledByGameFlow(bool controlled)
    {
        isControlledByGameFlow = controlled;
    }

    void Update()
    {
        // 【优化】缓存InGameMenuController引用，避免每帧查找
        if (cachedMenuController == null)
        {
            cachedMenuController = FindObjectOfType<InGameMenuController>();
        }

        // 【修复】检查游戏内菜单是否打开
        if (cachedMenuController != null && cachedMenuController.IsMenuOpen())
        {
            return; // 菜单打开时不处理对话输入
        }

        // 只有在非选项状态下才响应点击
        if (!isWaitingForChoice && Input.GetMouseButtonDown(0))
        {
            // 【修复】检查是否点击在可交互UI上（如按钮）
            // 使用更精确的检测：只有点击到Button等可交互组件时才忽略
            if (IsClickingOnInteractableUI())
            {
                Debug.Log("[DialogueManager] 点击在可交互UI上，忽略");
                return;
            }

            Debug.Log($"[DialogueManager] 检测到点击: isTyping={isTyping}, currentIndex={currentIndex}/{currentLines.Count}");

            if (isTyping)
            {
                // 【逻辑 A】如果正在打字 -> 立即显示全句 (Skip)
                FinishTypingImmediately();
            }
            else
            {
                // 【逻辑 B】如果打字完了 -> 下一句
                if (currentLines.Count > currentIndex && currentLines[currentIndex].type == DialogueType.DIALOG)
                {
                    currentIndex++;
                    ProcessCurrentLine();
                }
            }
        }
    }

    /// <summary>
    /// 检查是否点击在可交互UI上（如按钮）
    /// </summary>
    private bool IsClickingOnInteractableUI()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
            return false;

        // 获取当前鼠标下的所有UI对象
        var pointerEventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        pointerEventData.position = Input.mousePosition;

        var results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerEventData, results);

        // 检查是否点击到Button或其他可交互组件
        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<Button>() != null ||
                result.gameObject.GetComponent<UnityEngine.UI.Scrollbar>() != null ||
                result.gameObject.GetComponent<UnityEngine.UI.Slider>() != null ||
                result.gameObject.GetComponent<UnityEngine.UI.Toggle>() != null)
            {
                return true; // 点击在可交互UI上
            }
        }

        return false; // 点击在普通UI区域（如对话框背景），允许继续对话
    }

    // --- 核心逻辑 1: 加载剧本 ---
    public void LoadScript(string fileName)
    {
        currentScriptName = fileName; // 【新增】记录当前脚本名
        currentLines = CSVLoader.Load(fileName);
        idToIndexMap.Clear();
        currentIndex = 0;
        isWaitingForChoice = false;

        if (currentLines.Count == 0) return;

        // 构建索引字典 (ID -> List Index)，方便 O(1) 跳转
        for (int i = 0; i < currentLines.Count; i++)
        {
            int id = currentLines[i].id;
            if (!idToIndexMap.ContainsKey(id))
            {
                idToIndexMap.Add(id, i);
            }
        }

        // 立即处理第一行
        ProcessCurrentLine();
    }

    // --- 核心逻辑 2: 处理当前行 (状态机) ---
    void ProcessCurrentLine()
    {
        if (currentIndex >= currentLines.Count) return; // 结束保护

        DialogueLine line = currentLines[currentIndex];

        switch (line.type)
        {
            case DialogueType.DIALOG:
                // 【修改】立绘逻辑移交给 PortraitManager
                // 解析位置 (CSV 第 5 列)
                int posIndex = -1; // 默认 -1 表示保持不变
                if (!string.IsNullOrEmpty(line.position))
                {
                    if (line.position == "HIDE")
                    {
                        PortraitManager.Instance.HideCharacter(line.charId);
                    }
                    else
                    {
                        int.TryParse(line.position, out posIndex);
                        // 调用管理器：如果 posIndex 是 -1，你可能需要修改 PortraitManager 让它支持"保持原位"
                        // 这里我们假设 CSV 如果填了数字就是移动，没填就是 -1
                        // 但 PortraitManager 需要知道如果不动，就只换表情
                        
                        // 修正逻辑：如果 CSV 没填位置，我们怎么知道他在哪？
                        // 简单方案：在 Manager 里记录每个人的位置，或者这里只传"有效的位置ID"
                        PortraitManager.Instance.UpdatePortrait(line.charId, line.expression, posIndex);
                    }
                }
                else
                {
                    // 如果位置是空，传一个约定值（比如 -1），让 Manager 保持它当前的位置
                    PortraitManager.Instance.UpdatePortrait(line.charId, line.expression, -1);
                }
                
                // 【新增】设置当前说话角色，自动高亮
                if (!string.IsNullOrEmpty(line.charId))
                {
                    PortraitManager.Instance.SetActiveSpeaker(line.charId);
                }

                UpdateDialogueUI(line); // 只负责文字显示
                break;

            case DialogueType.CMD_JUMP:
                JumpToId(int.Parse(line.content));
                break;

            case DialogueType.CMD_LOAD:
                LoadScript(line.content);
                break;

            case DialogueType.CMD_CHOOSE:
                StartCoroutine(GenerateChoices(line.content));
                break;

            case DialogueType.CMD_BG:
                ChangeBackground(line.content);
                currentIndex++; ProcessCurrentLine(); // 自动下一步
                break;

            case DialogueType.CMD_BGM:
                PlayBGM(line.content);
                currentIndex++; ProcessCurrentLine(); // 自动下一步
                break;

            case DialogueType.CMD_FOCUS:
                // 【改进】通过 CharID 精确控制聚焦
                if (string.IsNullOrEmpty(line.charId))
                {
                    Debug.LogError($"[DialogueManager] CMD_FOCUS 缺少角色ID (CharID列为空) at ID {line.id}");
                    currentIndex++;
                    ProcessCurrentLine();
                    break;
                }
                
                // Content 列决定是聚焦还是取消聚焦
                string focusCommand = line.content?.Trim().ToUpper() ?? "";
                bool isFocus = (focusCommand == "TRUE" || focusCommand == "1" || focusCommand == "ON");
                
                Debug.Log($"[DialogueManager] 执行 CMD_FOCUS: 角色={line.charId}, 聚焦={isFocus}");
                PortraitManager.Instance.SetFocusByCharacter(line.charId, isFocus);
                
                // 【自动跳过】不等待点击，直接下一句
                currentIndex++;
                ProcessCurrentLine();
                break;

            case DialogueType.CMD_HIDE:
                // 读取 Content 列，看看要隐藏谁
                string targetToHide = line.charId.Trim();

                if (string.IsNullOrEmpty(targetToHide))
                {
                    Debug.LogWarning("CMD_HIDE 指令缺少参数(Content)，请填入角色ID或 'ALL'");
                }
                else if (targetToHide.ToUpper() == "ALL")
                {
                    // 【功能扩展】如果是 ALL，就隐藏所有人
                    PortraitManager.Instance.HideAllCharacters(); 
                }
                else
                {
                    // 隐藏指定的一个人
                    PortraitManager.Instance.HideCharacter(targetToHide);
                }

                // 自动跳到下一行 (不等待点击)
                currentIndex++;
                ProcessCurrentLine();
                break;
        }
    }

    // --- 功能 A: 更新显示 ---
    void UpdateDialogueUI(DialogueLine line)
    {
        // 1. 获取角色名字 (仍然需要 CharacterManager 来把 ID 翻译成名字)
        // 注意：这里不再处理立绘图片了，只处理名字
        if (CharacterManager.Instance != null && !string.IsNullOrEmpty(line.charId))
        {
            CharacterProfile profile = CharacterManager.Instance.GetCharacter(line.charId);
            
            if (profile != null)
            {
                nameText.text = profile.displayName;
                // 【关键修改】旧的 portraitImage.sprite = ... 代码全部删除！
                // 立绘现在由 ProcessCurrentLine 里的 PortraitManager.UpdatePortrait 处理
            }
            else
            {
                // 有 ID 但没找到档案，直接显示 ID
                nameText.text = line.charId;
            }
        }
        else
        {
            // 旁白模式
            nameText.text = "";
        }

        // 2. 启动打字机 (文字显示逻辑不变)
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        // 如果 CSV 里没有 Speed 这一列，line.speed 可能是 null，给个默认值
        string speed = string.IsNullOrEmpty(line.speed) ? "Normal" : line.speed;
        
        typingCoroutine = StartCoroutine(TypeWriterEffect(line.content, speed));
    }

    // --- 【修改后】支持富文本的打字机协程 ---
    IEnumerator TypeWriterEffect(string content, string speedConfig)
    {
        isTyping = true;
        if (waitingCursor) waitingCursor.SetActive(false);

        // 1. 把完整的、包含标签的文本直接给 TMP
        contentText.text = content;
        
        // 2. 强制刷新！这一步至关重要
        // 因为刚赋值文本，TMP 还没来得及计算里面有多少个有效字符，必须强制它算一次
        contentText.ForceMeshUpdate();

        // 3. 初始隐藏所有字
        contentText.maxVisibleCharacters = 0;

        // 解析速度 (保持之前的逻辑)
        float delay = 0.05f;
        switch (speedConfig.ToLower())
        {
            case "fast": delay = 0.02f; break;
            case "slow": delay = 0.15f; break;
            default: float.TryParse(speedConfig, out delay); if(delay==0) delay=0.05f; break;
        }

        // 4. 获取实际要显示的字符总数 (不包含 <color> 这种标签字符)
        int totalVisibleChars = contentText.textInfo.characterCount;

        // 5. 按照“可见字符”进行循环
        for (int i = 0; i <= totalVisibleChars; i++)
        {
            contentText.maxVisibleCharacters = i; 

            if (i > 0 && i <= totalVisibleChars)
            {
                TMP_CharacterInfo charInfo = contentText.textInfo.characterInfo[i - 1];
                
                // 【核心修改】不再信任 charInfo.isVisible
                // 而是直接检查：这个字符存在(不为0) 且 不是空白字符(空格/换行)
                // 这样只要是汉字、字母、标点，都会播放音效
                bool isValidChar = charInfo.character != 0 && !char.IsWhiteSpace(charInfo.character);

                if (isValidChar) 
                {
                    PlayTypingSound();

                    // 标点停顿逻辑 (同样直接用字符判断，不依赖 isVisible)
                    char c = charInfo.character;
                    if (c == '，' || c == ',') yield return new WaitForSeconds(delay * 3);
                    if (c == '。' || c == '.' || c == '！' || c == '？') yield return new WaitForSeconds(delay * 6);
                }
            }

            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        if (waitingCursor) waitingCursor.SetActive(true);
    }

    // --- 【新增】立即完成打字 ---
    void FinishTypingImmediately()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        contentText.maxVisibleCharacters = 99999; // 显示所有字
        isTyping = false;
        
        if (waitingCursor) waitingCursor.SetActive(true); // 显示光标
    }

    // --- 【新增】播放打字音效 (带音调随机) ---
    void PlayTypingSound()
    {
        if (sfxSource != null && typingClip != null)
        {
            sfxSource.pitch = Random.Range(0.9f, 1.1f); // 随机音调，让声音更自然
            sfxSource.PlayOneShot(typingClip);
        }
    }

    // --- 功能 B: 跳转逻辑 ---
    void JumpToId(int id)
    {
        if (idToIndexMap.ContainsKey(id))
        {
            currentIndex = idToIndexMap[id];
            ProcessCurrentLine(); // 递归调用：立即处理跳转后的那一行，不要等待点击
        }
        else
        {
            Debug.LogError($"跳转失败：找不到 ID {id}");
        }
    }

    // --- 功能 C: 选项生成 ---
    System.Collections.IEnumerator GenerateChoices(string rawContent)
    {
        isWaitingForChoice = true;
        // 隐藏对话框 (可选，看你设计)
        // contentText.transform.parent.gameObject.SetActive(false);

        // 格式: "选项A:100|选项B:200"
        string[] options = rawContent.Split('|');

        // 清理旧按钮
        foreach (Transform child in choicePanel) Destroy(child.gameObject);

        // 稍微等待一下 (防止上一帧的点击误触)
        yield return null; 

        foreach (string option in options)
        {
            string[] parts = option.Split(':');
            if (parts.Length < 2) continue;

            string btnText = parts[0];
            int targetId = int.Parse(parts[1]);

            // 生成按钮
            GameObject btn = Instantiate(choiceBtnPrefab, choicePanel);
            // 设置文字 (假设按钮下有一个 TextMeshProUGUI)
            btn.GetComponentInChildren<TextMeshProUGUI>().text = btnText;
            
            // 绑定事件
            btn.GetComponent<Button>().onClick.AddListener(() => 
            {
                OnChoiceSelected(targetId);
            });
        }
    }

    // --- 功能 D: 切换背景 ---
    public void ChangeBackground(string bgName) // 【修改】改为 public，允许外部调用
    {
        // 从 Resources/Backgrounds/ 文件夹加载图片
        Sprite bgSprite = Resources.Load<Sprite>("Backgrounds/" + bgName);
        
        if (bgSprite != null)
        {
            backgroundImage.sprite = bgSprite;
            backgroundImage.color = Color.white; // 确保图片不是透明的
        }
        else
        {
            Debug.LogWarning($"找不到背景图: Resources/Backgrounds/{bgName}");
        }
    }

    // --- 功能 E: 切换音乐 ---
    public void PlayBGM(string musicName) // 【修改】改为 public，允许外部调用
    {
        // 如果填 "STOP"，则停止音乐
        if (musicName == "STOP")
        {
            bgmSource.Stop();
            return;
        }

        // 从 Resources/Audio/ 文件夹加载音乐
        AudioClip clip = Resources.Load<AudioClip>("Audio/" + musicName);
        
        if (clip != null)
        {
            // 如果是同一首哥，就不重播
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            bgmSource.clip = clip;
            bgmSource.loop = true; // BGM 默认循环
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"找不到音乐: Resources/Audio/{musicName}");
        }
    }

    void OnChoiceSelected(int targetId)
    {
        // 销毁所有按钮
        foreach (Transform child in choicePanel) Destroy(child.gameObject);
        
        // 恢复状态
        isWaitingForChoice = false;
        // contentText.transform.parent.gameObject.SetActive(true); // 恢复对话框
        
        // 执行跳转
        JumpToId(targetId);
    }
}