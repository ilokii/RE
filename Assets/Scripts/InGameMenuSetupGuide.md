# 游戏内系统菜单设置指南

本文档说明如何在游戏场景中搭建系统菜单（SystemMenuCanvas）。

## 阶段二完成内容

已创建以下脚本：
- **InGameMenuController.cs**: 游戏内系统菜单控制器
- **SaveLoadPanelController.cs**: 双模式存档面板控制器

## Unity场景搭建步骤

### 1. 在GameplayScene中创建SystemMenuCanvas

```
GameplayScene
├── (现有的游戏UI Canvas)
└── SystemMenuCanvas (新增)
    ├── MenuPanel (系统菜单)
    └── SaveLoadPanel (存档/读档面板)
```

### 2. SystemMenuCanvas层级结构

```
SystemMenuCanvas (Canvas)
├── MenuToggleButton (可选 - 右上角Menu按钮)
│   └── Text (TextMeshPro: "菜单")
├── MenuPanel (默认隐藏)
│   ├── Background (Image - 半透明遮罩)
│   ├── Title (TextMeshPro: "系统菜单")
│   ├── Button_Resume (Button)
│   │   └── Text (TextMeshPro: "继续游戏")
│   ├── Button_Save (Button)
│   │   └── Text (TextMeshPro: "保存进度")
│   ├── Button_Load (Button)
│   │   └── Text (TextMeshPro: "读取存档")
│   └── Button_BackToTitle (Button)
│       └── Text (TextMeshPro: "返回主菜单")
└── SaveLoadPanel (复用主菜单的存档面板结构)
    ├── PanelRoot (GameObject)
    │   ├── Title (TextMeshPro)
    │   ├── AutoSaveSlot (SaveSlotUI)
    │   ├── ManualSaveSlot_0 (SaveSlotUI)
    │   ├── ManualSaveSlot_1 (SaveSlotUI)
    │   ├── ManualSaveSlot_2 (SaveSlotUI)
    │   ├── ConfirmDialog
    │   └── Button_Back (Button)
    └── SaveLoadPanelController组件
```

### 3. Canvas设置

#### SystemMenuCanvas配置
- Render Mode: Screen Space - Overlay
- Sort Order: 100 (确保在游戏UI之上)
- Canvas Scaler: Scale With Screen Size
- Reference Resolution: 1920x1080

### 4. MenuPanel配置

**RectTransform**:
- Anchor: Stretch (全屏)

**Background (Image)**:
- Color: 黑色，Alpha: 180 (半透明遮罩)
- Raycast Target: 勾选（阻止点击穿透）

**按钮布局建议**:
- 垂直排列，居中显示
- 按钮大小: 200x50
- 间距: 20像素

**重要**: 在Inspector中取消勾选MenuPanel的"Active"（默认隐藏）

### 5. MenuToggleButton配置（可选）

如果需要右上角的Menu按钮：

**RectTransform**:
- Anchor: Top Right
- Position: (-50, -50, 0)
- Size: 80x40

**样式**:
- 半透明背景
- 文本: "菜单" 或 "MENU"

### 6. SaveLoadPanel配置

复用主菜单的SaveLoadPanel结构，但需要：

1. 添加 `SaveLoadPanelController` 组件
2. 配置所有引用：
   - Panel Root: 拖拽PanelRoot对象
   - Title Text: 拖拽Title
   - Button Back: 拖拽Button_Back
   - Auto Save Slot: 拖拽AutoSaveSlot的SaveSlotUI组件
   - Manual Save Slots: 拖拽3个手动存档槽位
   - Confirm Dialog: 拖拽ConfirmDialog组件

3. 文本配置：
   - Save Mode Title: "保存进度"
   - Load Mode Title: "读取存档"

### 7. InGameMenuController配置

在SystemMenuCanvas上添加 `InGameMenuController` 组件：

**UI组件引用**:
- Menu Panel: 拖拽MenuPanel
- Button Resume: 拖拽Button_Resume
- Button Save: 拖拽Button_Save
- Button Load: 拖拽Button_Load
- Button Back To Title: 拖拽Button_BackToTitle
- Button Menu Toggle: 拖拽MenuToggleButton（可选）

**面板引用**:
- Save Load Panel Controller: 拖拽SaveLoadPanel的SaveLoadPanelController组件
- Confirm Dialog: 拖拽ConfirmDialog组件

**设置**:
- Menu Toggle Key: Escape（默认）
- Pause Game When Open: 勾选（默认）

## 功能说明

### 菜单切换

**打开菜单**:
- 按ESC键
- 点击右上角Menu按钮（如果有）

**关闭菜单**:
- 按ESC键
- 点击Resume按钮

### 暂停机制

- 菜单打开时：`Time.timeScale = 0`（暂停游戏）
- 菜单关闭时：`Time.timeScale = 1`（恢复游戏）

