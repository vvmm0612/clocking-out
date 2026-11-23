using System.IO;
using DeathNotice.Core;
using DeathNotice.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Stage = DeathNotice.Game.Stage;

/// <summary>
/// Renders every level's diorama from the in-game camera to Logs/stage_&lt;id&gt;.png (and a Shift-preview
/// variant), so layouts can be reviewed without opening the editor. Needs a GPU (no -nographics):
///   Unity.exe -batchmode -quit -projectPath . -executeMethod StagePreview.RenderAll -logFile Logs/preview.log
/// </summary>
public static class StagePreview
{
    public static void RenderAll()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Directory.CreateDirectory("Logs");
        foreach (LevelData level in LevelLoader.LoadAllFromResources())
        {
            Render(level, null);
            if (level.states.Count > 0 && !level.HasShiftOnlyOptions()) Render(level, level.states[level.states.Count - 1].shift);
        }
        Debug.Log("[StagePreview] Done.");
    }

    static void Render(LevelData level, string peekShift)
    {
        Stage stage = StageBuilder.Build(level, null);
        bool peek = peekShift != null;
        if (peek)
        {
            stage.ApplyShow(peekShift);
            FieldData place = level.FindField(Simulator.PlaceField);
            if (place != null && place.options.Count > 0) stage.ShowGhost(place.options[0].id);
        }
        StagePalette p = stage.palette;

        var sunGo = new GameObject("Sun");
        sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        Light sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.color = peek ? new Color(0.62f, 0.55f, 0.95f) : p.sun;
        sun.intensity = peek ? 0.55f : 1.1f;
        RenderSettings.ambientMode = AmbientMode.Flat;
        Color amb = peek ? p.peekAmbient : p.ambient;
        RenderSettings.ambientLight = amb;
        var sh = new SphericalHarmonicsL2();
        sh.AddAmbientLight(amb);
        RenderSettings.ambientProbe = sh;

        const int w = 780, h = 720;
        var camGo = new GameObject("PreviewCam");
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 38f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = peek ? p.peekSky : p.sky;
        cam.aspect = w / (float)h;
        Quaternion rot = Quaternion.Euler(stage.camPitch, stage.camYaw, 0f);
        float dist = stage.camDistance * Mathf.Max(1f, 1.1f / cam.aspect);
        cam.transform.SetPositionAndRotation(stage.camFocus - rot * Vector3.forward * dist, rot);

        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        string file = "Logs/stage_" + level.id + (peek ? "_peek_" + peekShift : "") + ".png";
        File.WriteAllBytes(file, tex.EncodeToPNG());
        Debug.Log("[StagePreview] Wrote " + file);

        cam.targetTexture = null;
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(sunGo);
        Object.DestroyImmediate(stage.gameObject);
    }
}
