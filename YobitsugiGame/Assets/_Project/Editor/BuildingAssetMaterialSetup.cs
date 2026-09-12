#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The BTA House/Mall building kits ship as bare FBX + loose textures with no material setup.
/// This extracts each FBX's embedded materials to editable assets and wires up Albedo/Normal/
/// MetallicSmoothness maps by matching the material name against the pack's texture folder,
/// then flips fences/hedges/foliage-style materials to alpha-clipped based on actual pixel alpha.
/// </summary>
public static class BuildingAssetMaterialSetup
{
    private static readonly string[] AssetRoots =
    {
        "Assets/ThirdParty/BTA/HouseSet",
        "Assets/ThirdParty/BTA/MallSet",
    };

    [MenuItem("Yobitsugi/Assets/Setup Building Materials")]
    public static void Setup()
    {
        int configured = 0, skipped = 0;

        foreach (var root in AssetRoots)
        {
            string texturesDir = $"{root}/textures";
            if (!Directory.Exists(texturesDir))
            {
                Debug.LogWarning($"No textures folder at {texturesDir}, skipping {root}.");
                continue;
            }

            foreach (var fbxPath in Directory.GetFiles(root, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                if (importer == null) continue;
                if (importer.materialLocation != ModelImporterMaterialLocation.External)
                {
                    importer.materialLocation = ModelImporterMaterialLocation.External;
                    importer.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh();

            var textureFiles = Directory.GetFiles(texturesDir, "*.png").Select(p => p.Replace('\\', '/')).ToArray();
            var materialGuids = AssetDatabase.FindAssets("t:Material", new[] { root });

            foreach (var guid in materialGuids)
            {
                string matPath = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null) continue;

                if (ConfigureMaterial(mat, textureFiles)) configured++;
                else skipped++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Building materials: 設定 {configured} 件 / 一致するテクスチャなし {skipped} 件");
    }

    private static bool ConfigureMaterial(Material mat, string[] textureFiles)
    {
        string albedoPath = FindTexture(textureFiles, mat.name, "AlbedoTransparency", "Albedo");
        if (albedoPath == null) return false;

        string normalPath = FindTexture(textureFiles, mat.name, "Normal");
        string metallicPath = FindTexture(textureFiles, mat.name, "MetallicSmoothness");

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null && mat.shader != shader) mat.shader = shader;

        var albedoTex = LoadTexture(albedoPath, TextureImporterType.Default);
        if (albedoTex != null) mat.SetTexture("_BaseMap", albedoTex);

        if (normalPath != null)
        {
            var normalTex = LoadTexture(normalPath, TextureImporterType.NormalMap);
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        if (metallicPath != null)
        {
            var metallicTex = LoadTexture(metallicPath, TextureImporterType.Default);
            if (metallicTex != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallicTex);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetFloat("_Metallic", 1f);
            }
        }

        bool needsCutout = TextureHasRealAlpha(albedoPath);
        ConfigureSurface(mat, needsCutout);

        EditorUtility.SetDirty(mat);
        return true;
    }

    private static string FindTexture(string[] textureFiles, string materialName, params string[] suffixes)
    {
        string name = materialName.ToLowerInvariant();

        var candidates = textureFiles
            .Where(f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant().StartsWith(name))
            .ToList();

        foreach (var suffix in suffixes)
        {
            var match = candidates.FirstOrDefault(f =>
                Path.GetFileNameWithoutExtension(f).ToLowerInvariant().Contains(suffix.ToLowerInvariant()));
            if (match != null) return match;
        }

        return null;
    }

    private static Texture2D LoadTexture(string path, TextureImporterType type)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != type)
        {
            importer.textureType = type;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static bool TextureHasRealAlpha(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return false;

        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        bool hasAlpha = false;
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        try
        {
            var pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i += 7) // sample, full scan is unnecessary for a yes/no check
            {
                if (pixels[i].a < 250) { hasAlpha = true; break; }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not read pixels for {path}: {e.Message}");
        }

        if (!wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        return hasAlpha;
    }

    private static void ConfigureSurface(Material mat, bool cutout)
    {
        mat.SetFloat("_Surface", 0f); // both opaque and cutout render in the opaque queue path
        mat.SetInt("_ZWrite", 1);
        mat.SetInt("_SrcBlend", (int)BlendMode.One);
        mat.SetInt("_DstBlend", (int)BlendMode.Zero);
        mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        if (cutout)
        {
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            mat.SetInt("_Cull", (int)CullMode.Off); // thin foliage/fence geo reads better double-sided
        }
        else
        {
            mat.SetFloat("_AlphaClip", 0f);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = (int)RenderQueue.Geometry;
            mat.SetInt("_Cull", (int)CullMode.Back);
        }
    }
}
#endif
