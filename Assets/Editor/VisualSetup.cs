using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Visual-style assets the runtime cannot create itself (Shader.Find is unreliable in builds):
/// template materials for the project's own shaders under Assets/Resources/Materials, and the URP
/// asset's shadow / MSAA settings. Idempotent. Called by ProjectSetup.Run and BuildScript.BuildWebGL;
/// run alone with
///   Unity.exe -batchmode -quit -projectPath . -executeMethod VisualSetup.Run
/// </summary>
public static class VisualSetup
{
    const string MaterialsDir = "Assets/Resources/Materials";
    const string UrpAssetPath = "Assets/Settings/URP-Asset.asset";

    public static void Run()
    {
        EnsureFolder();
        CreateMaterial("Toon", "ClockingOut/Toon", null);
        CreateMaterial("ToonTransparent", "ClockingOut/ToonTransparent", null);
        CreateMaterial("ParticleAdd", "ClockingOut/ParticleAdd", null);
        CreateMaterial("Sky", "ClockingOut/Sky", null);
        ConfigureUrp();
        AssetDatabase.SaveAssets();
        Debug.Log("[VisualSetup] Done.");
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(MaterialsDir)) AssetDatabase.CreateFolder("Assets/Resources", "Materials");
    }

    static void CreateMaterial(string name, string shaderName, System.Action<Material> configure)
    {
        string path = MaterialsDir + "/" + name + ".mat";
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogError("[VisualSetup] Shader not found: " + shaderName);
            return;
        }
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            if (existing.shader != shader) existing.shader = shader;
            if (configure != null) configure(existing);
            EditorUtility.SetDirty(existing);
            return;
        }
        var mat = new Material(shader) { name = name };
        if (configure != null) configure(mat);
        AssetDatabase.CreateAsset(mat, path);
        Debug.Log("[VisualSetup] Created " + path);
    }

    /// <summary>Small dioramas: one cascade, 2048 map, soft edges, 2x MSAA (outlines need some AA).</summary>
    static void ConfigureUrp()
    {
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
        if (asset == null) { Debug.LogWarning("[VisualSetup] " + UrpAssetPath + " not found."); return; }
        var so = new SerializedObject(asset);
        Set(so, "m_SoftShadowsSupported", 1);
        Set(so, "m_MainLightShadowsSupported", 1);
        Set(so, "m_MainLightShadowmapResolution", 2048);
        Set(so, "m_ShadowCascadeCount", 1);
        Set(so, "m_MSAA", 2);
        Set(so, "m_SupportsHDR", 0);
        Set(so, "m_AdditionalLightsPerObjectLimit", 4);
        SetF(so, "m_ShadowDistance", 40f);
        SetF(so, "m_ShadowDepthBias", 0.6f);
        SetF(so, "m_ShadowNormalBias", 0.5f);
        // Keep the soft-shadow keyword variants (prefiltering would strip them).
        Set(so, "m_PrefilterSoftShadows", 0);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    static void Set(SerializedObject so, string prop, int v)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning("[VisualSetup] URP property missing: " + prop); return; }
        if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v != 0;
        else p.intValue = v;
    }

    static void SetF(SerializedObject so, string prop, float v)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning("[VisualSetup] URP property missing: " + prop); return; }
        p.floatValue = v;
    }
}
