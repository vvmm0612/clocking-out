using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot (idempotent) project configuration. Run in batchmode:
///   Unity.exe -batchmode -quit -projectPath . -executeMethod ProjectSetup.Run -logFile Logs/setup.log
/// </summary>
public static class ProjectSetup
{
    const string SettingsDir = "Assets/Settings";
    const string UrpAssetPath = SettingsDir + "/URP-Asset.asset";
    const string UrpRendererPath = SettingsDir + "/URP-Renderer.asset";
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string TmpEssentialsPackage = "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage";

    public static void Run()
    {
        ConfigurePlayerAndEditor();
        var urp = CreateOrLoadUrpAsset();
        AssignRenderPipeline(urp);
        ImportTmpEssentials();
        CreateRuntimeMaterials();
        VisualSetup.Run();
        CreateMainScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ProjectSetup] Done.");
    }

    static void ConfigurePlayerAndEditor()
    {
        PlayerSettings.productName = "Clocking Out";
        PlayerSettings.companyName = "GameOff2026";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        EditorSettings.serializationMode = SerializationMode.ForceText;
        VersionControlSettings.mode = "Visible Meta Files";
        Debug.Log("[ProjectSetup] Player/Editor settings applied (Linear, Force Text, Visible Meta Files).");
    }

    static UniversalRenderPipelineAsset CreateOrLoadUrpAsset()
    {
        if (!AssetDatabase.IsValidFolder(SettingsDir))
            AssetDatabase.CreateFolder("Assets", "Settings");

        var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
        if (existing != null)
        {
            Debug.Log("[ProjectSetup] URP asset already exists.");
            return existing;
        }

        // Mirrors URP's internal CreateRendererAsset (menu: Create > Rendering > URP Asset).
        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
            UniversalRenderPipelineAsset.packagePath + "/Runtime/Data/PostProcessData.asset");
        AssetDatabase.CreateAsset(renderer, UrpRendererPath);
        ResourceReloader.ReloadAllNullIn(renderer, UniversalRenderPipelineAsset.packagePath);
        EditorUtility.SetDirty(renderer);

        var asset = UniversalRenderPipelineAsset.Create(renderer);
        AssetDatabase.CreateAsset(asset, UrpAssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[ProjectSetup] Created " + UrpAssetPath + " and " + UrpRendererPath);
        return asset;
    }

    static void AssignRenderPipeline(RenderPipelineAsset asset)
    {
        GraphicsSettings.defaultRenderPipeline = asset;
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = asset;
        }
        QualitySettings.SetQualityLevel(current, false);
        Debug.Log("[ProjectSetup] URP assigned to Graphics + " + QualitySettings.names.Length + " quality levels.");
    }

    static void ImportTmpEssentials()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
        {
            Debug.Log("[ProjectSetup] TMP Essential Resources already present.");
            return;
        }
        if (!File.Exists(Path.GetFullPath(TmpEssentialsPackage)))
        {
            Debug.LogWarning("[ProjectSetup] TMP essentials package not found: " + TmpEssentialsPackage);
            return;
        }
        // AssetDatabase.ImportPackage is asynchronous and never finishes under "-batchmode -quit",
        // so unpack the .unitypackage (a gzipped tar of <guid>/asset, asset.meta, pathname) ourselves.
        int count = ExtractUnityPackage(Path.GetFullPath(TmpEssentialsPackage));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[ProjectSetup] Imported TMP Essential Resources (" + count + " assets).");
    }

    static int ExtractUnityPackage(string packagePath)
    {
        var entries = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, byte[]>>();
        using (var fs = File.OpenRead(packagePath))
        using (var gz = new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionMode.Decompress))
        {
            var header = new byte[512];
            string longName = null;
            while (ReadFully(gz, header, 512))
            {
                if (header.All(b => b == 0)) break;
                string name = System.Text.Encoding.UTF8.GetString(header, 0, 100).TrimEnd('\0');
                string prefix = System.Text.Encoding.UTF8.GetString(header, 345, 155).TrimEnd('\0');
                if (prefix.Length > 0) name = prefix + "/" + name;
                string sizeText = System.Text.Encoding.ASCII.GetString(header, 124, 12).Trim('\0', ' ');
                long size = sizeText.Length == 0 ? 0 : System.Convert.ToInt64(sizeText, 8);
                char type = (char)header[156];
                var data = new byte[size];
                if (size > 0 && !ReadFully(gz, data, (int)size)) break;
                long pad = (512 - size % 512) % 512;
                if (pad > 0) ReadFully(gz, new byte[pad], (int)pad);

                if (type == 'L') { longName = System.Text.Encoding.UTF8.GetString(data).TrimEnd('\0'); continue; }
                if (longName != null) { name = longName; longName = null; }
                if (type != '0' && type != '\0') continue;

                name = name.Replace('\\', '/');
                if (name.StartsWith("./")) name = name.Substring(2);
                int slash = name.IndexOf('/');
                if (slash <= 0) continue;
                string guid = name.Substring(0, slash);
                string part = name.Substring(slash + 1);
                if (!entries.ContainsKey(guid)) entries[guid] = new System.Collections.Generic.Dictionary<string, byte[]>();
                entries[guid][part] = data;
            }
        }

        int written = 0;
        foreach (var kv in entries)
        {
            byte[] pathBytes;
            if (!kv.Value.TryGetValue("pathname", out pathBytes)) continue;
            string assetPath = System.Text.Encoding.UTF8.GetString(pathBytes).Split('\n')[0].Trim();
            if (!assetPath.StartsWith("Assets/")) continue;
            string full = Path.GetFullPath(assetPath);
            byte[] asset;
            if (kv.Value.TryGetValue("asset", out asset))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, asset);
                written++;
            }
            else
            {
                Directory.CreateDirectory(full);
            }
            byte[] meta;
            if (kv.Value.TryGetValue("asset.meta", out meta)) File.WriteAllBytes(full + ".meta", meta);
        }
        return written;
    }

    static bool ReadFully(Stream s, byte[] buffer, int count)
    {
        int read = 0;
        while (read < count)
        {
            int n = s.Read(buffer, read, count - read);
            if (n <= 0) return false;
            read += n;
        }
        return true;
    }

    const string MaterialsDir = "Assets/Resources/Materials";

    /// <summary>
    /// Template materials loaded by DeathNotice.Game.Mats via Resources.Load. Shipping them as assets
    /// under Resources guarantees their shaders are included in the build (Shader.Find would return
    /// null for stripped shaders at runtime).
    /// </summary>
    static void CreateRuntimeMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(MaterialsDir))
            AssetDatabase.CreateFolder("Assets/Resources", "Materials");

        // The URP Lit / Unlit templates were replaced by the project's toon shaders (VisualSetup).
        foreach (string old in new[] { "Lit", "Unlit" })
        {
            string oldPath = MaterialsDir + "/" + old + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(oldPath) != null) AssetDatabase.DeleteAsset(oldPath);
        }
        CreateMaterial("Sprite", "Sprites/Default", m =>
        {
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
        });
    }

    static void CreateMaterial(string name, string shaderName, System.Action<Material> configure)
    {
        string path = MaterialsDir + "/" + name + ".mat";
        var shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogError("[ProjectSetup] Shader not found: " + shaderName);
            return;
        }
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            if (existing.shader != shader) existing.shader = shader;
            configure(existing);
            EditorUtility.SetDirty(existing);
            Debug.Log("[ProjectSetup] Material up to date: " + path);
            return;
        }
        var mat = new Material(shader) { name = name };
        configure(mat);
        AssetDatabase.CreateAsset(mat, path);
        Debug.Log("[ProjectSetup] Created " + path + " (" + shaderName + ")");
    }

    static void CreateMainScene()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        // The scene stays empty: DeathNotice.Game.GameRoot spawns itself at runtime
        // ([RuntimeInitializeOnLoadMethod]) and builds camera, light, UI and stage in code.
        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[ProjectSetup] Created empty " + ScenePath);
        }
        else
        {
            // Older setups put placeholder objects in the scene; remove them.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            bool changed = false;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == "Cube" || go.name == "Main Camera" || go.name == "Directional Light")
                {
                    Object.DestroyImmediate(go);
                    changed = true;
                }
            }
            if (changed)
            {
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[ProjectSetup] Removed placeholder objects from " + ScenePath);
            }
        }

        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[ProjectSetup] Build Settings scenes: " + string.Join(", ", EditorBuildSettings.scenes.Select(s => s.path)));
    }
}
