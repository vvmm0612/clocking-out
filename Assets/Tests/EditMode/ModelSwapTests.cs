using System;
using System.Collections.Generic;
using DeathNotice.Core;
using DeathNotice.Game;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeathNotice.Tests.EditMode
{
    /// <summary>
    /// Model replacement with fake "prefabs" built at runtime (no real assets needed): appearance swaps,
    /// transform / behaviours / clues / program visuals stay, colliders are refitted to the model.
    /// </summary>
    public class ModelSwapTests
    {
        private readonly List<GameObject> temp = new List<GameObject>();
        private Func<string, GameObject> savedLoader;

        [SetUp]
        public void SetUp() { savedLoader = StageBuilder.ModelLoader; }

        [TearDown]
        public void TearDown()
        {
            StageBuilder.ModelLoader = savedLoader;
            foreach (GameObject g in temp) if (g != null) Object.DestroyImmediate(g);
            temp.Clear();
        }

        private GameObject Track(GameObject g) { temp.Add(g); return g; }

        private GameObject Cube(string name, Transform parent, Vector3 localPos, Vector3 size, Color color)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.name = name;
            if (parent != null) c.transform.SetParent(parent, false);
            c.transform.localPosition = localPos;
            c.transform.localScale = size;
            var m = new Material(Mats.Lit(Color.white));
            Mats.SetColor(m, color);
            c.GetComponent<Renderer>().sharedMaterial = m;
            return c;
        }

        /// <summary>A model with an optional named part, standing on its origin.</summary>
        private GameObject FakeModel(string name, string part, Vector3 size)
        {
            GameObject root = Track(new GameObject(name));
            Cube("body", root.transform, new Vector3(0f, size.y * 0.5f, 0f), size, new Color(0.3f, 0.6f, 0.2f));
            if (part != null) Cube(part, root.transform, new Vector3(0f, size.y + 0.2f, 0f), new Vector3(0.3f, 0.3f, 0.3f), new Color(0.9f, 0.8f, 0.1f));
            return root;
        }

        private Stage BuildWith(string levelId, Dictionary<string, GameObject> models)
        {
            StageBuilder.ModelLoader = delegate (string path)
            {
                GameObject g;
                return models.TryGetValue(path, out g) ? g : null;
            };
            Stage s = StageBuilder.Build(LevelLoader.LoadFromResources(levelId), null);
            Track(s.gameObject);
            return s;
        }

        private static bool AnyEnabledRendererOutside(Transform root, Transform model)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled || r is ParticleSystemRenderer || r.GetComponentInParent<ProgramVisual>(true) != null) continue;
                if (r.transform.IsChildOf(model)) continue;
                return true;
            }
            return false;
        }

        [Test]
        public void SpinPartKeepsRotationAndColliderFitsModel()
        {
            GameObject wheel = FakeModel("wheel_fake", "spin", new Vector3(2f, 2f, 0.5f));
            Stage s = BuildWith("level_02", new Dictionary<string, GameObject> { { "Models/level_02/wheel", wheel } });
            AnchorTag t = s.Find("wheel");
            Vector3 originBefore = t.transform.position;
            Transform model = t.transform.Find(ModelSwap.ModelChildName);
            Assert.IsNotNull(model, "model not instantiated under the anchor");
            Assert.AreEqual(originBefore, model.position, "model origin must sit on the anchor origin");
            Assert.AreEqual(Vector3.one, model.localScale, "no automatic scaling");
            Transform spin = model.Find("spin");
            Assert.IsTrue(t.spins.Exists(x => x.t == spin), "spin behaviour not attached to the 'spin' part");
            Assert.IsFalse(AnyEnabledRendererOutside(t.transform, model), "greybox renderers still visible");

            BoxCollider[] boxes = t.GetComponents<BoxCollider>();
            BoxCollider fitted = Array.Find(boxes, b => b.enabled);
            Assert.IsNotNull(fitted, "no enabled collider on the anchor");
            Assert.AreEqual(2f, fitted.size.x, 0.01f);
            Assert.AreEqual(2.35f, fitted.size.y, 0.01f, "collider should cover body + spin part");
            foreach (Collider c in t.GetComponentsInChildren<Collider>(true))
                if (c != fitted && c.enabled) Assert.Fail("greybox collider still enabled: " + c.name);

            // Turning the wheel rotates the model part.
            Quaternion before = spin.localRotation;
            t.SetSpin(1f);
            foreach (Spin sp in t.spins) if (sp.t == spin) sp.t.Rotate(sp.axis, 30f, Space.Self);
            Assert.AreNotEqual(before, spin.localRotation);

            // Materials moved onto the project template, colour kept.
            Material m = model.Find("body").GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(Mats.Lit(Color.white).shader, m.shader);
            Assert.AreEqual(0.6f, Mats.GetColor(m).g, 0.01f);
        }

        [Test]
        public void SetHidesSceneryButKeepsAnchorsAndProgramVisuals()
        {
            GameObject set = FakeModel("set_fake", null, new Vector3(12f, 0.5f, 6f));
            Stage s = BuildWith("level_02", new Dictionary<string, GameObject> { { "Models/level_02/_set", set } });
            Transform setInst = s.transform.Find(StageBuilder.SetName);
            Assert.IsNotNull(setInst, "set model missing");
            Assert.IsNotNull(setInst.GetComponentInChildren<Collider>(), "set model needs colliders (falls land on it)");
            foreach (Renderer r in s.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r.transform.IsChildOf(setInst)) continue;
                bool underAnchor = r.GetComponentInParent<AnchorTag>(true) != null;
                bool program = r.GetComponentInParent<ProgramVisual>(true) != null;
                bool figure = r.GetComponentInParent<Figure>(true) != null;
                if (underAnchor || figure) continue;
                if (program) Assert.IsTrue(r.enabled, "program visual hidden: " + r.name);
                else Assert.IsFalse(r.enabled, "greybox scenery still visible: " + r.name);
            }
            // The pond water (program-owned) still follows the water level.
            s.ApplyShow("noon");
            Assert.IsTrue(s.Find("wheel").GetComponentInChildren<Renderer>().enabled, "anchors must keep their greybox when only _set is replaced");
        }

        [Test]
        public void ToggleLightAndHingeBehavioursSurvive()
        {
            var models = new Dictionary<string, GameObject>
            {
                { "Models/level_01/lantern", FakeModel("lantern_fake", null, new Vector3(0.3f, 0.45f, 0.3f)) },
                { "Models/level_01/lamp", FakeModel("lamp_fake", "light", new Vector3(1f, 1f, 1f)) },
                { "Models/common/player", FakeModel("player_fake", null, new Vector3(0.4f, 0.7f, 0.3f)) },
            };
            Stage s = BuildWith("level_01", models);

            AnchorTag lantern = s.Find("lantern");
            Transform lm = lantern.transform.Find(ModelSwap.ModelChildName);
            Light lanternLight = lantern.GetComponentInChildren<Light>(true);
            lantern.Toggle(false);
            Assert.IsFalse(lm.gameObject.activeSelf, "toggle off must hide the model");
            Assert.IsFalse(lanternLight.gameObject.activeInHierarchy, "toggle off must still switch the lantern light off");
            lantern.Toggle(true);
            Assert.IsTrue(lm.gameObject.activeSelf);

            AnchorTag lamp = s.Find("lamp");
            Transform lightPart = lamp.transform.Find(ModelSwap.ModelChildName + "/light");
            Light lampLight = null;
            foreach (Light l in lamp.GetComponentsInChildren<Light>(true)) lampLight = l;
            lamp.LightUp();
            Assert.AreEqual(Mats.Glow(Color.white).shader, lightPart.GetComponent<Renderer>().sharedMaterial.shader, "light part should glow");
            Assert.IsTrue(lampLight.gameObject.activeInHierarchy, "the lamp's own light must still come on");
            Assert.Greater(lamp.spinSpeed, 0f, "the beam must still spin");
            Assert.IsNotNull(lamp.GetComponent<BoxCollider>(), "clue anchor needs a collider for hovering");
            Assert.Greater(lamp.MarkerPoint().y, lamp.transform.position.y + 1f, "clue marker should float above the model");

            Assert.IsNotNull(s.player.visual.Find(ModelSwap.ModelChildName), "player model not used");
            Assert.IsNotNull(s.ghost.visual.Find(ModelSwap.ModelChildName), "ghost should reuse the player model");
            s.player.SetColor(Color.red);
            Assert.AreEqual(Color.red, Mats.GetColor(s.player.mats[0]));
        }

        [Test]
        public void HingeTogglePartFollowsTheGreyboxHinge()
        {
            GameObject arm = FakeModel("arm_fake", "toggle", new Vector3(0.12f, 3.2f, 0.12f));
            Stage s = BuildWith("level_04", new Dictionary<string, GameObject> { { "Models/level_04/signal_arm", arm } });
            AnchorTag t = s.Find("signal_arm");
            RotationFollower f = t.GetComponentInChildren<RotationFollower>();
            Assert.IsNotNull(f, "toggle part should follow the hinge");
            Assert.AreEqual("toggle", f.name);
            Quaternion before = f.transform.localRotation;
            t.Toggle(true);
            f.Sync();
            Assert.AreEqual(80f, Quaternion.Angle(before, f.transform.localRotation), 0.5f, "toggle part should swing like the greybox arm");
        }
    }
}
