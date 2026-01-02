using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SaveSystem;

/// <summary>
/// PortraitManager 的存档扩展
/// 实现 ISavable 接口来保存和恢复角色立绘状态
/// </summary>
public partial class PortraitManager : ISavable
{
    /// <summary>
    /// 捕获当前所有角色的状态
    /// </summary>
    public object CaptureState()
    {
        List<CharacterState> characterStates = new List<CharacterState>();

        Debug.Log($"[PortraitManager] 开始捕获角色状态，当前activeCharacters数量: {activeCharacters.Count}");

        // 遍历所有已显示的角色
        foreach (var kvp in activeCharacters)
        {
            string charId = kvp.Key;
            GameObject portraitObj = kvp.Value;

            Debug.Log($"[PortraitManager] 检查角色: {charId}, GameObject={(portraitObj != null ? "存在" : "null")}");

            if (portraitObj == null)
            {
                Debug.LogWarning($"[PortraitManager] 角色 {charId} 的GameObject为null，跳过");
                continue;
            }

            // 创建角色状态
            CharacterState state = new CharacterState
            {
                CharacterId = charId,
                IsVisible = portraitObj.activeSelf
            };

            // 获取位置信息
            if (characterPositions.ContainsKey(charId))
            {
                state.Position = characterPositions[charId].ToString();
            }
            else
            {
                Debug.LogWarning($"[PortraitManager] 角色 {charId} 没有位置记录");
                state.Position = "2"; // 默认中间位置
            }

            // 获取表情信息（从Image组件的sprite名称）
            Image img = portraitObj.GetComponent<Image>();
            if (img != null && img.sprite != null)
            {
                state.Expression = img.sprite.name;
            }
            else
            {
                Debug.LogWarning($"[PortraitManager] 角色 {charId} 没有Image或sprite");
            }

            // 获取层级信息（使用Transform的siblingIndex）
            state.SortingOrder = portraitObj.transform.GetSiblingIndex();

            characterStates.Add(state);
            Debug.Log($"[PortraitManager] ✓ 捕获角色状态: {charId}, 位置={state.Position}, 表情={state.Expression}, 可见={state.IsVisible}, 层级={state.SortingOrder}");
        }

        Debug.Log($"[PortraitManager] ===== 捕获完成，共 {characterStates.Count} 个角色 =====");
        return characterStates;
    }

    /// <summary>
    /// 恢复所有角色的状态
    /// </summary>
    public void RestoreState(object state)
    {
        if (state is List<CharacterState> characterStates)
        {
            Debug.Log($"[PortraitManager] ===== 开始恢复 {characterStates.Count} 个角色的状态 =====");

            // 先隐藏所有现有角色
            HideAllCharacters();
            Debug.Log("[PortraitManager] 已清空所有现有角色");

            // 恢复每个角色的状态
            for (int i = 0; i < characterStates.Count; i++)
            {
                var charState = characterStates[i];
                
                try
                {
                    Debug.Log($"[PortraitManager] [{i+1}/{characterStates.Count}] 处理角色: {charState.CharacterId}, 可见={charState.IsVisible}, 位置={charState.Position}, 表情={charState.Expression}");

                    if (!charState.IsVisible)
                    {
                        Debug.Log($"[PortraitManager] 跳过不可见角色: {charState.CharacterId}");
                        continue;
                    }

                    // 解析位置
                    int position = 2; // 默认中间位置
                    if (!string.IsNullOrEmpty(charState.Position))
                    {
                        if (!int.TryParse(charState.Position, out position))
                        {
                            Debug.LogWarning($"[PortraitManager] 无法解析位置: {charState.Position}，使用默认位置2");
                            position = 2;
                        }
                    }

                    Debug.Log($"[PortraitManager] 调用UpdatePortrait: charId={charState.CharacterId}, expression={charState.Expression}, position={position}");

                    // 【修复】检查positionAnchors是否有效
                    if (positionAnchors == null || positionAnchors.Length == 0)
                    {
                        Debug.LogError("[PortraitManager] positionAnchors 未配置或为空！");
                        continue;
                    }

                    if (position < 0 || position >= positionAnchors.Length)
                    {
                        Debug.LogWarning($"[PortraitManager] 位置索引 {position} 超出范围，使用默认位置2");
                        position = 2;
                    }

                    if (positionAnchors[position] == null)
                    {
                        Debug.LogError($"[PortraitManager] positionAnchors[{position}] 为null！");
                        continue;
                    }

                    // 恢复角色立绘（使用immediate模式，立即显示）
                    UpdatePortrait(charState.CharacterId, charState.Expression, position, true);

                    // 确保角色可见
                    if (activeCharacters.ContainsKey(charState.CharacterId))
                    {
                        GameObject charObj = activeCharacters[charState.CharacterId];
                        if (charObj != null)
                        {
                            charObj.SetActive(true);
                            Debug.Log($"[PortraitManager] ✓ 角色 {charState.CharacterId} 已激活，activeSelf={charObj.activeSelf}");
                        }
                        else
                        {
                            Debug.LogError($"[PortraitManager] 角色 {charState.CharacterId} 的GameObject为null！");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[PortraitManager] 角色 {charState.CharacterId} 不在activeCharacters中！");
                    }

                    // 恢复聚焦状态（如果需要）
                    if (charState.SortingOrder > 0)
                    {
                        SetFocusByCharacter(charState.CharacterId, true);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PortraitManager] 恢复角色 {charState.CharacterId} 时出错: {ex.Message}\n{ex.StackTrace}");
                }
            }

            Debug.Log($"[PortraitManager] ===== 角色状态恢复完成，当前在场角色数: {activeCharacters.Count} =====");
        }
        else
        {
            Debug.LogError("[PortraitManager] 恢复状态失败：状态数据类型不匹配");
        }
    }

    /// <summary>
    /// 注册到SaveManager（在Awake或Start中调用）
    /// </summary>
    private void RegisterToSaveManager()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSavable(this);
            Debug.Log("[PortraitManager] 已注册到SaveManager");
        }
    }
}
