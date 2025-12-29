using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PortraitManager : MonoBehaviour
{
    public static PortraitManager Instance;

    [Header("配置")]
    public Transform portraitLayer;     // 对应场景里的 PortraitLayer
    public RectTransform[] positionAnchors; // 对应 Pos_0 到 Pos_4
    public GameObject portraitPrefab;   // 立绘预制体 (下面会让你做)

    // 缓存当前屏幕上的角色：Key=CharID, Value=立绘物体
    private Dictionary<string, GameObject> activeCharacters = new Dictionary<string, GameObject>();
    
    // 【新增】记录每个角色当前所在的位置索引
    private Dictionary<string, int> characterPositions = new Dictionary<string, int>();

    private float focusScale;
    private float defaultScale;

    void Awake()
    {
        Instance = this;
        getPortraitPrefabDefaultScale();
    }

    // --- 核心功能 1: 更新立绘状态 ---
    public void UpdatePortrait(string charId, string expression, int targetPosIndex, bool isImmediate = false)
    {
        // 1. 先确定目标坐标 (targetCoord)
        // 默认放到中间 (Pos_2)，防止后面报错
        Vector2 targetCoord = positionAnchors[2].anchoredPosition;
        bool keepCurrentPos = (targetPosIndex == -1); // 标记：是否保持原位
        
        // 【新增】记录实际使用的位置索引
        int actualPosIndex = 2; // 默认中间位置

        // 如果传入了有效的座位号 (0~4)，就用座位的坐标
        if (targetPosIndex >= 0 && targetPosIndex < positionAnchors.Length)
        {
            targetCoord = positionAnchors[targetPosIndex].anchoredPosition;
            actualPosIndex = targetPosIndex;
        }

        GameObject charObj = null;

        // 2. 检查角色是否已经在场上
        if (activeCharacters.ContainsKey(charId))
        {
            // --- 旧角色逻辑 ---
            charObj = activeCharacters[charId];
            RectTransform rt = charObj.GetComponent<RectTransform>();

            // 如果 CSV 里没填位置 (targetPosIndex == -1)，则目标位置等于当前位置
            if (keepCurrentPos)
            {
                targetCoord = rt.anchoredPosition;
                // 保持原有位置索引
                if (characterPositions.ContainsKey(charId))
                {
                    actualPosIndex = characterPositions[charId];
                }
            }
            else
            {
                // 【新增】更新位置记录
                characterPositions[charId] = actualPosIndex;
            }

            // 执行移动
            if (rt.anchoredPosition != targetCoord)
            {
                StopAllCoroutines(); // 简单防冲突
                if (isImmediate)
                    rt.anchoredPosition = targetCoord;
                else
                    StartCoroutine(MoveTo(rt, targetCoord, 0.5f));
            }
        }
        else
        {
            // --- 新角色进场逻辑 ---
            // 如果 CSV 没填位置，默认给中间 (Pos 2) 的坐标，上面已经默认赋值了
            
            charObj = Instantiate(portraitPrefab, portraitLayer);
            charObj.name = charId;
            
            RectTransform rt = charObj.GetComponent<RectTransform>();
            rt.anchoredPosition = targetCoord; // 这里现在能访问到 targetCoord 了！
            //rt.localScale = Vector3.one;

            activeCharacters.Add(charId, charObj);
            // 【新增】记录新角色的位置
            characterPositions[charId] = actualPosIndex;
        }

        // 3. 切换表情
        Image img = charObj.GetComponent<Image>();
        CharacterProfile profile = CharacterManager.Instance.GetCharacter(charId);
        if (profile != null)
        {
            Sprite face = profile.GetPortrait(expression);
            if (face != null) img.sprite = face;
            img.SetNativeSize();
        }

        // 提到最上层
        charObj.transform.SetAsLastSibling();
    }

    // --- 核心功能 2: 隐藏角色 ---
    public void HideCharacter(string charId)
    {
        if (activeCharacters.ContainsKey(charId))
        {
            GameObject obj = activeCharacters[charId];
            activeCharacters.Remove(charId);
            characterPositions.Remove(charId); // 【新增】同时移除位置记录
            Destroy(obj); // 简单粗暴：直接销毁 (进阶可以做淡出)
        }
    }

    // --- 核心功能 3: 聚焦/缩放 (CMD_FOCUS) - 通过角色ID控制 ---
    public void SetFocusByCharacter(string charId, bool isFocus)
    {
        // 检查角色是否存在
        if (!activeCharacters.ContainsKey(charId))
        {
            Debug.LogWarning($"[PortraitManager] SetFocusByCharacter: 角色 '{charId}' 不在场上");
            return;
        }

        GameObject charObj = activeCharacters[charId];
        RectTransform rt = charObj.GetComponent<RectTransform>();
        
        // 停止该物体上可能正在运行的缩放协程
        MonoBehaviour[] components = charObj.GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            if (comp != null) comp.StopAllCoroutines();
        }
        
        if (isFocus)
        {
            StartCoroutine(ScaleTo(rt, focusScale, 0.3f)); // 0.3秒缩放
            Debug.Log($"[PortraitManager] 聚焦角色 {charId}");
        }
        else
        {
            StartCoroutine(ScaleTo(rt, defaultScale, 0.3f)); // 0.3秒缩放
            Debug.Log($"[PortraitManager] 取消聚焦角色 {charId}");
        }
    }
    
    // --- 【保留】通过位置索引聚焦 (向后兼容) ---
    public void SetFocus(int posIndex, bool isFocus)
    {
        // 【修复】使用位置记录字典来精确匹配，而不是坐标比较
        if (posIndex < 0 || posIndex >= positionAnchors.Length)
        {
            Debug.LogWarning($"[PortraitManager] SetFocus: 无效的位置索引 {posIndex}");
            return;
        }

        foreach (var kvp in characterPositions)
        {
            string charId = kvp.Key;
            int charPosIndex = kvp.Value;
            
            // 如果这个角色在目标位置上
            if (charPosIndex == posIndex && activeCharacters.ContainsKey(charId))
            {
                GameObject charObj = activeCharacters[charId];
                RectTransform rt = charObj.GetComponent<RectTransform>();
                
                // 停止该物体上可能正在运行的缩放协程
                MonoBehaviour[] components = charObj.GetComponents<MonoBehaviour>();
                foreach (var comp in components)
                {
                    if (comp != null) comp.StopAllCoroutines();
                }
                
                if (isFocus)
                {
                    StartCoroutine(ScaleTo(rt, focusScale, 0.3f)); // 0.3秒缩放
                    Debug.Log($"[PortraitManager] 聚焦角色 {charId} 在位置 {posIndex}");
                }
                else
                {
                    StartCoroutine(ScaleTo(rt, defaultScale, 0.3f)); // 0.3秒缩放
                    Debug.Log($"[PortraitManager] 取消聚焦角色 {charId} 在位置 {posIndex}");
                }
            }
        }
    }

    // --- 核心功能 4: 隐藏所有人 (清场) ---
    public void HideAllCharacters()
    {
        // 遍历所有在场角色的 Key
        List<string> allKeys = new List<string>(activeCharacters.Keys);
        
        foreach (var key in allKeys)
        {
            HideCharacter(key);
        }
        // activeCharacters 会在 HideCharacter 里被移除，所以这里不用 clear
    }
    
    // --- 辅助协程：平滑移动 ---
    IEnumerator MoveTo(RectTransform target, Vector2 endPos, float duration)
    {
        Vector2 startPos = target.anchoredPosition;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 使用 SmoothStep 让移动起步和结束更柔和
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            target.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        target.anchoredPosition = endPos;
    }

    // --- 辅助协程：平滑缩放 ---
    IEnumerator ScaleTo(RectTransform target, float endScale, float duration)
    {
        Vector3 startScale = target.localScale;
        Vector3 endScaleVec = Vector3.one * endScale;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            target.localScale = Vector3.Lerp(startScale, endScaleVec, t);
            yield return null;
        }
        target.localScale = endScaleVec;
    }
    
    // --- 辅助功能：获取预制体初始缩放尺寸 ---
    void getPortraitPrefabDefaultScale()
    {
        defaultScale = portraitPrefab.transform.localScale.x;
        focusScale = defaultScale * 1.05f;
    }

    // 辅助工具：停止某个物体上正在跑的专用协程 (防止移动和缩放冲突)
    // 这里简化处理，直接用 StopAllCoroutines 可能会误伤，但在当前架构下问题不大
    void StopAllCoroutinesOn(GameObject obj)
    {
        // 由于协程是跑在 Manager 上的，这里其实比较难精准停止针对某个 Obj 的协程
        // 严谨的做法是把 MoveTo/ScaleTo 写在立绘物体自己的脚本上
        // 但为了 MVP，我们这里依靠覆盖逻辑：每次启动新协程前，其实不需要强制停止旧的，
        // 只要逻辑写得好（Lerp 更新当前位置），直接覆盖也是一种策略。
        // *更好的做法*：每个立绘挂一个独立脚本 PortraitController。
        // 既然你在这个阶段想要简单，我们暂时不做复杂的 Coroutine 句柄管理。
    }
}