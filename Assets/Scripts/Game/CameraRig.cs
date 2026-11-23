using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Fixed oblique camera on the stage with a slow "breathing" sway and a decaying shake.
    /// The camera renders into the left part of the screen; the right part belongs to the paper form.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera cam;
        public float stageFraction = 0.6f;

        private Vector3 focus = new Vector3(0f, 1.5f, 0.5f);
        private float distance = 15f;
        private float pitch = 28f;
        private float yaw = -18f;
        private float shake;
        private float t;

        /// <summary>The stage view looks right for roughly this aspect; narrower views pull the camera back.</summary>
        private const float DesignAspect = 1.1f;

        public void Frame(Vector3 newFocus, float newDistance, float newPitch, float newYaw)
        {
            focus = newFocus;
            distance = newDistance;
            pitch = newPitch;
            yaw = newYaw;
        }

        public void Shake(float amount)
        {
            shake = Mathf.Max(shake, Mathf.Clamp01(amount));
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            float frac = Mathf.Clamp(stageFraction, 0.2f, 1f);
            Rect r = new Rect(0f, 0f, frac, 1f);
            if (cam.rect != r) cam.rect = r;

            t += Time.deltaTime;
            float yawOff = Mathf.Sin(t * 0.31f) * 1.4f;
            float pitchOff = Mathf.Sin(t * 0.23f + 1.3f) * 0.7f;
            Quaternion rot = Quaternion.Euler(pitch + pitchOff, yaw + yawOff, 0f);

            float aspect = cam.aspect > 0.01f ? cam.aspect : DesignAspect;
            float dist = distance * Mathf.Max(1f, DesignAspect / aspect);
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
