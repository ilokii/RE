# 主菜单场景设置指南

本文档说明如何在Unity中搭建主菜单场景（MainMenuScene）的UI结构。

## 阶段二完成内容

已创建以下脚本：
- **MainMenuController.cs**: 主菜单控制器，管理UI交互和场景切换
- **SaveSlotUI.cs**: 存档槽位UI组件，显示存档信息

## Unity场景搭建步骤

### 1. 创建主菜单场景

1. 在Unity中创建新场景：`File > New Scene`
2. 保存场景为：`Assets/Scenes/MainMenuScene.unity`

### 2. UI层级结构

在场景中创建以下UI层级结构：

```
Canvas (Screen Space - Overlay)
├── MainPanel
│   ├── Title (TextMeshPro - Text)
│   ├── Button_NewGame (Button)
│   │   └── Text (TextMeshPro - Text: "新游戏")
│   ├── Button_Continue (Button)
│   │   └── Text (TextMeshPro - Text: "继续游戏")
│   ├── Button_Load (Button)
│   │   └── Text (TextMeshPro - Text: "读取存档")
│   └── Button_Quit (Button)
│       └── Text (TextMeshPro - Text: "退出游戏")
└── SaveLoadPanel (默认隐藏)
    ├── Title (TextMeshPro - Text: "读取存档")
    ├── AutoSaveSlot (GameObject)
    │   └── SaveSlotUI组件
    ├── ManualSaveSlot_0 (GameObject)
    │   └── SaveSlotUI组件
    ├── ManualSaveSlot_1 (GameObject)
    │   └── SaveSlotUI组件
    ├── ManualSaveSlot_2 (GameObject)
    │   └── SaveSlotUI组件
    ├── ConfirmDialog (新增 - 确认对话框，默认隐藏)
    │   ├── Background (Image - 半透明遮罩)
    │   └── DialogPanel (Panel)
    │       ├── TitleText (TextMeshPro - Text)
    │       ├── MessageText (TextMeshPro - Text)
    │       ├── DetailText (TextMeshPro - Text)
    │       ├── ButtonConfirm (Button)
    │       │   └── Text (TextMeshPro - Text: "确认删除")
    │       └── ButtonCancel (Button)
    │           └── Text (TextMeshPro - Text: "取消")
    └── Button_Back (Button)
        └── Text (TextMeshPro - Text: "返回")
```

### 3. 详细组件配置

#### 3.1 Canvas设置
- Render Mode: Screen Space - Overlay
- Canvas Scaler: Scale With Screen Size
- Reference Resolution: 1920x1080

#### 3.2 MainPanel
- 添加组件：`RectTransform`
- Anchor: Stretch (全屏)
- 可选：添加背景图片（Image组件）

#### 3.3 SaveLoadPanel
- 添加组件：`RectTransform`
- Anchor: Stretch (全屏)
- **重要**：在Inspector中取消勾选"Active"，默认隐藏
- 可选：添加背景图片（Image组件）

#### 3.4 SaveSlotUI预制体结构

每个存档槽位（AutoSaveSlot, ManualSaveSlot_0/1/2）应包含以下子对象：

```
SaveSlot (GameObject)
├── SlotButton (Button) - 整个槽位可点击（加载存档）
├── DeleteButton (Button) - 删除按钮（新增，建议放在右上角）
│   └── Text/Icon (TextMeshPro - Text: "×" 或 "删除")
├── Screenshot (Image) - 存档截图
├── SlotIdText (TextMeshPro - Text) - 槽位名称（如"自动存档"、"存档1"）
├── ChapterNameText (TextMeshPro - Text) - 章节名称
├── SaveTimeText (TextMeshPro - Text) - 保存时间
├── PlayTimeText (TextMeshPro - Text) - 游玩时长
└── EmptySlotIndicator (GameObject) - 空槽位指示器
    └── Text (TextMeshPro - Text: "空槽位")
```

**SaveSlotUI组件配置**：
- 将上述UI元素拖拽到SaveSlotUI脚本的对应字段
- Slot Button: 拖拽SlotButton
- Delete Button: 拖拽DeleteButton（新增）
- Screenshot Image: 拖拽Screenshot
- Chapter Name Text: 拖拽ChapterNameText
- Save Time Text: 拖拽SaveTimeText
- Play Time Text: 拖拽PlayTimeText
- Empty Slot Indicator: 拖拽EmptySlotIndicator
- Slot Id Text: 拖拽SlotIdText
- Empty Slot Text: "空槽位"
- Default Screenshot: 设置一个默认的占位图片
- Show Delete Button: 勾选（默认显示删除按钮）

### 4. MainMenuController配置

1. 在Canvas或MainPanel上添加`MainMenuController`组件
2. 在Inspector中配置引用：

