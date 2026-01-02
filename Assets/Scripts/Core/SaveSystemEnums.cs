/// <summary>
/// 存档系统相关的枚举定义
/// 这些枚举被放在全局命名空间中，以便所有脚本都能访问
/// </summary>

/// <summary>
/// 自动存档触发器类型
/// </summary>
public enum AutoSaveTrigger
{
    /// <summary>
    /// 时间间隔触发（例如每10分钟）
    /// </summary>
    TimeInterval,

    /// <summary>
    /// 章节结束时触发
    /// </summary>
    ChapterEnd,

    /// <summary>
    /// 解密完成时触发
    /// </summary>
    PuzzleComplete,

    /// <summary>
    /// 关键决策做出后触发
    /// </summary>
    DecisionMade
}

/// <summary>
/// 游戏状态枚举（用于判断是否可以自动存档）
/// </summary>
public enum GameState
{
    /// <summary>
    /// 空闲状态（可以安全存档）
    /// </summary>
    Idle,

    /// <summary>
    /// 对话中
    /// </summary>
    InDialogue,

    /// <summary>
    /// 动画播放中
    /// </summary>
    InAnimation,

    /// <summary>
    /// 场景转换中
    /// </summary>
    InTransition,

    /// <summary>
    /// 解密中
    /// </summary>
    InPuzzle
}
