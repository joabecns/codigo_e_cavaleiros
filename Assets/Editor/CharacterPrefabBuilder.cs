#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using CodigoECavaleiros.Characters;

namespace CodigoECavaleiros.EditorTools
{
    /// <summary>Monta os prefabs de herois/viloes a partir dos FBX do pacote de personagens.</summary>
    public static class CharacterPrefabBuilder
    {
        const string FbxDir = "Assets/Characters/FBX/";
        const string MeshDir = "Assets/Characters/Meshes/";
        const string PrefabDir = "Assets/Prefabs/Characters/";
        const string Atlas = "Assets/Characters/FBX/Textures_4.png";

        class Spec
        {
            public string id; public string skin; public float scale = 1f; public string[] parts;
            public Spec(string id, string skin, float scale, params string[] parts)
            { this.id = id; this.skin = skin; this.scale = scale; this.parts = parts; }
        }

        static readonly Spec[] Specs =
        {
            new Spec("hero_leo","240,190,150",1f,"Hairstyle_male_010","T-Shirt_009","Pants_010","Shoe_Sneakers_009"),
            new Spec("hero_theo","198,134,90",1f,"Hairstyle_male_012","Outwear_036","Pants_014","Glasses_004","Shoe_Slippers_005"),
            new Spec("hero_bruno","128,86,58",1f,"Headphones_002","Outwear_029","Shorts_003","Socks_008","Shoe_Sneakers_009"),
            new Spec("hero_max","233,170,130",1f,"Hat_010","T-Shirt_009:214,58,60","Shorts_003:60,92,180","Shoe_Slippers_002","Moustache_002"),
            new Spec("hero_caio","252,205,170",1f,"Hat_049","T-Shirt_009:130,80,190","Pants_010","Glasses_006","Shoe_Sneakers_009"),
            new Spec("vil_palhaco","236,226,216",1f,"Costume_10_001","Clown_nose_001","Gloves_014"),
            new Spec("vil_rato","150,166,140",1f,"Costume_6_001","Gloves_014"),
            new Spec("vil_coelho","205,190,205",1f,"Hat_049:48,48,62","Outwear_036:36,34,48","Pants_014","Gloves_014","Glasses_004","Shoe_Sneakers_009:30,30,36"),
            new Spec("vil_sargento","196,140,112",1f,"Hat_057","Moustache_002","Outwear_029:70,84,52","Pants_010:46,52,44","Gloves_006","Shoe_Sneakers_009:30,30,36"),
            new Spec("boss_capitao","112,150,102",1.28f,"Hat_057","Glasses_006","Moustache_001","Outwear_029:28,26,34","Pants_014","Gloves_014:160,24,34","Shoe_Sneakers_009:30,30,36"),
        };

        static Color32[] atlasPixels; static int atlasW, atlasH;
        static readonly Dictionary<string, Vector2> uvCache = new Dictionary<string, Vector2>();

        [MenuItem("Codigo e Cavaleiros/Habilitar leitura dos FBX")]
        public static string EnableReadable()
        {
            int n = 0;
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Characters/FBX" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var imp = AssetImporter.GetAtPath(p) as ModelImporter;
                if (imp != null && !imp.isReadable) { imp.isReadable = true; imp.SaveAndReimport(); n++; }
            }
            return "reimportados: " + n;
        }

        static void LoadAtlas()
        {
            if (atlasPixels != null) return;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(File.ReadAllBytes(Atlas));
            atlasPixels = t.GetPixels32(); atlasW = t.width; atlasH = t.height;
            Object.DestroyImmediate(t);
        }

        static Vector2 NearestUV(string rgb)
        {
            Vector2 r;
            if (uvCache.TryGetValue(rgb, out r)) return r;
            LoadAtlas();
            var s = rgb.Split(',');
            int cr = int.Parse(s[0]), cg = int.Parse(s[1]), cb = int.Parse(s[2]);
            int best = int.MaxValue, bi = 0;
            for (int i = 0; i < atlasPixels.Length; i++)
            {
                var c = atlasPixels[i]; if (c.a < 128) continue;
                int d = (c.r - cr) * (c.r - cr) + (c.g - cg) * (c.g - cg) + (c.b - cb) * (c.b - cb);
                if (d < best) { best = d; bi = i; if (d == 0) break; }
            }
            r = new Vector2((bi % atlasW + 0.5f) / atlasW, (bi / atlasW + 0.5f) / atlasH);
            uvCache[rgb] = r; return r;
        }