**主面板引用**：
- Main Panel: 拖拽MainPanel对象
- Button New Game: 拖拽Button_NewGame
- Button Continue: 拖拽Button_Continue
- Button Load: 拖拽Button_Load
- Button Quit: 拖拽Button_Quit

**存档/读档面板引用**：
- Save Load Panel: 拖拽SaveLoadPanel对象
- Button Back: 拖拽Button_Back
- Auto Save Slot: 拖拽AutoSaveSlot的SaveSlotUI组件
- Manual Save Slots: 设置Size为3，拖拽3个手动存档槽位的SaveSlotUI组件

**确认对话框**（新增）：
- Confirm Dialog: 拖拽ConfirmDialog对象的ConfirmDialog组件

**UI文本**（可选）：
- Title Text: 拖拽主标题Text组件

**删除存档设置**（新增）：
- Allow Delete Auto Save: 是否允许删除自动存档（默认false）
- Protect Last Save: 是否保护最后一个存档（默认false）

### 5. 场景引用配置

确保在Build Settings中添加场景：
1. `File > Build Settings`
2. 添加MainMenuScene
3. 添加GameplayScene（游戏主场景）

**重要**：GameplayScene的名称必须与GameEntryManager中的场景名称一致。

## 功能说明

### Continue按钮逻辑
- 在`Start()`中自动检查所有存档（包括自动存档和手动存档）
- 找到时间戳最新的存档
- 如果没有任何存档，按钮将被禁用
- 点击后调用`GameEntryManager.LoadFromSave(latestSlotId)`

### Load按钮逻辑
- 点击打开SaveLoadPanel
- 自动刷新所有存档槽位的显示
- 显示每个槽位的元数据：章节名、保存时间、游玩时长、截图
- 点击具体存档槽位后，调用`GameEntryManager.LoadFromSave(slotId)`

### New Game按钮逻辑
- 点击调用`GameEntryManager.StartNewGame()`
- 将加载GameplayScene并标记为新游戏模式

### Quit按钮逻辑
- 在编辑器中：停止播放
- 在构建版本中：调用`Application.Quit()`

### 删除存档功能（新增）
- 每个非空存档槽位显示删除按钮
- 点击删除按钮显示确认对话框
- 确认对话框显示存档详细信息（槽位名、章节、时间）
- 用户确认后调用`SaveManager.DeleteSave(slotId)`
- 删除成功后自动刷新UI并更新Continue按钮状态
- 支持ESC键取消删除

**安全特性**：
- 空槽位不显示删除按钮
- 自动存档可配置是否允许删除（默认不允许）
- 可配置是否保护最后一个存档
- 二次确认防止误删

## 存档槽位ID说明

- **自动存档**: SlotId = -1
- **手动存档1**: SlotId = 0
- **手动存档2**: SlotId = 1
- **手动存档3**: SlotId = 2

## ConfirmDialog组件配置

在SaveLoadPanel下创建ConfirmDialog对象，添加`ConfirmDialog`组件：

**UI组件引用**：
- Dialog Panel: 拖拽DialogPanel对象
- Title Text: 拖拽TitleText
- Message Text: 拖拽MessageText
- Detail Text: 拖拽DetailText
- Confirm Button: 拖拽ButtonConfirm
- Cancel Button: 拖拽ButtonCancel

**按钮文本**：
- Confirm Button Text: "确认"
- Cancel Button Text: "取消"

**重要**：在Inspector中取消勾选ConfirmDialog的"Active"，默认隐藏。

## 测试清单

### 基础功能
- [ ] 主菜单场景正确显示
- [ ] 没有存档时，Continue按钮被禁用
- [ ] 有存档时，Continue按钮可用且加载最新存档
- [ ] Load按钮打开存档面板
- [ ] 存档面板正确显示所有槽位信息
- [ ] 点击存档槽位能正确加载游戏
- [ ] Back按钮返回主菜单
- [ ] New Game按钮启动新游戏
- [ ] Quit按钮退出游戏

### 删除存档功能（新增）
- [ ] 非空槽位显示删除按钮
- [ ] 空槽位不显示删除按钮
- [ ] 点击删除按钮显示确认对话框
- [ ] 确认对话框显示正确的存档信息
- [ ] 点击"确认删除"成功删除存档
- [ ] 点击"取消"关闭对话框且不删除
- [ ] 删除后UI立即刷新显示空槽位
- [ ] 删除最新存档后Continue按钮正确更新
- [ ] ESC键可以取消删除
- [ ] 自动存档保护功能正常（如果启用）
- [ ] 最后一个存档保护功能正常（如果启用）

## 下一步

等待阶段三文档，继续完善主页功能。
