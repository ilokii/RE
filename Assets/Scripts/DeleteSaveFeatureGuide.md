# 删除存档功能使用指南

本文档说明如何使用和测试删除存档功能。

## 功能概述

删除存档功能允许玩家在主菜单的存档/读档面板中删除不需要的存档。该功能包含以下特性：

- ✅ 每个非空存档槽位显示删除按钮
- ✅ 二次确认对话框防止误删
- ✅ 自动存档保护（可配置）
- ✅ 最后一个存档保护（可配置）
- ✅ 删除后自动刷新UI
- ✅ ESC键取消删除

## 已创建的组件

### 1. SaveSlotUI.cs（已修改）

**新增字段**：
- `deleteButton`: 删除按钮引用
- `showDeleteButton`: 是否显示删除按钮
- `onDeleteClicked`: 删除回调

**新增方法**：
- `Initialize(metadata, clickCallback, deleteCallback)`: 初始化时可传入删除回调
- `OnDeleteButtonClicked()`: 删除按钮点击处理
- `SetShowDeleteButton(bool)`: 设置是否显示删除按钮

### 2. ConfirmDialog.cs（新建）

通用确认对话框组件，支持：
- 自定义标题、消息、详细信息
- 确认和取消回调
- ESC键取消
- 自动隐藏

**主要方法**：
- `Show(title, message, confirmCallback, cancelCallback, detail)`: 显示对话框
- `Hide()`: 隐藏对话框
- `IsShowing()`: 检查是否正在显示

### 3. MainMenuController.cs（已修改）

**新增字段**：
- `confirmDialog`: 确认对话框引用
- `allowDeleteAutoSave`: 是否允许删除自动存档
- `protectLastSave`: 是否保护最后一个存档
- `currentDeletingSlotId`: 当前待删除的槽位ID

**新增方法**：
- `OnDeleteSlotRequested(int slotId)`: 删除请求处理
- `ShowDeleteConfirmDialog(int slotId)`: 显示删除确认对话框
- `OnConfirmDelete()`: 确认删除回调
- `OnCancelDelete()`: 取消删除回调
- `CountNonEmptySaves()`: 统计非空存档数量

## Unity场景配置步骤

### 步骤1：更新SaveSlot预制体

为每个存档槽位添加删除按钮：

1. 打开SaveSlot预制体或在场景中选择存档槽位
2. 添加删除按钮：
   - 右键点击SaveSlot → UI → Button
   - 命名为"DeleteButton"
   - 设置位置（建议右上角）
   - 设置大小（如 40x40）
   - 添加文本或图标（"×" 或垃圾桶图标）

3. 配置SaveSlotUI组件：
   - 将DeleteButton拖拽到"Delete Button"字段
   - 勾选"Show Delete Button"

**删除按钮样式建议**：
- 颜色：红色或警告色（如 #FF4444）
- 位置：右上角（Anchor: Top Right）
- 大小：40x40 像素
- 文本：使用 "×" 符号或垃圾桶图标

### 步骤2：创建ConfirmDialog

在SaveLoadPanel下创建确认对话框：

```
SaveLoadPanel
└── ConfirmDialog (GameObject)
    ├── Background (Image)
    │   - Color: 黑色，Alpha: 128 (半透明)
    │   - Anchor: Stretch (全屏)
    └── DialogPanel (Image/Panel)
        - Anchor: Center
        - Size: 400x250
        ├── TitleText (TextMeshPro - Text)
        │   - Text: "确认删除"
        │   - Font Size: 24
        │   - Alignment: Center
        ├── MessageText (TextMeshPro - Text)
        │   - Text: "确定要删除此存档吗？"
        │   - Font Size: 18
        │   - Alignment: Center
        ├── DetailText (TextMeshPro - Text)
        │   - Text: ""
        │   - Font Size: 14
        │   - Alignment: Center
        │   - Color: 灰色
        ├── ButtonConfirm (Button)
        │   - Color: 红色 (#FF4444)
        │   └── Text: "确认删除"
        └── ButtonCancel (Button)
            - Color: 灰色
            └── Text: "取消"
```

**重要**：
1. 在ConfirmDialog对象上添加`ConfirmDialog`组件
2. 配置所有UI引用
3. 取消勾选ConfirmDialog的"Active"（默认隐藏）

### 步骤3：配置MainMenuController

在MainMenuController组件中：

1. **确认对话框**：
   - 拖拽ConfirmDialog对象到"Confirm Dialog"字段

2. **删除存档设置**：
   - Allow Delete Auto Save: 根据需求勾选（默认false）
   - Protect Last Save: 根据需求勾选（默认false）

## 使用流程

### 用户操作流程

```
1. 玩家打开主菜单
   ↓
2. 点击"读取存档"按钮
   ↓
3. 看到存档列表，每个非空槽位有删除按钮
   ↓
4. 点击某个存档的删除按钮
   ↓
5. 弹出确认对话框，显示存档详情
   ↓
6. 玩家选择：
   - 点击"确认删除" → 删除存档 → UI刷新
   - 点击"取消" 或 按ESC → 关闭对话框
```

### 代码执行流程

```csharp
// 1. 用户点击删除按钮
SaveSlotUI.OnDeleteButtonClicked()
    ↓
// 2. 调用删除回调
MainMenuController.OnDeleteSlotRequested(slotId)
    ↓
// 3. 检查保护规则
if (protectLastSave && CountNonEmptySaves() <= 1)
    → 显示"无法删除"提示
else
    ↓
// 4. 显示确认对话框
MainMenuController.ShowDeleteConfirmDialog(slotId)
    ↓
ConfirmDialog.Show(title, message, OnConfirmDelete, OnCancelDelete, detail)
    ↓
// 5. 用户确认
MainMenuController.OnConfirmDelete()
    ↓
// 6. 删除存档
SaveManager.Instance.DeleteSave(currentDeletingSlotId)
    ↓
// 7. 刷新UI
RefreshSaveSlots()
CheckSaveStatus()
```

