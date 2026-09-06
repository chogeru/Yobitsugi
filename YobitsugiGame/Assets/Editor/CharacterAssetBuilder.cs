#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yobitsugi.VisualNovel;

/// <summary>
/// Turns a folder of portrait art into CharacterDefinition assets.
/// Drop art as Assets/Art/Characters/&lt;CharacterName&gt;/&lt;expression&gt;.png and run the menu item:
/// each folder becomes one character whose expression keys are the file names.
/// Existing definitions are updated in place, so hand-tuned scale/offset/colour survive.
/// </summary>
public static class CharacterAssetBuilder
{
    private const string ArtRoot = "Assets/Art/Characters";
    private const string OutputFolder = "Assets/Resources/Characters";

    [MenuItem("Yobitsugi/Build Character Definitions from Art")]
    public static void Build()
    {
        if (!Directory.Exists(ArtRoot))
        {
            Directory.CreateDirectory(ArtRoot);
            AssetDatabase.Refresh();
            Debug.LogWarning($"立ち絵フォルダを作成しました: {ArtRoot}\n" +
                             $"{ArtRoot}/<キャラ名>/<表情>.png の形で画像を置いてから、もう一度実行してください。");
            return;
        }

        Directory.CreateDirectory(OutputFolder);

        var characterDirs = Directory.GetDirectories(ArtRoot);
        if (characterDirs.Length == 0)
        {
            Debug.LogWarning($"{ArtRoot} にキャラクターフォルダがありません。");
            return;
        }

        int created = 0, updated = 0;

        foreach (var dir in characterDirs)
        {
            string characterName = Path.GetFileName(dir);
            var sprites = CollectSprites(dir);
            if (sprites.Count == 0)
            {
                Debug.LogWarning($"[{characterName}] にスプライトが見つかりませんでした。");
                continue;
            }

            string assetPath = $"{OutputFolder}/{characterName}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(assetPath);
            bool isNew = definition == null;

            if (isNew)
            {
                definition = ScriptableObject.CreateInstance<CharacterDefinition>();
                definition.displayName = characterName;
                AssetDatabase.CreateAsset(definition, assetPath);
                created++;
            }
            else
            {
                updated++;
            }

            definition.expressions = sprites
                .OrderBy(pair => pair.Key)
                .Select(pair => new CharacterExpression { key = pair.Key, sprite = pair.Value })
                .ToArray();

            EditorUtility.SetDirty(definition);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        VNAssetLibrary.Invalidate();

        Debug.Log($"キャラクター定義: 新規 {created} 件 / 更新 {updated} 件 → {OutputFolder}");
    }

    private static Dictionary<string, Sprite> CollectSprites(string directory)
    {
        var result = new Dictionary<string, Sprite>();

        foreach (var file in Directory.GetFiles(directory))
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".psd" && extension != ".tga") continue;

            string path = file.Replace('\\', '/');
            EnsureSpriteImport(path);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) result[Path.GetFileNameWithoutExtension(file)] = sprite;
        }

        return result;
    }

    /// <summary>Portrait art is useless to the VN layer unless it is imported as a Sprite, so fix that up front.</summary>
    private static void EnsureSpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.Sprite) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
    }
}
#endif
