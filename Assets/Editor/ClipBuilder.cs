using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CodigoECavaleiros.EditorTools
{
    /// <summary>
    /// Gera os clipes Humanoid que o KayKit não tem: Lament_Down (ajoelhar),
    /// Lament_Loop (chorando ajoelhado) e Rage (raiva em pé, batendo o pé).
    /// Parte da pose Idle_A e sobrescreve só alguns músculos.
    /// </summary>
    public static class ClipBuilder
    {
        const string OutDir = "Assets/Animations/Generated/";
        const string General = "Assets/Animations/KayKit/Rig_Medium/Rig_Medium_General.fbx";

        class Key
        {
            public float t; public float y; public float z; public float pitch;
            public Dictionary<string, float> m;
            public Key(float t, float y, float z, Dictionary<string, float> m) { this.t = t; this.y = y; this.z = z; this.m = m; }
        }

        static Dictionary<string, float> M(params object[] kv)
        {
            var d = new Dictionary<string, float>();
            for (int i = 0; i < kv.Length; i += 2) d[(string)kv[i]] = System.Convert.ToSingle(kv[i + 1]);
            return d;
        }

        static Dictionary<string, float> Both(string suffix, float v)
        {
            return M("Left " + suffix, v, "Right " + suffix, v);
        }

        static Dictionary<string, float> Merge(params Dictionary<string, float>[] ds)
        {
            var r = new Dictionary<string, float>();
            foreach (var d in ds) foreach (var kv in d) r[kv.Key] = kv.Value;
            return r;
        }

        static string Bind(string n)
        {
            if (n.Contains("Thumb") || n.Contains("Index") || n.Contains("Middle") || n.Contains("Ring") || n.Contains("Little"))
            {
                string side = n.StartsWith("Left") ? "LeftHand" : "RightHand";
                string rest = n.Substring(n.IndexOf(' ') + 1);            // "Thumb 1 Stretched"
                string[] p = rest.Split(' ');
                if (p.Length == 3) return side + "." + p[0] + "." + p[1] + " " + p[2];
                if (p.Length == 2) return side + "." + p[0] + " " + p[1]; // "Thumb Spread"
            }
            return n;
        }

        // ---- poses -------------------------------------------------------------
        static Dictionary<string, float> KneelLegs()
        {
            return Merge(
                Both("Upper Leg Front-Back", 0.55f),
                Both("Lower Leg Stretch", -0.95f),
                Both("Foot Up-Down", -0.9f));
        }

        static Dictionary<string, float> HandsOnFace(float armUp)
        {
            return Merge(
                Both("Arm Down-Up", armUp),
                Both("Arm Front-Back", 0.9f),
                Both("Forearm Stretch", -0.95f),
                Both("Hand Down-Up", 0.2f));
        }

        static Dictionary<string, float> Sob(float k)
        {
            // k = 0..1 (soluço)
            return Merge(
                M("Spine Front-Back", 0.15f + 0.15f * k, "Chest Front-Back", 0.2f + 0.15f * k,
                  "Neck Nod Down-Up", -0.6f - 0.15f * k, "Head Nod Down-Up", -0.5f - 0.15f * k),
                Both("Shoulder Down-Up", 0.15f * k));
        }

        [MenuItem("Tools/Código & Cavaleiros/Gerar Clipes (Lamentar e Raiva)")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(OutDir);

            var proto = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Hero_Leo.prefab");
            var go = (GameObject)UnityEngine.Object.Instantiate(proto);
            go.hideFlags = HideFlags.HideAndDontSave;
            var anim = go.GetComponent<Animator>();

            AnimationClip idle = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(General))
                if (o is AnimationClip c && c.name == "Idle_A") idle = c;
            idle.SampleAnimation(go, 0f);

            var handler = new HumanPoseHandler(anim.avatar, go.transform);
            var basePose = new HumanPose();
            handler.GetHumanPose(ref basePose);
            float baseY = basePose.bodyPosition.y, baseZ = basePose.bodyPosition.z;

            // Lament_Down: em pé -> ajoelha -> mãos no rosto
            var down = new List<Key>
            {
                new Key(0.00f, baseY, baseZ, M()),
                new Key(0.45f, baseY - 0.18f, baseZ, Merge(
                    Both("Upper Leg Front-Back", 0.45f), Both("Lower Leg Stretch", 0.0f), M("Spine Front-Back", 0.1f))),
                new Key(1.00f, 0.62f, baseZ, Merge(KneelLegs(), HandsOnFace(0.35f), Sob(0f))),
                new Key(1.30f, 0.62f, baseZ, Merge(KneelLegs(), HandsOnFace(0.35f), Sob(0.3f))),
            };
            Save("Lament_Down", basePose, down, false, 1.3f);

            // Lament_Loop: chorando, ombros balançando (loop)
            var loop = new List<Key>();
            var baseKneel = Merge(KneelLegs(), HandsOnFace(0.35f));
            loop.Add(new Key(0.00f, 0.62f, baseZ, Merge(baseKneel, Sob(0f))));
            loop.Add(new Key(0.20f, 0.62f, baseZ, Merge(baseKneel, Sob(1f))));
            loop.Add(new Key(0.40f, 0.62f, baseZ, Merge(baseKneel, Sob(0.1f))));
            loop.Add(new Key(0.60f, 0.62f, baseZ, Merge(baseKneel, Sob(0.9f))));
            loop.Add(new Key(1.00f, 0.62f, baseZ, Merge(baseKneel, Sob(0f))));
            Save("Lament_Loop", basePose, loop, true, 1.0f);

            // Rage: punhos fechados, tronco inclinado, bate o pé alternando
            var rageBase = Merge(
                Both("Arm Down-Up", -0.55f), Both("Arm Front-Back", 0.35f), Both("Forearm Stretch", -0.4f),
                M("Spine Front-Back", 0.2f, "Chest Front-Back", 0.15f, "Neck Nod Down-Up", -0.25f, "Head Nod Down-Up", -0.2f));
            var rage = new List<Key>
            {
                new Key(0.00f, baseY, baseZ, Merge(rageBase, M("Spine Twist Left-Right", 0.25f))),
                new Key(0.20f, baseY - 0.04f, baseZ, Merge(rageBase, M("Spine Twist Left-Right", 0.1f, "Left Upper Leg Front-Back", 0.95f, "Left Lower Leg Stretch", 0.1f))),
                new Key(0.40f, baseY, baseZ, Merge(rageBase, M("Spine Twist Left-Right", -0.25f))),
                new Key(0.60f, baseY - 0.04f, baseZ, Merge(rageBase, M("Spine Twist Left-Right", -0.1f, "Right Upper Leg Front-Back", 0.95f, "Right Lower Leg Stretch", 0.1f))),
                new Key(0.80f, baseY, baseZ, Merge(rageBase, M("Spine Twist Left-Right", 0.25f))),
            };
            Save("Rage", basePose, rage, true, 0.8f);

            UnityEngine.Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ClipBuilder] Clipes gerados em " + OutDir);
        }

        static void Save(string name, HumanPose basePose, List<Key> keys, bool loop, float length)
        {
            var clip = new AnimationClip { name = name, frameRate = 30 };
            var curves = new Dictionary<string, AnimationCurve>();
            Func(curves, "RootT.x", keys, k => basePose.bodyPosition.x);
            Func(curves, "RootT.y", keys, k => k.y);
            Func(curves, "RootT.z", keys, k => k.z);
            Func(curves, "RootQ.x", keys, k => basePose.bodyRotation.x);
            Func(curves, "RootQ.y", keys, k => basePose.bodyRotation.y);
            Func(curves, "RootQ.z", keys, k => basePose.bodyRotation.z);
            Func(curves, "RootQ.w", keys, k => basePose.bodyRotation.w);
            for (int i = 0; i < HumanTrait.MuscleCount; i++)
            {
                string mn = HumanTrait.MuscleName[i];
                float b = basePose.muscles[i];
                Func(curves, Bind(mn), keys, k => { float v; return k.m.TryGetValue(mn, out v) ? v : b; });
            }
            foreach (var kv in curves)
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), kv.Key), kv.Value);

            var s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = loop; s.loopBlendOrientation = true; s.loopBlendPositionY = true; s.loopBlendPositionXZ = true;
            s.keepOriginalOrientation = true; s.keepOriginalPositionY = true; s.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, s);

            string path = OutDir + name + ".anim";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
        }

        static void Func(Dictionary<string, AnimationCurve> curves, string binding, List<Key> keys, System.Func<Key, float> val)
        {
            var c = new AnimationCurve();
            foreach (var k in keys) c.AddKey(new Keyframe(k.t, val(k)));
            for (int i = 0; i < c.length; i++) AnimationUtility.SetKeyBroken(c, i, false);
            for (int i = 0; i < c.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            curves[binding] = c;
        }
    }
}
