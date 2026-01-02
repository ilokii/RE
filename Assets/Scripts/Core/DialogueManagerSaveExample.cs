using UnityEngine;
using System.Collections.Generic;
using SaveSystem;

/// <summary>
/// DialogueManager 的存档扩展示例
/// 演示如何让 DialogueManager 实现 ISavable 接口来保存当前的行号和脚本名
/// </summary>
public partial class DialogueManager : ISavable
{
    /// <summary>
    /// 对话管理器的状态数据（可序列化）
    /// </summary>
    [System.Serializable]
    public class DialogueManagerState
    {
        /// <summary>
        /// 当前脚本文件名
        /// </summary>
        public string CurrentScriptFile;

        /// <summary>
        /// 当前对话行索引
        /// </summary>
        public int CurrentLineIndex;

        /// <summary>
        /// 当前背景图片名称
        /// </summary>
        public string CurrentBackground;

        /// <summary>
        /// 当前BGM名称
        /// </summary>
        public string CurrentBGM;

        /// <summary>
        /// 当前BGM音量
        /// </summary>
        public float BGMVolume;

        /// <summary>
        /// 是否正在等待选项
        /// </summary>
        public bool IsWaitingForChoice;
    }

    /// <summary>
    /// 捕获当前对话管理器的状态
    /// </summary>
    public object CaptureState()
    {
        DialogueManagerState state = new DialogueManagerState
        {
            CurrentScriptFile = currentScriptName, // 【修改】使用 currentScriptName 而不是 startScript
            CurrentLineIndex = currentIndex,
            CurrentBackground = backgroundImage.sprite != null ? backgroundImage.sprite.name : "",
            CurrentBGM = bgmSource.clip != null ? bgmSource.clip.name : "",
            BGMVolume = bgmSource.volume,
            IsWaitingForChoice = isWaitingForChoice
        };

        Debug.Log($"[DialogueManager] 捕获状态: 脚本={state.CurrentScriptFile}, 行号={state.CurrentLineIndex}");
        return state;
    }

    /// <summary>
    /// 根据保存的状态恢复对话管理器
    /// </summary>
    public void RestoreState(object state)
    {
        if (state is DialogueManagerState dialogueState)
        {
            Debug.Log($"[DialogueManager] 恢复状态: 脚本={dialogueState.CurrentScriptFile}, 行号={dialogueState.CurrentLineIndex}");

            // 1. 加载脚本
            LoadScript(dialogueState.CurrentScriptFile);

            // 2. 跳转到保存的行号
            currentIndex = dialogueState.CurrentLineIndex;

            // 3. 恢复背景
            if (!string.IsNullOrEmpty(dialogueState.CurrentBackground))
            {
                ChangeBackground(dialogueState.CurrentBackground);
            }

            // 4. 恢复BGM
            if (!string.IsNullOrEmpty(dialogueState.CurrentBGM))
            {
                PlayBGM(dialogueState.CurrentBGM);
                bgmSource.volume = dialogueState.BGMVolume;
            }

            // 5. 恢复选项状态
            isWaitingForChoice = dialogueState.IsWaitingForChoice;

            // 【修复】重置打字状态，确保可以继续对话
            isTyping = false;
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            if (waitingCursor != null)
                waitingCursor.SetActive(true);

            // 6. 处理当前行（显示对话）
            ProcessCurrentLine();
            
            Debug.Log("[DialogueManager] 状态恢复完成，对话系统已就绪");
        }
        else
        {
            Debug.LogError("[DialogueManager] 恢复状态失败：状态数据类型不匹配");
        }
    }
}
