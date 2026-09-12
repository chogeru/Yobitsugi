#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The TsubokuLab street/bicycle packs ship with Built-in Standard shader materials, which render magenta
/// under this project's URP. Standard and URP/Lit share most property names, so this just swaps the shader
/// and remaps the handful of renamed properties (_MainTex/_Color/_Glossiness) instead of rebuilding materials.
/// </summary>
public static class StandardToUrpMaterialConverter
{
    private const string AssetRoot = "Assets/ThirdParty/TsubokuLab";

    [MenuItem("Yobitsugi/Assets/Convert TsubokuLab Materials to URP")]
    public static void Convert()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("Universal Render Pipeline/Lit shader not found.");
            return;
        }

        int converted = 0, skipped = 0;
        var guids = AssetDatabase.FindAssets("t:Material", new[] { AssetRoot });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;
            string shaderName = mat.shader.name;
            // Both Built-in Standard and the "Autodesk Interactive" shader FBX importers sometimes assign
            // expose the same Standard-compatible property set (_MainTex/_Glossiness/_Mode/...).
            if (!shaderName.Contains("Standard") && shaderName != "Autodesk Interactive") { skipped++; continue; }

            ConvertMaterial(mat);
            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"TsubokuLab materials: URP変換 {converted} 件 / 対象外 {skipped} 件");
    }

    private static void ConvertMaterial(Material mat)
    {
        // Standard's specular-setup fileID (47) sets _SpecGlossMap/_SpecColor; the metallic-setup (46) sets
        // _MetallicGlossMap/_Metallic. Both names already match URP/Lit, so only read what decides workflow.
        bool isSpecularWorkflow = mat.HasProperty("_SpecGlossMap") &&
            (mat.GetTexture("_SpecGlossMap") != null || mat.GetColor("_SpecColor") != Color.black);

        var mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
        var mainTexScale = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex") : Vector2.one;
        var mainTexOffset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero;
        var baseColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
        float glossiness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
        float mode = mat.HasProperty("_Mode") ? mat.GetFloat("_Mode") : 0f;
        float cutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f;
        var emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;

        mat.shader = urpLitCache ?? (urpLitCache = Shader.Find("Universal Render Pipeline/Lit"));

        if (mainTex != null)
        {
            mat.SetTexture("_BaseMap", mainTex);
            mat.SetTextureScale("_BaseMap", mainTexScale);
            mat.SetTextureOffset("_BaseMap", mainTexOffset);
        }
        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Smoothness", glossiness);
        mat.SetFloat("_WorkflowMode", isSpecularWorkflow ? 0f : 1f); // URP/Lit: 0=Specular, 1=Metallic
        mat.SetFloat("_Cutoff", cutoff);

        if (mat.GetTexture("_BumpMap") != null) mat.EnableKeyword("_NORMALMAP");
        if (isSpecularWorkflow && mat.GetTexture("_SpecGlossMap") != null) mat.EnableKeyword("_SPECGLOSSMAP");
        else if (!isSpecularWorkflow && mat.GetTexture("_MetallicGlossMap") != null) mat.EnableKeyword("_METALLICSPECGLOSSMAP");

        bool hasEmission = emissionColor != Color.black || mat.GetTexture("_EmissionMap") != null;
        if (hasEmission)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        // Standard _Mode: 0=Opaque, 1=Cutout, 2=Fade, 3=Transparent
        if (mode >= 2f) ConfigureTransparent(mat);
        else if (mode >= 1f) ConfigureCutout(mat);
        else ConfigureOpaque(mat);

        EditorUtility.SetDirty(mat);
    }

    private static Shader urpLitCache;

    private static void ConfigureOpaque(Material mat)
    {
        mat.SetFloat("_Surface", 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Opaque");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        mat.SetInt("_ZWrite", 1);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
    }

    private static void ConfigureCutout(Material mat)
    {
        ConfigureOpaque(mat);
        mat.SetFloat("_AlphaClip", 1f);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.SetOverrideTag("RenderType", "TransparentCutout");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
    }

    private static void ConfigureTransparent(Material mat)
    {
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
    }
}
#endif
