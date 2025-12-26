using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "ADV/Character Profile")]
public class CharacterProfile : ScriptableObject
{
    [Header("基础信息")]
    public string characterId;    // 唯一ID，对应Excel里的 ID (例如: "hero")
    public string displayName;    // 游戏里显示的名字 (例如: "勇者")

    [Header("立绘管理")]
    // 我们用一个结构体列表来配置表情
    public List<PortraitData> portraits;

    // 这是一个辅助字典，方便快速查找，不用每次都遍历列表
    private Dictionary<string, Sprite> portraitDict;

    private void OnEnable() // 当资源加载时自动建立索引
    {
        portraitDict = new Dictionary<string, Sprite>();
        foreach (var p in portraits)
        {
            if (!portraitDict.ContainsKey(p.emotionName))
            {
                portraitDict.Add(p.emotionName, p.sprite);
            }
        }
    }

    // 提供给外部的方法：根据表情名拿图片
    public Sprite GetPortrait(string emotion)
    {
        // 如果字典没初始化（有时候Unity编辑模式下会发生），重新初始化
        if (portraitDict == null || portraitDict.Count == 0) OnEnable();

        if (portraitDict.ContainsKey(emotion))
        {
            return portraitDict[emotion];
        }
        return null; // 找不到就返回空
    }
}

[System.Serializable]
public struct PortraitData
{
    public string emotionName; // 例如 "happy", "sad", "normal"
    public Sprite sprite;      // 对应的图片文件
}