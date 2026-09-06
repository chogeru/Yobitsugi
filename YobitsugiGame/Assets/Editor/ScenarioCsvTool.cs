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
    private const string CsvFolder = "Assets/Scenario";

    private static readonly string[] Header =
    {
        "index", "characterId", "speaker", "text", "backgroundId",
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

        var rows = ParseCsv(File.ReadAllText(path));
        if (rows.Count <= 1)
        {
            Debug.LogWarning("CSV に行がありません。");
            return;
        }

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
                setFlag = Field(row, 5),
                setFlagValue = ParseInt(Field(row, 6), 1),
                requiredFlag = Field(row, 7),
                forbiddenFlag = Field(row, 8),
            };

            var choices = new List<VNChoice>();
            AddChoice(choices, Field(row, 9), Field(row, 10), Field(row, 11));
            AddChoice(choices, Field(row, 12), Field(row, 13), Field(row, 14));
            if (choices.Count > 0) line.choices = choices.ToArray();

            lines.Add(line);
        }

        Undo.RecordObject(scene, "Import scenario CSV");
        scene.lines = lines.ToArray();
        EditorUtility.SetDirty(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"シナリオを取り込みました: {scene.SceneId} ← {Path.GetFileName(path)} ({lines.Count} 行)");
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
