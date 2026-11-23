using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Batchmode build entry points. Use tools/build-webgl.ps1 rather than calling directly.
///   Unity.exe -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod BuildScript.BuildWebGL -logFile Logs/build-webgl.log
/// </summary>
public static class BuildScript
{
    const string WebGLOutput = "Builds/WebGL";

    public static void BuildWebGL()
    {
        VisualSetup.Run(); // toon material templates + URP shadow settings must exist before the build
        ApplyWebGLSettings();

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("[BuildScript] No enabled scenes in Build Settings. Run ProjectSetup.Run first.");
            Exit(2);
            return;
        }

        if (Directory.Exists(WebGLOutput))
            Directory.Delete(WebGLOutput, true);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = WebGLOutput,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None, // Development Build off
        };

        var started = DateTime.Now;
        BuildReport report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        Debug.Log($"[BuildScript] WebGL build result: {summary.result}, size: {summary.totalSize} bytes, " +
                  $"errors: {summary.totalErrors}, warnings: {summary.totalWarnings}, time: {DateTime.Now - started}");

        Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static void ApplyWebGLSettings()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true; // itch.io serves without Content-Encoding headers
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None; // smallest
        PlayerSettings.WebGL.dataCaching = true;
        // Assets/WebGLTemplates/ClockingOut: canvas fills the window (DPR capped at 2), dark page, minimal loader.
        PlayerSettings.WebGL.template = "PROJECT:ClockingOut";
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.runInBackground = true;
        PlayerSettings.stripEngineCode = true;
        EditorUserBuildSettings.development = false;
        AssetDatabase.SaveAssets();
    }

    static void Exit(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}
