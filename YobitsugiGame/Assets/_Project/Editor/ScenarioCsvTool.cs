#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Yobitsugi.VisualNovel;

/// <summary>
/// Round-trips scenario text between VN Scene assets and CSV, so writing can happen in a spreadsheet
/// while the game keeps reading the assets. Portrait direction stays in the inspector; this covers
/// speaker, text, background and branching, which is what a writer needs.
/// </summary>
public static class ScenarioCsvTool
{
    private const string CsvFolder = "Assets/_Project/Scenario";

    private static readonly string[] Header =
    {
        "index", "characterId", "speaker", "text", "backgroundId", "voiceId",
        "setFlag", "setFlagValue", "requiredFlag", "forbiddenFlag",
        "choice1Text", "choice1Next", "choice1Flag",
        "choice2Text", "choice2Next", "choice2Flag",
    };

    [MenuItem("Yobitsugi/Scenario/Export Selected VN Scene to CSV")]
    public static void Export()
    {
        var scene = Selection.activeObject as VNScene;
        if (scene == null)
        {
            Debug.LogWarning("Project ウィンドウで VN Scene アセットを選択してから実行してください。");
            return;
        }

        Directory.CreateDirectory(CsvFolder);
        string path = $"{CsvFolder}/{scene.SceneId}.csv";

        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", Header));

        for (int i = 0; i < (scene.lines?.Length ?? 0); i++)
        {
            var line = scene.lines[i];
            var choice1 = line.choices != null && line.choices.Length > 0 ? line.choices[0] : null;
            var choice2 = line.choices != null && line.choices.Length > 1 ? line.choices[1] : null;

            var fields = new List<string>
            {
                i.ToString(),
                line.character != null ? line.character.CharacterId : "",
                line.speaker ?? "",
                line.text ?? "",
                line.background != null ? line.background.name : "",
                line.voice != null ? line.voice.name : "",
                line.setFlag ?? "",
                line.setFlagValue.ToString(),
                line.requiredFlag ?? "",
                line.forbiddenFlag ?? "",
                choice1?.text ?? "",
                choice1 != null ? choice1.nextLineIndex.ToString() : "",
                choice1?.setFlag ?? "",
                choice2?.text ?? "",
                choice2 != null ? choice2.nextLineIndex.ToString() : "",
                choice2?.setFlag ?? "",
            };

            builder.AppendLine(string.Join(",", fields.ConvertAll(Escape)));
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
        AssetDatabase.Refresh();
        Debug.Log($"シナリオを書き出しました: {path} ({scene.lines?.Length ?? 0} 行)");
    }

    /// <summary>
    /// Imports every CSV in the scenario folder into the VN Scene of the same name, creating it when missing.
    /// The writer's copy in Assets/_Project/Scenario is the source, so a whole chapter can be revised in one pass.
    /// </summary>
    [MenuItem("Yobitsugi/Scenario/Import All Scenario CSV")]
    public static void ImportAll()
    {
        if (!Directory.Exists(CsvFolder))
        {
            Debug.LogWarning($"{CsvFolder} がありません。");
            return;
        }

        const string scenesFolder = "Assets/_Project/Resources/VNScenes";
        Directory.CreateDirectory(scenesFolder);

        int imported = 0, created = 0, lines = 0;
        foreach (var path in Directory.GetFiles(CsvFolder, "*.csv"))
        {
            string id = Path.GetFileNameWithoutExtension(path);
            string assetPath = $"{scenesFolder}/{id}.asset";

            var scene = AssetDatabase.LoadAssetAtPath<VNScene>(assetPath);
            if (scene == null)
            {
                scene = ScriptableObject.CreateInstance<VNScene>();
                AssetDatabase.CreateAsset(scene, assetPath);
                created++;
            }

            scene.lines = ReadLines(File.ReadAllText(path));
            lines += scene.lines.Length;
            EditorUtility.SetDirty(scene);
            imported++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"シナリオ取り込み: {imported} シーン ({created} 件は新規) / 合計 {lines} 行");
    }

    [MenuItem("Yobitsugi/Scenario/Import CSV into Selected VN Scene")]
    public static void Import()
    {
        var scene = Selection.activeObject as VNScene;
        if (scene == null)
        {
            Debug.LogWarning("Project ウィンドウで取り込み先の VN Scene アセットを選択してから実行してください。");
            return;
        }

        string path = EditorUtility.OpenFilePanel("シナリオCSVを選択", CsvFolder, "csv");
        if (string.IsNullOrEmpty(path)) return;

        var lines = ReadLines(File.ReadAllText(path));
        if (lines.Length == 0)
        {
            Debug.LogWarning("CSV に行がありません。");
            return;
        }

        Undo.RecordObject(scene, "Import scenario CSV");
        scene.lines = lines;
        EditorUtility.SetDirty(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"シナリオを取り込みました: {scene.SceneId} ← {Path.GetFileName(path)} ({lines.Length} 行)");
    }

    private static VNLine[] ReadLines(string csv)
    {
        var rows = ParseCsv(csv);
        var lines = new List<VNLine>();

        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (row.Count == 0 || string.IsNullOrWhiteSpace(string.Join("", row))) continue;

            var line = new VNLine
            {
                character = VNAssetLibrary.FindCharacter(Field(row, 1)),
                speaker = Field(row, 2),
                text = Field(row, 3),
                background = VNAssetLibrary.FindBackground(Field(row, 4)),
                voice = VNAssetLibrary.FindVoice(Field(row, 5)),
                setFlag = Field(row, 6),
                setFlagValue = ParseInt(Field(row, 7), 1),
                requiredFlag = Field(row, 8),
                forbiddenFlag = Field(row, 9),
            };

            var choices = new List<VNChoice>();
            AddChoice(choices, Field(row, 10), Field(row, 11), Field(row, 12));
            AddChoice(choices, Field(row, 13), Field(row, 14), Field(row, 15));
            if (choices.Count > 0) line.choices = choices.ToArray();

            lines.Add(line);
        }

        return lines.ToArray();
    }

    private static void AddChoice(List<VNChoice> choices, string text, string next, string flag)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        choices.Add(new VNChoice
        {
            text = text,
            nextLineIndex = ParseInt(next, -1),
            setFlag = flag,
            setFlagValue = 1,
        });
    }

    private static string Field(List<string> row, int index) => index < row.Count ? row[index] : "";

    private static int ParseInt(string value, int fallback) => int.TryParse(value, out int result) ? result : fallback;

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>Minimal RFC4180 reader: handles quoted fields, escaped quotes and newlines inside text.</summary>
    private static List<List<string>> ParseCsv(string content)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(c);
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = new List<string>();
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        // Strip a UTF-8 BOM from the first cell if the file came from a spreadsheet.
        if (rows.Count > 0 && rows[0].Count > 0)
            rows[0][0] = rows[0][0].TrimStart('﻿');

        return rows;
    }
}
#endif