## 配置选项说明

### allowDeleteAutoSave（允许删除自动存档）

- **false（默认）**: 自动存档不显示删除按钮，无法删除
- **true**: 自动存档可以删除

**使用场景**：
- 如果自动存档是系统自动管理的，建议设为false
- 如果允许玩家完全控制所有存档，可设为true

### protectLastSave（保护最后一个存档）

- **false（默认）**: 可以删除所有存档
- **true**: 至少保留一个存档，最后一个无法删除

**使用场景**：
- 如果希望玩家始终有一个存档可以继续游戏，设为true
- 如果允许玩家删除所有存档重新开始，设为false

## 测试指南

### 基础功能测试

1. **删除按钮显示**
   - [ ] 打开存档面板
   - [ ] 确认非空槽位显示删除按钮
   - [ ] 确认空槽位不显示删除按钮

2. **删除流程测试**
   - [ ] 点击删除按钮
   - [ ] 确认对话框弹出
   - [ ] 确认对话框显示正确的存档信息
   - [ ] 点击"确认删除"
   - [ ] 确认存档被删除
   - [ ] 确认UI刷新显示空槽位

3. **取消删除测试**
   - [ ] 点击删除按钮
   - [ ] 点击"取消"
   - [ ] 确认对话框关闭
   - [ ] 确认存档未被删除
   - [ ] 按ESC键也能取消

### 保护功能测试

4. **自动存档保护**（allowDeleteAutoSave = false）
   - [ ] 确认自动存档不显示删除按钮
   - [ ] 或删除按钮被禁用

5. **最后存档保护**（protectLastSave = true）
   - [ ] 删除存档直到只剩一个
   - [ ] 尝试删除最后一个
   - [ ] 确认显示"无法删除"提示
   - [ ] 确认存档未被删除

### UI更新测试

6. **Continue按钮更新**
   - [ ] 删除最新的存档
   - [ ] 确认Continue按钮加载次新的存档
   - [ ] 删除所有存档
   - [ ] 确认Continue按钮被禁用

7. **截图文件删除**
   - [ ] 删除一个存档
   - [ ] 检查persistentDataPath/Screenshots目录
   - [ ] 确认对应的截图文件也被删除

### 边界情况测试

8. **空槽位测试**
   - [ ] 尝试点击空槽位的删除按钮（应该不存在或被禁用）
   - [ ] 确认不会触发删除流程

9. **连续删除测试**
   - [ ] 快速连续点击多个删除按钮
   - [ ] 确认对话框正确处理
   - [ ] 确认不会出现错误

10. **场景切换测试**
    - [ ] 打开删除确认对话框
    - [ ] 不关闭对话框，返回主面板
    - [ ] 确认对话框正确隐藏
    - [ ] 再次打开存档面板
    - [ ] 确认状态正常

## 常见问题

### Q: 删除按钮不显示？
A: 检查以下几点：
1. SaveSlotUI的"Delete Button"字段是否已配置
2. "Show Delete Button"是否勾选
3. 槽位是否为空（空槽位不显示删除按钮）
4. 如果是自动存档，检查"Allow Delete Auto Save"设置

### Q: 点击删除按钮没反应？
A: 检查：
1. MainMenuController的"Confirm Dialog"字段是否已配置
2. ConfirmDialog组件是否正确配置
3. 查看Console是否有错误信息

### Q: 删除后UI没有刷新？
A: 确认：
1. SaveManager.DeleteSave()返回true
2. RefreshSaveSlots()被调用
3. CheckSaveStatus()被调用

### Q: 如何自定义删除按钮样式？
A: 修改DeleteButton的：
- Image组件的颜色
- RectTransform的位置和大小
- 子对象Text的文本和字体

### Q: 如何自定义确认对话框文本？
A: 在MainMenuController.ShowDeleteConfirmDialog()中修改：
```csharp
confirmDialog.Show(
    "自定义标题",
    "自定义消息",
    OnConfirmDelete,
    OnCancelDelete,
    "自定义详情"
);
```

## 扩展功能建议

### 1. 添加删除音效

在OnConfirmDelete()中：
```csharp
// 播放删除音效
if (sfxSource != null && deleteSFX != null)
{
    sfxSource.PlayOneShot(deleteSFX);
}
```

### 2. 添加删除动画

在RefreshSaveSlots()前：
```csharp
// 淡出动画
StartCoroutine(FadeOutSlot(currentDeletingSlotId));
```

### 3. 批量删除

添加"全部删除"按钮：
```csharp
public void DeleteAllSaves()
{
    confirmDialog.Show(
        "确认删除全部",
        "确定要删除所有存档吗？",
        () => {
            for (int i = 0; i < 3; i++)
                SaveManager.Instance.DeleteSave(i);
            SaveManager.Instance.DeleteSave(-1);
            RefreshSaveSlots();
            CheckSaveStatus();
        }
    );
}
```

## 总结

删除存档功能已完全实现，包括：
- ✅ UI组件（SaveSlotUI, ConfirmDialog）
- ✅ 逻辑控制（MainMenuController）
- ✅ 安全保护（二次确认、自动存档保护、最后存档保护）
- ✅ 用户体验（ESC取消、UI刷新、详细信息显示）

按照本指南配置Unity场景后，即可使用完整的删除存档功能。