**注意**: 暂停会影响：
- 所有使用 `Time.deltaTime` 的动画和移动
- 协程中的 `WaitForSeconds`
- 不影响UI动画（使用unscaledTime）

### 按钮功能

#### Resume按钮
- 关闭菜单
- 恢复游戏（Time.timeScale = 1）

#### Save按钮
- 隐藏系统菜单
- 打开存档面板（Save模式）
- 自动存档槽位不可点击
- 点击空槽位直接保存
- 点击非空槽位显示覆盖确认

#### Load按钮
- 隐藏系统菜单
- 打开存档面板（Load模式）
- 所有非空槽位可点击
- 点击槽位加载游戏

#### Back to Title按钮
- 显示确认对话框
- 确认后返回主菜单
- 自动恢复Time.timeScale

## 代码集成

### 在DialogueManager中集成

可以在DialogueManager中添加章节名称更新：

```csharp
// 当加载新脚本或到达新章节时
InGameMenuController menuController = FindObjectOfType<InGameMenuController>();
if (menuController != null)
{
    menuController.SetChapterName("第一章");
}
```

### 禁用菜单期间的输入

在DialogueManager的Update中：

```csharp
void Update()
{
    // 检查菜单是否打开
    InGameMenuController menuController = FindObjectOfType<InGameMenuController>();
    if (menuController != null && menuController.IsMenuOpen())
    {
        return; // 菜单打开时不处理对话输入
    }

    // 原有的输入处理逻辑
    if (!isWaitingForChoice && Input.GetMouseButtonDown(0))
    {
        // ...
    }
}
```

## UI层级完整示例

```
GameplayScene
├── GameCanvas (现有的对话UI)
│   ├── DialoguePanel
│   ├── ChoicePanel
│   └── ...
└── SystemMenuCanvas (新增)
    ├── InGameMenuController组件
    ├── MenuToggleButton (右上角)
    ├── MenuPanel (ESC打开的系统菜单)
    │   ├── Background
    │   ├── Title
    │   ├── Button_Resume
    │   ├── Button_Save
    │   ├── Button_Load
    │   └── Button_BackToTitle
    └── SaveLoadPanel
        ├── SaveLoadPanelController组件
        ├── PanelRoot
        │   ├── Title
        │   ├── AutoSaveSlot
        │   ├── ManualSaveSlot_0
        │   ├── ManualSaveSlot_1
        │   ├── ManualSaveSlot_2
        │   ├── ConfirmDialog
        │   └── Button_Back
        └── (所有SaveSlotUI的子对象)
```

## 测试清单

### 基础功能
- [ ] 按ESC键打开/关闭菜单
- [ ] 点击Menu按钮打开菜单（如果有）
- [ ] 菜单打开时游戏暂停（Time.timeScale = 0）
- [ ] 菜单关闭时游戏恢复（Time.timeScale = 1）

### 按钮功能
- [ ] Resume按钮关闭菜单并恢复游戏
- [ ] Save按钮打开存档面板（Save模式）
- [ ] Load按钮打开存档面板（Load模式）
- [ ] Back to Title按钮显示确认对话框
- [ ] 确认返回主菜单后Time.timeScale恢复为1

### 存档面板（Save模式）
- [ ] 标题显示"保存进度"
- [ ] 自动存档槽位不可点击
- [ ] 点击空槽位直接保存
- [ ] 点击非空槽位显示覆盖确认
- [ ] 保存成功后UI自动刷新
- [ ] Back按钮返回系统菜单

### 存档面板（Load模式）
- [ ] 标题显示"读取存档"
- [ ] 所有非空槽位可点击
- [ ] 点击槽位加载游戏
- [ ] 加载成功后菜单自动关闭
- [ ] Back按钮返回系统菜单

## 常见问题

### Q: 菜单打开后对话仍在继续？
A: 在DialogueManager的Update中添加菜单状态检查（见上方"禁用菜单期间的输入"）

### Q: 暂停后音乐也停了？
A: AudioSource默认受Time.timeScale影响。如果需要音乐继续播放，在AudioSource上取消勾选"Ignore Listener Pause"

### Q: 保存时章节名称显示"游戏进度"？
A: 调用 `InGameMenuController.SetChapterName()` 设置正确的章节名称

### Q: 返回主菜单后游戏仍然暂停？
A: InGameMenuController已在ReturnToMainMenu()中恢复Time.timeScale，检查是否有其他地方修改了timeScale

## 下一步

游戏内菜单系统已完成，包括：
- ✅ 系统菜单（暂停、Resume）
- ✅ 存档功能（Save模式）
- ✅ 读档功能（Load模式）
- ✅ 返回主菜单（带确认）

可以开始在Unity中搭建UI并测试完整流程。
