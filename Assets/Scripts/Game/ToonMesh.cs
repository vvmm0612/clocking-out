using System.Collections.Generic;
using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Copies of meshes with a smoothed normal in UV channel 3, which the toon outline pass extrudes along.
    /// Hard-edged meshes (cubes, cylinder caps) have split normals; extruding those opens cracks in the
    /// outline at every corner. Averaging the normals of vertices that share a position closes them.
    /// One copy per source mesh (the greybox uses four built-in primitives, so this is tiny).
    /// Unreadable meshes (imported models without Read/Write) are returned unchanged: the shader then falls
    /// back to the regular normal.
    /// </summary>
    public static class ToonMesh
    {
        private static readonly Dictionary<int, Mesh> cache = new Dictionary<int, Mesh>();

        public static Mesh Get(Mesh src)
        {
            if (src == null) return null;
            Mesh m;
            if (cache.TryGetValue(src.GetInstanceID(), out m) && m != null) return m;
            m = Build(src);
            cache[src.GetInstanceID()] = m;
            return m;
        }

        /// <summary>Swaps the MeshFilter's mesh for its smoothed-normal copy.</summary>
        public static void Apply(MeshFilter mf)
        {
            if (mf == null || mf.sharedMesh == null) return;
            Mesh m = Get(mf.sharedMesh);
            if (m != null && m != mf.sharedMesh) mf.sharedMesh = m;
        }

        private static Mesh Build(Mesh src)
        {
            if (!src.isReadable) return src;
            try
            {
                Vector3[] v = src.vertices;
                Vector3[] n = src.normals;
                if (v == null || n == null || n.Length != v.Length || v.Length == 0) return src;
                var sums = new Dictionary<Vector3Int, Vector3>();
                var keys = new Vector3Int[v.Length];
                for (int i = 0; i < v.Length; i++)
                {
                    Vector3Int k = new Vector3Int(Mathf.RoundToInt(v[i].x * 1000f), Mathf.RoundToInt(v[i].y * 1000f), Mathf.RoundToInt(v[i].z * 1000f));
                    keys[i] = k;
                    Vector3 s;
                    sums.TryGetValue(k, out s);
                    // Each distinct face direction counts once (a cube corner has 3 normals; a sphere seam 2 equal ones).
                    sums[k] = s + n[i];
                }
                var smooth = new List<Vector3>(v.Length);
                for (int i = 0; i < v.Length; i++)
                {
                    Vector3 s = sums[keys[i]];
                    smooth.Add(s.sqrMagnitude > 1e-8f ? s.normalized : n[i]);
                }
                Mesh copy = Object.Instantiate(src);
                copy.name = src.name + " (toon)";
                copy.SetUVs(3, smooth);
                return copy;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[ToonMesh] " + src.name + ": " + e.Message);
                return src;
            }
        }
    }
}
