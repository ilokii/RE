using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveSystem
{
    /// <summary>
    /// 全局数据：存储与特定进度无关的元数据（结局、CG、已读文本）
    /// </summary>
    [System.Serializable]
    public class GlobalData
    {
        /// <summary>
        /// 已达成的结局列表
        /// </summary>
        public List<string> UnlockedEndings = new List<string>();

        /// <summary>
        /// 已读对话ID集合，用于实现跳过已读功能
        /// </summary>
        public HashSet<int> ReadDialogueIds = new HashSet<int>();

        /// <summary>
        /// 跨周目全局变量（例如：隐藏要素解锁状态）
        /// </summary>
        public Dictionary<string, bool> GlobalFlags = new Dictionary<string, bool>();

        /// <summary>
        /// 已解锁的CG图片ID列表（可选扩展）
        /// </summary>
        public List<string> UnlockedCGs = new List<string>();
    }

    /// <summary>
    /// 角色状态：记录屏幕上角色的显示信息
    /// </summary>
    [System.Serializable]
    public class CharacterState
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public string CharacterId;

        /// <summary>
        /// 角色在屏幕上的位置（Left/Center/Right 或具体坐标）
        /// </summary>
        public string Position;

        /// <summary>
        /// 当前表情/立绘ID
        /// </summary>
        public string Expression;

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible = true;

        /// <summary>
        /// 角色层级（用于控制显示顺序）
        /// </summary>
        public int SortingOrder = 0;
    }

    /// <summary>
    /// 存档数据：存储某个时间点的游戏快照
    /// </summary>
    [System.Serializable]
    public class ArchiveData
    {
        /// <summary>
        /// 当前对话脚本文件名
        /// </summary>
        public string CurrentScriptFile;

        /// <summary>
        /// 当前对话行号
        /// </summary>
        public int CurrentLineIndex;

        /// <summary>
        /// 当前背景图片ID
        /// </summary>
        public string BackgroundImageId;

        /// <summary>
        /// 当前播放的背景音乐名称
        /// </summary>
        public string BGMName;

        /// <summary>
        /// 当前BGM音量
        /// </summary>
        public float BGMVolume = 1.0f;

        /// <summary>
        /// 屏幕上所有角色的状态列表
        /// </summary>
        public List<CharacterState> Characters = new List<CharacterState>();

        /// <summary>
        /// 解密系统状态：Key是解密房间/物体ID，Value是其状态的JSON字符串
        /// 预留给解密系统使用，可以存储任意复杂的解密状态
        /// </summary>
        public Dictionary<string, string> PuzzleStates = new Dictionary<string, string>();

        /// <summary>
        /// 当前周目的剧情变量（例如：好感度、选项计数等）
        /// </summary>
        public Dictionary<string, int> StoryVariables = new Dictionary<string, int>();

        /// <summary>
        /// 字符串类型的剧情变量（用于存储非数值型数据）
        /// </summary>
        public Dictionary<string, string> StoryStringVariables = new Dictionary<string, string>();

        /// <summary>
        /// 当前章节名称
        /// </summary>
        public string CurrentChapter;

        /// <summary>
        /// 存档创建时间戳
        /// </summary>
        public long SaveTimestamp;

        /// <summary>
        /// 总游玩时长（秒）
        /// </summary>
        public long PlayTimeSeconds;
    }

    /// <summary>
    /// 存档槽元数据：不读取完整存档就能看到的UI信息
    /// </summary>
    [System.Serializable]
    public class SaveSlotMeta
    {
        /// <summary>
        /// 存档槽位ID（0-99等）
        /// </summary>
        public int SlotId;

        /// <summary>
        /// 格式化后的保存时间字符串（例如："2024-01-15 14:30"）
        /// </summary>
        public string SaveTime;

        /// <summary>
        /// 章节名称（用于UI显示）
        /// </summary>
        public string ChapterName;

        /// <summary>
        /// 截图文件路径（相对路径或文件名）
        /// </summary>
        public string ScreenshotPath;

        /// <summary>
        /// 总游玩时长（秒）
        /// </summary>
        public long PlayTimeSeconds;

        /// <summary>
        /// 存档是否为空
        /// </summary>
        public bool IsEmpty = true;

        /// <summary>
        /// 获取格式化的游玩时长字符串（HH:MM:SS）
        /// </summary>
        public string GetFormattedPlayTime()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(PlayTimeSeconds);
            return string.Format("{0:D2}:{1:D2}:{2:D2}", 
                (int)timeSpan.TotalHours, 
                timeSpan.Minutes, 
                timeSpan.Seconds);
        }
    }

    /// <summary>
    /// 完整存档数据：包含存档数据和元数据
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        /// <summary>
        /// 存档元数据
        /// </summary>
        public SaveSlotMeta Metadata;

        /// <summary>
        /// 实际游戏进度数据
        /// </summary>
        public ArchiveData ArchiveData;

        public SaveData()
        {
            Metadata = new SaveSlotMeta();
            ArchiveData = new ArchiveData();
        }
    }
}
