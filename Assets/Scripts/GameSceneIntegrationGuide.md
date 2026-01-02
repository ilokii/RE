# 游戏场景接入指南（阶段三）

本文档说明如何将游戏场景与主菜单系统集成，实现新游戏和读档功能。

## 阶段三完成内容

已创建和修改以下脚本：
- **GameFlowController.cs**: 游戏流程控制器，负责游戏场景初始化
- **DialogueManager.cs**: 修改为支持外部控制，不再自动加载脚本
- **DialogueManagerSaveExample.cs**: 修改为正确保存当前脚本名称

## 核心逻辑流程

### 1. 游戏场景启动流程

```
GameplayScene 加载
    ↓
GameFlowController.Start()
    ↓
检查 GameEntryManager.IsLoadingFromSave
    ↓
┌─────────────────┬─────────────────┐
│   True (读档)    │  False (新游戏)  │
└─────────────────┴─────────────────┘
         ↓                   ↓
   LoadFromSave()      StartNewGame()
         ↓                   ↓
SaveManager.LoadGame()  ResetGameState()
         ↓                   ↓
DialogueManager          LoadScript()
自动恢复状态              (DefaultStartScript)
```

### 2. 新游戏流程详解

当玩家从主菜单点击"新游戏"时：

1. **主菜单**: `MainMenuController.OnNewGameClicked()`
   - 调用 `GameEntryManager.Instance.StartNewGame()`
   - 设置 `IsLoadingFromSave = false`
   - 加载 "GameplayScene"

2. **游戏场景**: `GameFlowController.InitializeGame()`
   - 检测到 `IsLoadingFromSave = false`
   - 调用 `StartNewGame()`
   - 执行 `ResetGameState()` 清除当前周目数据
   - 调用 `dialogueManager.LoadScript(DefaultStartScript)`
   - 开始播放指定的对话脚本

### 3. 读档流程详解

当玩家从主菜单点击"继续游戏"或"读取存档"时：

1. **主菜单**: `MainMenuController.OnContinueClicked()` 或 `OnSaveSlotClicked()`
   - 调用 `GameEntryManager.Instance.LoadFromSave(slotId)`
   - 设置 `IsLoadingFromSave = true`
   - 设置 `TargetSaveSlotId = slotId`
   - 加载 "GameplayScene"

2. **游戏场景**: `GameFlowController.InitializeGame()`
   - 检测到 `IsLoadingFromSave = true`
   - 调用 `LoadFromSave()`
   - 调用 `SaveManager.Instance.LoadGame(TargetSaveSlotId)`
   - SaveManager 自动调用所有 ISavable 的 `RestoreState()`
   - DialogueManager 恢复：脚本文件、行号、背景、BGM等
   - 游戏从存档点继续

## Unity 场景配置

### 1. GameplayScene 设置

在 GameplayScene 中添加以下组件：

```
GameplayScene
├── Canvas
│   ├── DialogueUI (包含 DialogueManager 组件)
│   └── ... (其他UI元素)
├── GameFlowController (新增)
├── SaveManager (如果还没有)
└── ... (其他游戏对象)
```

### 2. GameFlowController 配置

1. 在场景中创建空对象，命名为 "GameFlowController"
2. 添加 `GameFlowController` 组件
3. 在 Inspector 中配置：

**管理器引用**:
- Dialogue Manager: 拖拽场景中的 DialogueManager 组件

**新游戏设置**:
- Default Start Script: "Chapter1_Start" (或你的启动脚本名)
- Default Background: "SchoolDay" (可选，留空则由脚本控制)
- Default BGM: "DailyTheme" (可选，留空则由脚本控制)

### 3. DialogueManager 修改说明

DialogueManager 已被修改为：
- 不再在 `Start()` 中自动加载 `startScript`
- 改由 `GameFlowController` 控制何时加载脚本
- 如果场景中没有 `GameFlowController`（测试模式），会自动加载默认脚本

**重要**: 确保 DialogueManager 的 `startScript` 字段仍然设置为测试脚本名，用于独立测试。

## 数据流说明

### 新游戏数据重置

`GameFlowController.ResetGameState()` 会：
- **保留**: 全局数据（GlobalData）- 结局、CG、已读文本等跨周目数据
- **清除**: 当前周目的剧情变量、角色状态等
- **重置**: 屏幕上的所有角色立绘

### 读档数据恢复

`SaveManager.LoadGame()` 会自动恢复：
- 当前对话脚本文件名
- 当前对话行号
- 背景图片
- BGM 和音量
- 屏幕上的角色状态（位置、表情、可见性）
- 解密系统状态
- 剧情变量

## 扩展功能

### 快速保存/读取

GameFlowController 提供了快速保存和读取的方法：

```csharp
// 快速保存（保存到自动存档槽位-1）
GameFlowController.Instance.QuickSave();

// 快速读取（从自动存档槽位-1读取）
GameFlowController.Instance.QuickLoad();
```

可以绑定到快捷键：

```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.F5))
    {
        FindObjectOfType<GameFlowController>().QuickSave();
    }
    
    if (Input.GetKeyDown(KeyCode.F9))
    {
        FindObjectOfType<GameFlowController>().QuickLoad();
    }
}
```

### 返回主菜单

```csharp
// 从游戏场景返回主菜单
GameFlowController.Instance.ReturnToMainMenu();
```

## 测试清单

### 新游戏测试
- [ ] 从主菜单点击"新游戏"
- [ ] 游戏场景正确加载
- [ ] 对话系统从指定的启动脚本开始
- [ ] 背景和音乐正确设置
- [ ] 没有残留的角色立绘

### 读档测试
- [ ] 创建一个存档（在游戏中保存）
- [ ] 返回主菜单
- [ ] 点击"继续游戏"或选择存档槽位
- [ ] 游戏从存档点正确恢复
- [ ] 对话、背景、音乐、角色状态都正确

### 快速保存/读取测试
- [ ] 在游戏中按 F5 快速保存
- [ ] 继续游戏一段时间
- [ ] 按 F9 快速读取
- [ ] 游戏回到快速保存的位置

### 独立测试
- [ ] 直接打开 GameplayScene（不通过主菜单）
- [ ] DialogueManager 自动加载默认脚本
- [ ] 游戏正常运行

## 常见问题

### Q: 读档后对话没有恢复到正确位置？
A: 检查 DialogueManager 是否正确实现了 ISavable 接口，并且在 Start() 中注册到了 SaveManager。

### Q: 新游戏时有残留的角色立绘？
A: 确保 `GameFlowController.ResetGameState()` 中调用了 `PortraitManager.Instance.HideAllCharacters()`。

### Q: 场景名称不匹配？
A: 确保 `GameEntryManager.StartNewGame()` 和 `LoadFromSave()` 中的场景名称与实际场景名称一致。

### Q: DialogueManager 自动加载了默认脚本？
A: 确保场景中有 GameFlowController，并且它在 DialogueManager 之前执行（通过 Script Execution Order 设置）。

## 下一步

主页系统的三个阶段已全部完成：
1. ✅ 阶段一：跨场景数据传输（GameEntryManager）
2. ✅ 阶段二：UI 结构与面板逻辑（MainMenuController + SaveSlotUI）
3. ✅ 阶段三：游戏场景接入（GameFlowController）

现在可以开始测试完整的主菜单 → 游戏场景 → 存档/读档流程。
