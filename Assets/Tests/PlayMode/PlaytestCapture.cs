using System.IO;
using DeathNotice.Game;
using UnityEngine;

namespace DeathNotice.Tests.PlayMode
{
    /// <summary>
    /// Screenshots in batchmode (no Game view, so ScreenCapture does not work): the 3D camera renders into
    /// one RenderTexture, the canvas is switched to Screen Space - Camera and drawn by a UI-only camera into
    /// a second, transparent one, and the two are composited on the CPU. While active, the whole game runs
    /// at the capture resolution (canvas scaler, camera aspect and stage layout all follow the RT size),
    /// which is also how the layout test simulates different window shapes.
    /// Needs a GPU: run Unity without -nographics (tools/test-playmode.ps1 does).
    /// </summary>
    public class PlaytestCapture
    {
        private GameRoot root;
        private Camera uiCam;
        private RenderTexture sceneRT;
        private RenderTexture uiRT;
        private int prevMask;
        private RenderMode prevMode;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public string Folder { get; private set; }

        public static bool GraphicsAvailable
        {
            get { return SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null; }
        }

        public void Begin(GameRoot gameRoot, int w, int h, string folder)
        {
            root = gameRoot;
            Folder = folder;
            Directory.CreateDirectory(folder);
            prevMask = root.Cam.cullingMask;
            prevMode = root.Canvas.renderMode;

            var go = new GameObject("PlaytestUICam");
            Object.DontDestroyOnLoad(go);
            go.transform.position = new Vector3(0f, -800f, 0f);
            uiCam = go.AddComponent<Camera>();
            uiCam.clearFlags = CameraClearFlags.SolidColor;
            uiCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            uiCam.cullingMask = 1 << 5;
            uiCam.nearClipPlane = 0.01f;
            uiCam.farClipPlane = 10f;
            uiCam.allowHDR = false;
            uiCam.allowMSAA = false;
            uiCam.depth = 50f;

            root.Cam.cullingMask = prevMask & ~(1 << 5);
            root.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
            root.Canvas.worldCamera = uiCam;
            root.Canvas.planeDistance = 1f;
            Resize(w, h);
        }

        public void Resize(int w, int h)
        {
            Width = w;
            Height = h;
            if (sceneRT != null) { root.Cam.targetTexture = null; sceneRT.Release(); Object.Destroy(sceneRT); }
            if (uiRT != null) { uiCam.targetTexture = null; uiRT.Release(); Object.Destroy(uiRT); }
            sceneRT = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            sceneRT.antiAliasing = 4;
            uiRT = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            root.Cam.targetTexture = sceneRT;
            uiCam.targetTexture = uiRT;
        }

        public void End()
        {
            if (root != null)
            {
                if (root.Cam != null) { root.Cam.targetTexture = null; root.Cam.cullingMask = prevMask; }
                if (root.Canvas != null) { root.Canvas.renderMode = prevMode; root.Canvas.worldCamera = null; }
            }
            if (uiCam != null) Object.Destroy(uiCam.gameObject);
            if (sceneRT != null) { sceneRT.Release(); Object.Destroy(sceneRT); }
            if (uiRT != null) { uiRT.Release(); Object.Destroy(uiRT); }
            sceneRT = null;
            uiRT = null;
        }

        /// <summary>Renders both layers now and writes Folder/name.png. Returns the full path.</summary>
        public string Save(string name)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = sceneRT;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = prev;
            Canvas.ForceUpdateCanvases();
            root.Cam.Render();
            uiCam.Render();

            Texture2D scene = Read(sceneRT);
            Texture2D ui = Read(uiRT);
            Color32[] a = scene.GetPixels32();
            Color32[] b = ui.GetPixels32();
            for (int i = 0; i < a.Length; i++)
            {
                // UI was blended over transparent black, so its rgb is already weighted by its alpha.
                int ia = 255 - b[i].a;
                a[i].r = (byte)Mathf.Min(255, b[i].r + a[i].r * ia / 255);
                a[i].g = (byte)Mathf.Min(255, b[i].g + a[i].g * ia / 255);
                a[i].b = (byte)Mathf.Min(255, b[i].b + a[i].b * ia / 255);
                a[i].a = 255;
            }
            scene.SetPixels32(a);
            scene.Apply();
            string path = Path.Combine(Folder, name + ".png");
            File.WriteAllBytes(path, scene.EncodeToPNG());
            Object.Destroy(scene);
            Object.Destroy(ui);
            return path;
        }

        private static Texture2D Read(RenderTexture rt)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }
    }
}
