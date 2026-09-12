#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Walks every component in the open scene and reports serialized object references that are still empty,
/// so a rebuilt or hand-edited scene never ships with a silently unwired dependency.
/// Fields whose names appear in <see cref="OptionalFields"/> are allowed to be empty.
/// </summary>
public static class SceneReferenceValidator
{
    /// <summary>References that are legitimately empty until content exists.</summary>
    private static readonly HashSet<string> OptionalFields = new HashSet<string>
    {
        "vnMusic", "explorationAmbience", "lineAdvanceClip", "clueClip", "saveClip",
        "introScene", "background", "vignettingMask", "lensDirtTexture",
    };

    [MenuItem("Yobitsugi/Validate Scene References")]
    public static void Validate()
    {
        var problems = new StringBuilder();
        int checkedComponents = 0, missingScripts = 0, emptyRefs = 0;

        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    missingScripts++;
                    continue;
                }

                // Only project scripts: engine components manage their own references.
                var type = component.GetType();
                if (type.Namespace == null || !type.Namespace.StartsWith("Yobitsugi")) continue;

                checkedComponents++;

                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                bool enterChildren = true;

                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (property.objectReferenceValue != null) continue;
                    if (OptionalFields.Contains(property.name)) continue;

                    emptyRefs++;
                    problems.AppendLine($"[未設定] {GetPath(component.transform)} / {type.Name}.{property.displayName}");
                }
            }
        }

        var summary = $"参照チェック: コンポーネント {checkedComponents} 件 / 未設定 {emptyRefs} 件 / スクリプト欠損 {missingScripts} 件";

        if (emptyRefs == 0 && missingScripts == 0)
            Debug.Log(summary + " — 問題ありません。");
        else
            Debug.LogWarning($"{summary}\n{problems}");
    }

    private static string GetPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
#endif