        static Mesh Recolor(Mesh src, string rgb, string id, string part)
        {
            if (!Directory.Exists(MeshDir)) Directory.CreateDirectory(MeshDir);
            var m = Object.Instantiate(src);
            var uv = NearestUV(rgb);
            var arr = new Vector2[src.vertexCount];
            for (int i = 0; i < arr.Length; i++) arr[i] = uv;
            m.uv = arr; m.name = id + "_" + part;
            var path = MeshDir + id + "_" + part + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        static void Attach(GameObject root, Dictionary<string, Transform> bones, string fbxName, string colorRgb, string id, StringBuilder log, out SkinnedMeshRenderer result)
        {
            result = null;
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(FbxDir + fbxName + ".fbx");
            if (src == null) { log.AppendLine("FALTA FBX: " + fbxName); return; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var smr = inst.GetComponentInChildren<SkinnedMeshRenderer>();
            var go = smr.gameObject;
            var newBones = new Transform[smr.bones.Length];
            for (int i = 0; i < newBones.Length; i++)
            {
                Transform b; var nm = smr.bones[i] != null ? smr.bones[i].name : "";
                if (!bones.TryGetValue(nm, out b)) { log.AppendLine("osso sem par: " + fbxName + "/" + nm); b = null; }
                newBones[i] = b;
            }
            var mesh = smr.sharedMesh;
            if (colorRgb != null) mesh = Recolor(mesh, colorRgb, id, fbxName);
            var mats = smr.sharedMaterials;
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
            go.name = fbxName;
            smr.bones = newBones; smr.rootBone = bones["Root"]; smr.sharedMesh = mesh; smr.sharedMaterials = mats;
            Object.DestroyImmediate(inst);
            result = smr;
        }

        public static string Build(string id)
        {
            Spec spec = null; foreach (var s in Specs) if (s.id == id) spec = s;
            if (spec == null) return "id desconhecido";
            var log = new StringBuilder();
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);

            var bodySrc = AssetDatabase.LoadAssetAtPath<GameObject>(FbxDir + "Body_010.fbx");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(bodySrc);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = id;
            root.transform.localScale = Vector3.one * spec.scale;

            var bones = new Dictionary<string, Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (!bones.ContainsKey(t.name)) bones[t.name] = t;

            var body = root.GetComponentInChildren<SkinnedMeshRenderer>();
            body.sharedMesh = Recolor(body.sharedMesh, spec.skin, id, "Body");

            foreach (var p in spec.parts)
            {
                var split = p.Split(':'); SkinnedMeshRenderer r;
                Attach(root, bones, split[0], split.Length > 1 ? split[1] : null, id, log, out r);
            }

            SkinnedMeshRenderer fn, ff, fr;
            Attach(root, bones, "Male_emotion_usual_001", null, id, log, out fn);
            Attach(root, bones, "Male_emotion_happy_002", null, id, log, out ff);
            Attach(root, bones, "Male_emotion_angry_003", null, id, log, out fr);

            var fc = root.AddComponent<FaceController>();
            var so = new SerializedObject(fc);
            so.FindProperty("faceNormal").objectReferenceValue = fn;
            so.FindProperty("faceFeliz").objectReferenceValue = ff;
            so.FindProperty("faceRaiva").objectReferenceValue = fr;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (ff != null) ff.enabled = false;
            if (fr != null) fr.enabled = false;

            var path = PrefabDir + id + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return id + " ok " + log;
        }

        public static string BuildAll()
        {
            var sb = new StringBuilder();
            foreach (var s in Specs) sb.AppendLine(Build(s.id));
            AssetDatabase.Refresh();
            return sb.ToString();
        }
    }
}
#endif
