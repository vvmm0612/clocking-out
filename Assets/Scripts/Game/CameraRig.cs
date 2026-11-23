using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Oblique camera on the stage with a slow "breathing" sway and a decaying shake.
    /// The camera renders into the left part of the screen; the right part belongs to the paper form.
    /// <see cref="FitTo"/> frames a bounding box: every frame (cheap: 8 corners) it works out the distance and
    /// focus that make the box fill <see cref="fill"/> of the 3D view's height (or 92% of its width, whichever
    /// is tighter) for the current viewport shape, so window size, the caption strip and the paper width
    /// never crop the diorama.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera cam;
        public float stageFraction = 0.6f;
        /// <summary>Fraction of the screen height kept free at the bottom (the caption strip during playback).</summary>
        public float bottomInset;
        /// <summary>Title screen: the 3D view covers the whole window.</summary>
        public bool fullScreen;

        private Vector3 focus = new Vector3(0f, 1.5f, 0.5f);
        private float distance = 15f;
        private float pitch = 28f;
        private float yaw = -18f;
        private float shake;
        private float t;

        private bool fitting;
        private Bounds fitBounds;
        /// <summary>Target height fraction of the view for the framed box.</summary>
        public float fill = 0.8f;
        /// <summary>Where in the view the box goes, in -1..1 viewport units (x: left..right, y: bottom..top).</summary>
        public Rect window = Rect.MinMaxRect(-0.92f, -0.86f, 0.92f, 0.8f);
        /// <summary>Sway amplitude multiplier (0 = still, for screenshots).</summary>
        public float sway = 1f;

        /// <summary>The stage view looks right for roughly this aspect; narrower views pull the camera back.</summary>
        private const float DesignAspect = 1.1f;

        public Vector3 Focus { get { return focus; } }
        public float Distance { get { return distance; } }

        public void Frame(Vector3 newFocus, float newDistance, float newPitch, float newYaw)
        {
            fitting = false;
            focus = newFocus;
            distance = newDistance;
            pitch = newPitch;
            yaw = newYaw;
        }

        /// <summary>Frames a box at the given angles; fillHeight is the share of the view height it should take.</summary>
        public void FitTo(Bounds b, float newPitch, float newYaw, float fillHeight)
        {
            fitting = true;
            fitBounds = b;
            pitch = newPitch;
            yaw = newYaw;
            fill = Mathf.Clamp(fillHeight, 0.3f, 0.98f);
            Solve();
        }

        public void Shake(float amount)
        {
            shake = Mathf.Max(shake, Mathf.Clamp01(amount));
        }

        /// <summary>Distance and focus that fit the box into the window (iterates: the box is not centred on its projection).</summary>
        private void Solve()
        {
            if (!fitting || cam == null) return;
            float aspect = cam.aspect > 0.01f ? cam.aspect : DesignAspect;
            float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * aspect;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 right = rot * Vector3.right, up = rot * Vector3.up, fwd = rot * Vector3.forward;

            // Vertical target: "fill" of the height, placed inside window.y (scaled to keep its centre).
            float wy0 = window.yMin, wy1 = window.yMax;
            float wyc = (wy0 + wy1) * 0.5f;
            float half = fill;
            float lo = Mathf.Max(wy0, wyc - half), hi = Mathf.Min(wy1, wyc + half);
            float xlo = window.xMin, xhi = window.xMax;

            Vector3 f = fitBounds.center;
            Vector3 ext = fitBounds.extents;
            // Distance from the box seen head-on (centred); the window's offset is applied afterwards by moving
            // the focus sideways, which barely changes the apparent size.
            float d = 0.1f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = Vector3.Scale(ext, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                float x = Vector3.Dot(c, right), y = Vector3.Dot(c, up), z = Vector3.Dot(c, fwd);
                d = Mathf.Max(d, Mathf.Abs(y) / (tanV * (hi - lo) * 0.5f) - z);
                d = Mathf.Max(d, Mathf.Abs(x) / (tanH * (xhi - xlo) * 0.5f) - z);
            }
            for (int iter = 0; iter < 4; iter++)
            {
                // Where does the box land on screen at this distance? Shift the focus to centre it in the window.
                float ymin = float.MaxValue, ymax = float.MinValue, xmin = float.MaxValue, xmax = float.MinValue;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = fitBounds.center + Vector3.Scale(ext, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)) - f;
                    float x = Vector3.Dot(c, right), y = Vector3.Dot(c, up), z = Vector3.Dot(c, fwd);
                    float py = y / ((d + z) * tanV), px = x / ((d + z) * tanH);
                    ymin = Mathf.Min(ymin, py); ymax = Mathf.Max(ymax, py);
                    xmin = Mathf.Min(xmin, px); xmax = Mathf.Max(xmax, px);
                }
                float dy = (ymin + ymax) * 0.5f - (lo + hi) * 0.5f;
                float dx = (xmin + xmax) * 0.5f - (xlo + xhi) * 0.5f;
                f += up * (dy * d * tanV) + right * (dx * d * tanH);
            }
            focus = f;
            distance = d;
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            float frac = fullScreen ? 1f : Mathf.Clamp(stageFraction, 0.2f, 1f);
            float inset = fullScreen ? 0f : Mathf.Clamp(bottomInset, 0f, 0.5f);
            Rect r = new Rect(0f, inset, frac, 1f - inset);
            if (cam.rect != r) cam.rect = r;

            t += Time.deltaTime;
            float yawOff = Mathf.Sin(t * 0.31f) * 1.4f * sway;
            float pitchOff = Mathf.Sin(t * 0.23f + 1.3f) * 0.7f * sway;
            Quaternion rot = Quaternion.Euler(pitch + pitchOff, yaw + yawOff, 0f);

            float dist;
            if (fitting)
            {
                Solve();
                dist = distance;
            }
            else
            {
                float aspect = cam.aspect > 0.01f ? cam.aspect : DesignAspect;
                dist = distance * Mathf.Max(1f, DesignAspect / aspect);
            }
            Vector3 pos = focus - rot * Vector3.forward * dist;

            if (shake > 0.001f)
            {
                pos += Random.insideUnitSphere * shake * 0.35f;
                shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 1.6f);
            }
            cam.transform.position = pos;
            cam.transform.rotation = rot;
        }
    }
}
