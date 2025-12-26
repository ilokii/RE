using UnityEngine;
using System.Collections.Generic;

public class CharacterManager : MonoBehaviour
{
    // 单例模式：让别的脚本能直接 CharacterManager.Instance 访问
    public static CharacterManager Instance;

    // 内存中缓存所有角色档案： Key = ID (hero), Value = Profile
    private Dictionary<string, CharacterProfile> characterDatabase;

    void Awake()
    {
        Instance = this;
        LoadAllCharacters();
    }

    void LoadAllCharacters()
    {
        characterDatabase = new Dictionary<string, CharacterProfile>();

        // 自动读取 Resources/Characters 文件夹下所有的档案
        var profiles = Resources.LoadAll<CharacterProfile>("Characters");

        foreach (var p in profiles)
        {
            if (!characterDatabase.ContainsKey(p.characterId))
            {
                characterDatabase.Add(p.characterId, p);
            }
            else
            {
                Debug.LogWarning($"重复的角色ID: {p.characterId}");
            }
        }
        Debug.Log($"角色管理器初始化完毕，共加载 {characterDatabase.Count} 名角色。");
    }

    // 核心功能：根据ID获取档案
    public CharacterProfile GetCharacter(string id)
    {
        if (characterDatabase.ContainsKey(id))
        {
            return characterDatabase[id];
        }
        return null;
    }
}