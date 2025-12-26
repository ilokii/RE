using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System; // 用于 Enum 解析

public static class CSVLoader
{
    // 设定 CSV 文件存放的根目录 (相对于 Resources)
    private const string ROOT_PATH = "Dialogs/";

    // 主方法：输入文件名，返回处理好的列表
    public static List<DialogueLine> Load(string fileName)
    {
        List<DialogueLine> lines = new List<DialogueLine>();

        // 1. 加载资源 (支持子文件夹，如 "Chapter1/Scene1")
        string fullPath = ROOT_PATH + fileName;
        TextAsset csvData = Resources.Load<TextAsset>(fullPath);

        if (csvData == null)
        {
            Debug.LogError($"[CSVLoader] 找不到剧本文件: Resources/{fullPath}");
            return lines;
        }

        // 2. 按行分割 (兼容 Windows \r\n 和 Mac/Linux \n)
        string[] rows = csvData.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // 3. 遍历每一行 (从 i=1 开始，跳过第一行表头)
        for (int i = 1; i < rows.Length; i++)
        {
            string row = rows[i];
            
            // 跳过空行或注释行 (以 // 开头的行)
            if (string.IsNullOrWhiteSpace(row) || row.StartsWith("//")) continue;

            // 解析当前行
            DialogueLine parsedLine = ParseRow(row, i);
            if (parsedLine != null)
            {
                lines.Add(parsedLine);
            }
        }

        Debug.Log($"[CSVLoader] 成功加载 {fileName}，共 {lines.Count} 行数据。");
        return lines;
    }

    // 解析单行逻辑
    private static DialogueLine ParseRow(string row, int rowIndex)
    {
        string[] cells = Regex.Split(row, ",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

        // 【修改】现在我们需要至少 6 列数据
        if (cells.Length < 6)
        {
            Debug.LogWarning($"[CSVLoader] 第 {rowIndex + 1} 行格式错误，列数不足: {row}");
            return null;
        }

        try
        {
            DialogueLine line = new DialogueLine();

            line.id = int.Parse(cells[0].Trim());

            // Type 解析
            string typeStr = cells[1].Trim().ToUpper();
            if (Enum.TryParse(typeStr, out DialogueType parsedType))
                line.type = parsedType;
            else
                line.type = DialogueType.DIALOG;

            line.charId = cells[2].Trim();
            line.expression = cells[3].Trim();
            
            // 【新增】解析 Position (第 5 列, 索引 4)
            line.position = cells[4].Trim();

            // 【移位】Speed 变成 (第 6 列, 索引 5)
            // 防止数组越界：检查 cells 长度
            line.speed = cells.Length > 5 ? cells[5].Trim() : "";

            // 【移位】Content 变成 (第 7 列, 索引 6)
            string contentRaw = cells.Length > 6 ? cells[6] : "";
            line.content = contentRaw.Replace("\"\"", "\"").Trim('"');

            return line;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CSVLoader] 解析第 {rowIndex + 1} 行错误: {e.Message}");
            return null;
        }
    }
}