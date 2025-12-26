using UnityEngine;

// 定义支持的指令类型
public enum DialogueType
{
    DIALOG,         // 普通对话
    CMD_LOAD,       // 切换文件
    CMD_JUMP,       // 跳转行
    CMD_CHOOSE,     // 显示选项
    CMD_BG,         // 切换背景
    CMD_BGM,        // 切换音乐
    CMD_FOCUS,      // 聚焦立绘
    CMD_HIDE        // 立绘退场
}

// 定义单行剧本的数据结构
[System.Serializable]
public class DialogueLine
{
    public int id;              // ID (必填，数字)
    public DialogueType type;   // 类型 (自动解析 Enum)
    public string charId;       // 角色ID (对应 CharacterProfile)
    public string expression;   // 表情 (对应 Sprite Key)
    public string speed;        // 【新增】速度控制 (Fast, Normal, Slow)
    public string content;      // 对话内容 或 指令参数
    public string position;     // 立绘位置
}