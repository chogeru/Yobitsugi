#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Yobitsugi.VisualNovel;

/// <summary>Authoring safety net: catches broken choice targets, empty lines and duplicate scene ids across every VN Scene asset.</summary>
public static class VNSceneValidator
{
    [MenuItem("Yobitsugi/Validate VN Scenes")]
    public static void Validate()
    {
        var guids = AssetDatabase.FindAssets($"t:{nameof(VNScene)}");
        var idOwners = new Dictionary<string, string>();
        var problems = new StringBuilder();
        int sceneCount = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = AssetDatabase.LoadAssetAtPath<VNScene>(path);
            if (scene == null) continue;

            sceneCount++;

            if (idOwners.TryGetValue(scene.SceneId, out string otherPath))
                problems.AppendLine($"[重複ID] '{scene.SceneId}' が {path} と {otherPath} で重複しています。");
            else
                idOwners[scene.SceneId] = path;

            if (!path.Contains("/Resources/"))
                problems.AppendLine($"[Resources外] {path} は Resources フォルダ外のため、セーブデータからの復元ができません。");

            if (scene.lines == null || scene.lines.Length == 0)
            {
                problems.AppendLine($"[空シーン] {path} に行がありません。");
                continue;
            }

            for (int i = 0; i < scene.lines.Length; i++)
            {
                var line = scene.lines[i];

                if (string.IsNullOrWhiteSpace(line.text))
                    problems.AppendLine($"[空テキスト] {scene.SceneId} の {i} 行目にテキストがありません。");

                if (line.choices == null) continue;

                for (int c = 0; c < line.choices.Length; c++)
                {
                    var choice = line.choices[c];

                    if (string.IsNullOrWhiteSpace(choice.text))
                        problems.AppendLine($"[空選択肢] {scene.SceneId} の {i} 行目・選択肢{c} にラベルがありません。");

                    if (choice.nextLineIndex >= scene.lines.Length)
                        problems.AppendLine($"[範囲外ジャンプ] {scene.SceneId} の {i} 行目・選択肢{c} が存在しない行 {choice.nextLineIndex} を指しています (最大 {scene.lines.Length - 1})。");
                }
            }
        }

        if (problems.Length == 0)
            Debug.Log($"VN Scene 検証: {sceneCount} 件すべて問題ありません。");
        else
            Debug.LogWarning($"VN Scene 検証: {sceneCount} 件中に問題が見つかりました。\n{problems}");
    }
}
#endif
