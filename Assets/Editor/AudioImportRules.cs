using UnityEditor;
using UnityEngine;

/// <summary>
/// One place for audio import settings, applied on every (re)import of files under
/// Assets/Resources/Audio:
///   Bgm/ : Vorbis, quality 0.4, Compressed In Memory (Streaming is not available on WebGL), stereo kept,
///          preloaded (an unloaded clip starts late on WebGL).
///   Sfx/ : Vorbis, quality 0.5, Decompress On Load (no decode hitch on play), forced to mono.
/// WebGL re-encodes to AAC at build time using the same quality value.
/// Bump <see cref="Version"/> after changing the rules so Unity reimports the clips.
/// Re-apply by hand (and write the .meta files):
///   Unity.exe -batchmode -quit -projectPath . -executeMethod AudioImportRules.ReimportAll
/// </summary>
public class AudioImportRules : AssetPostprocessor
{
    const uint Version = 2;
    const string Root = "Assets/Resources/Audio/";

    public override uint GetVersion() { return Version; }

    void OnPreprocessAudio()
    {
        var importer = assetImporter as AudioImporter;
        if (importer == null) return;
        Apply(importer, assetPath);
    }

    /// <summary>Returns true if the path is covered by the rules (and the importer was configured).</summary>
    static bool Apply(AudioImporter importer, string path)
    {
        path = path.Replace('\\', '/');
        bool bgm = path.StartsWith(Root + "Bgm/");
        bool sfx = path.StartsWith(Root + "Sfx/");
        if (!bgm && !sfx) return false;

        AudioImporterSampleSettings s = importer.defaultSampleSettings;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        if (bgm)
        {
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.quality = 0.4f;
            // Preloaded: on WebGL an unloaded clip logs "Trying to get length of sound which is not loaded"
            // and starts late; the data ships in the .data file anyway.
            s.preloadAudioData = true;
            importer.forceToMono = false;
            importer.loadInBackground = false;
        }
        else
        {
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.quality = 0.5f;
            s.preloadAudioData = true;
            importer.forceToMono = true;
            importer.loadInBackground = false;
        }
        importer.defaultSampleSettings = s;
        // No per-platform overrides: WebGL uses the defaults above.
        importer.ClearSampleSettingOverride("WebGL");
        return true;
    }

    [MenuItem("Tools/Clocking Out/Reimport Audio With Rules")]
    public static void ReimportAll()
    {
        int n = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Audio" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null || !Apply(importer, path)) continue;
            importer.SaveAndReimport();
            n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[AudioImportRules] Reimported " + n + " clips.");
    }
}
