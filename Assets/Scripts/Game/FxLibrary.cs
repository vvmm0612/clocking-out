using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>Flickers a light's intensity (fire glow).</summary>
    public class Flicker : MonoBehaviour
    {
        public Light target;
        public float baseIntensity = 1.5f;
        private float seed;

        private void Start() { seed = Random.value * 10f; }

        private void Update()
        {
            if (target == null) return;
            float n = Mathf.PerlinNoise(seed, Time.time * 6f);
            target.intensity = baseIntensity * (0.7f + 0.6f * n);
        }
    }

    /// <summary>Procedural particle effects: fire, water, smoke, sparks, splash, bubbles, dust.</summary>
    public static class FxLibrary
    {
        private static Gradient Fade(Color a, Color b)
        {
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        private static ParticleSystem Make(string name, Vector3 pos, Transform parent, Vector3 direction)
        {
            var go = new GameObject("fx_" + name);
            go.SetActive(false);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
            // The default cone shape emits along +Z.
            go.transform.rotation = Quaternion.FromToRotation(Vector3.forward, dir);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            if (r != null)
            {
                r.sharedMaterial = Mats.Particle();
                r.renderMode = ParticleSystemRenderMode.Billboard;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            return ps;
        }

        private static void Finish(ParticleSystem ps)
        {
            ps.gameObject.SetActive(true);
            ps.Play();
        }

        private static void Burst(ParticleSystem ps, int count)
        {
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
        }

        /// <summary>
        /// Spawns an effect at a world position. Returns null (and logs) for unknown kinds.
        /// "direction" is only used by "water" (stream direction).
        /// </summary>
        public static ParticleSystem Spawn(string kind, Vector3 pos, Transform parent, Vector3 direction)
        {
            ParticleSystem ps;
            ParticleSystem.MainModule main;
            ParticleSystem.EmissionModule em;
            ParticleSystem.ShapeModule sh;
            ParticleSystem.ColorOverLifetimeModule col;
            ParticleSystem.SizeOverLifetimeModule sz;

            switch (kind)
            {
                case "fire":
                {
                    ps = Make(kind, pos, parent, Vector3.up);
                    main = ps.main;
                    main.loop = true;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.4f, 0.12f));
                    main.gravityModifier = -0.05f;
                    em = ps.emission;
                    em.rateOverTime = 38f;
                    sh = ps.shape;
                    sh.shapeType = ParticleSystemShapeType.Cone;
                    sh.angle = 12f;
                    sh.radius = 0.16f;
                    col = ps.colorOverLifetime;
                    col.enabled = true;
                    col.color = new ParticleSystem.MinMaxGradient(Fade(new Color(1f, 0.9f, 0.5f), new Color(0.6f, 0.15f, 0.05f)));
                    sz = ps.sizeOverLifetime;
                    sz.enabled = true;
                    sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
                    Light l = new GameObject("fireLight").AddComponent<Light>();
                    l.transform.SetParent(ps.transform, false);
                    l.transform.position = pos + Vector3.up * 0.4f;
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.55f, 0.25f);
                    l.range = 3.5f;
                    l.intensity = 1.6f;
                    Flicker f = l.gameObject.AddComponent<Flicker>();
                    f.target = l;
                    f.baseIntensity = 1.6f;
                    Finish(ps);
                    return ps;
                }
                case "smoke":
                case "dust":
                {
                    bool dust = kind == "dust";
                    ps = Make(kind, pos, parent, Vector3.up);
                    main = ps.main;
                    main.loop = !dust;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(dust ? 0.8f : 0.3f, dust ? 1.6f : 0.7f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.55f, 0.58f, 0.7f), new Color(0.35f, 0.35f, 0.38f, 0.6f));
                    main.gravityModifier = dust ? 0.15f : -0.02f;
                    em = ps.emission;
                    em.rateOverTime = 10f;
                    sh = ps.shape;
                    sh.shapeType = ParticleSystemShapeType.Cone;
                    sh.angle = dust ? 70f : 18f;
                    sh.radius = 0.25f;
                    col = ps.colorOverLifetime;
                    col.enabled = true;
                    col.color = new ParticleSystem.MinMaxGradient(Fade(new Color(0.7f, 0.7f, 0.72f), new Color(0.4f, 0.4f, 0.45f)));
                    sz = ps.sizeOverLifetime;
                    sz.enabled = true;
                    sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
                    if (dust) Burst(ps, 18);
                    Finish(ps);
                    return ps;
                }
                case "sparks":
                {
                    ps = Make(kind, pos, parent, Vector3.up);
                    main = ps.main;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.6f), new Color(1f, 0.6f, 0.2f));
                    main.gravityModifier = 1.2f;
                    sh = ps.shape;
                    sh.shapeType = ParticleSystemShapeType.Cone;
                    sh.angle = 55f;
                    sh.radius = 0.05f;
                    Burst(ps, 40);
                    Finish(ps);
                    return ps;
                }
                case "splash":
                {
                    ps = Make(kind, pos, parent, Vector3.up);
                    main = ps.main;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.9f, 1f, 0.9f), new Color(0.4f, 0.65f, 0.9f, 0.8f));
                    main.gravityModifier = 1f;
                    sh = ps.shape;
                    sh.shapeType = ParticleSystemShapeType.Cone;
                    sh.angle = 35f;
                    sh.radius = 0.2f;
                    Burst(ps, 45);
                    Finish(ps);
                    return ps;
                }
                case "water":
                {
                    Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction : new Vector3(0f, -1f, -0.3f);
                    ps = Make(kind, pos, parent, dir);
                    main = ps.main;
                    main.loop = true;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.82f, 1f, 0.85f), new Color(0.3f, 0.55f, 0.85f, 0.8f));
                    main.gravityModifier = 0.6f;
                    em = ps.emission;
                    em.rateOverTime = 60f;
                    sh = ps.shape;
                    sh.shapeType = ParticleSystemShapeType.Cone;
                    sh.angle = 8f;
                    sh.radius = 0.18f;
                    Finish(ps);
                    return ps;
                }
                case "bubbles":
                {
                    ps = Make(kind, pos, parent, Vector3.up);
                    main = ps.main;
                    main.loop = true;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.95f, 1f, 0.9f), new Color(0.6f, 0.8f, 1f, 0.8f));
                    main.gravityModifier = -0.1f;
                    em = ps.emission;
                    em.rateOverTime = 16f;
                    sh = ps.shape;
                    sh.shapeType = ParticleSystemShapeType.Cone;
                    sh.angle = 15f;
                    sh.radius = 0.18f;
                    Finish(ps);
                    return ps;
                }
            }
            Debug.LogWarning("[FxLibrary] Unknown fx '" + kind + "' (ignored).");
            return null;
        }

        public static void StopEmitting(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            ParticleSystem.MainModule main = ps.main;
            main.stopAction = ParticleSystemStopAction.Destroy;
        }
    }
}
