using UnityEditor;

/// <summary>
/// Import settings for models under Assets/Resources/Models (docs/art-brief.md, section 2):
/// static meshes only (no rig, no animation, no cameras / lights), scale 1, and Read/Write enabled for
/// the whole-level "_set" models, which get MeshColliders at runtime (falls land on them).
/// </summary>
public class ModelImportRules : AssetPostprocessor
{
    const uint Version = 1;
    const string Root = "Assets/Resources/Models/";

    public override uint GetVersion() { return Version; }

    void OnPreprocessModel()
    {
        string path = assetPath.Replace('\\', '/');
        if (!path.StartsWith(Root)) return;
        var importer = assetImporter as ModelImporter;
        if (importer == null) return;
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        string file = System.IO.Path.GetFileNameWithoutExtension(path);
        importer.isReadable = file == "_set";
    }
}
