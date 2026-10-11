using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CodigoECavaleiros.Characters;

namespace CodigoECavaleiros.EditorTools
{
    /// <summary>Monta os prefabs dos heróis/vilões juntando as peças do pacote no esqueleto do corpo.</summary>
    public static class CharacterBuilder
    {
        const string FbxDir = "Assets/Characters/FBX/";
        const string OutDir = "Assets/Prefabs/Characters/";

        class Spec
        {
            public string prefab, display; public float scale; public string[] parts;
            public string hair; public Color hairColor;
            public Spec(string prefab, string display, float scale, params string[] parts)
            { this.prefab = prefab; this.display = display; this.scale = scale; this.parts = parts; }
        }

        static readonly Spec[] Specs =
        {
            // Heróis
            new Spec("Hero_Leo",  "Leo",  1f, "Male_emotion_usual_001","Male_emotion_angry_003","Male_emotion_happy_002","Hairstyle_male_010","T-Shirt_009","Pants_010","Shoe_Sneakers_009"),
            new Spec("Hero_Theo", "Theo", 1f, "Male_emotion_usual_001","Male_emotion_angry_003","Male_emotion_happy_002","Hairstyle_male_012","Outwear_029","Pants_014","Shoe_Sneakers_009","Glasses_004"),
            new Spec("Hero_Bruno","Bruno",1f, "Male_emotion_usual_001","Male_emotion_angry_003","Male_emotion_happy_002","Hat_010","T-Shirt_009","Shorts_003","Socks_008","Shoe_Sneakers_009"),
            new Spec("Hero_Max",  "Max",  1f, "Male_emotion_usual_001","Male_emotion_angry_003","Male_emotion_happy_002","Hairstyle_male_010","Headphones_002","T-Shirt_009","Shorts_003","Shoe_Slippers_005"),
            new Spec("Hero_Caio", "Caio", 1f, "Male_emotion_usual_001","Male_emotion_angry_003","Male_emotion_happy_002","Hairstyle_male_012","Glasses_006","Costume_10_001","Shoe_Sneakers_009"),
            // BugHunters femininas (o pacote só tem corpo/rosto masculino: o cabelo é criado por código)
            Female("Hero_Luna", "Luna", "long",     new Color(0.13f, 0.09f, 0.26f), "T-Shirt_009", "Shorts_003", "Socks_008", "Shoe_Sneakers_009"),
            Female("Hero_Bia",  "Bia",  "ponytail", new Color(0.45f, 0.26f, 0.12f), "Outwear_029", "Pants_014", "Glasses_004", "Shoe_Sneakers_009"),
            Female("Hero_Nina", "Nina", "buns",     new Color(0.92f, 0.36f, 0.58f), "T-Shirt_009", "Pants_010", "Glasses_006", "Shoe_Slippers_005"),
            // Vilões (ordem do jogo, como no protótipo Python)
            new Spec("Vil_Palhaco",  "Palhaço Bug",       1f,   "Male_emotion_usual_001","Male_emotion_angry_003","Male_emotion_happy_002","Clown_nose_001","Costume_10_001","Hat_057","Gloves_006","Shoe_Sneakers_009"),
            new Spec("Vil_Rato",     "Rato Loop",         1f,   "Male_emotion_usual_001","Male_emotion_angry_003","Male_emotion_happy_002","Costume_6_001","Moustache_002","Gloves_006","Shoe_Slippers_002"),
            new Spec("Vil_Coelho",   "Coelho Nulo",       1f,   "Male_emotion_usual_001","Male_emotion_angry_003","Male_emotion_happy_002","Hat_049","Outwear_036","Pants_014","Gloves_014","Shoe_Sneakers_009"),
            new Spec("Vil_Sargento", "Sargento Herança",  1.1f, "Male_emotion_usual_001","Male_emotion_angry_003","Male_emotion_happy_002","Hat_010","Moustache_001","Outwear_029","Pants_014","Gloves_006","Shoe_Sneakers_009"),
            // Chefão
            new Spec("Boss_Capitao", "Compilador Sombrio", 1.3f, "Male_emotion_usual_001","Male_emotion_angry_003","Male_emotion_happy_002","Headphones_002","Glasses_006","Moustache_001","Outwear_036","Pants_014","Gloves_014","Shoe_Sneakers_009"),
        };

        [MenuItem("Tools/Código & Cavaleiros/Construir Personagens")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(OutDir);
            foreach (var old in new[] { "Vil_Bebe", "Vil_Palhaco", "Vil_Rato", "Vil_Coelho", "Vil_Sargento", "Boss_Capitao" })
                AssetDatabase.DeleteAsset(OutDir + old + ".prefab");
            foreach (var s in Specs) Build(s);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CharacterBuilder] " + Specs.Length + " prefabs criados em " + OutDir);
        }

        static Spec Female(string prefab, string display, string hair, Color color, params string[] extra)
        {
            var parts = new List<string> { "Male_emotion_usual_001", "Male_emotion_angry_003", "Male_emotion_happy_002" };
            parts.AddRange(extra);
            return new Spec(prefab, display, 1f, parts.ToArray()) { hair = hair, hairColor = color };
        }

        struct Blob
        {
            public Vector3 pos, size, euler;
            public Blob(Vector3 p, Vector3 s, Vector3 e) { pos = p; size = s; euler = e; }
        }

        static Material HairMat(string name, Color c)
        {
            Directory.CreateDirectory("Assets/Materials/Characters");
            string path = "Assets/Materials/Characters/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Cabelo low-poly feito de elipsoides, preso ao osso da cabeça (coordenadas no pose de bind, personagem na origem).</summary>
        static void AddHair(GameObject root, Transform head, Spec s)
        {
            var sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            var unit = sphere.bounds.size;
            var z = Vector3.zero;
            var blobs = new List<Blob>
            {
                new Blob(new Vector3(0f, 1.76f, -0.05f),    new Vector3(0.32f, 0.28f, 0.32f), z),   // capa
                new Blob(new Vector3(0.150f, 1.64f, -0.03f), new Vector3(0.07f, 0.20f, 0.20f), z),  // lateral
                new Blob(new Vector3(-0.150f, 1.64f, -0.03f), new Vector3(0.07f, 0.20f, 0.20f), z),
                new Blob(new Vector3(0f, 1.795f, 0.045f),   new Vector3(0.27f, 0.09f, 0.17f), z),   // franja
            };
            switch (s.hair)
            {
                case "long":
                    blobs.Add(new Blob(new Vector3(0f, 1.50f, -0.14f), new Vector3(0.30f, 0.62f, 0.16f), z));
                    break;
                case "ponytail":
                    blobs.Add(new Blob(new Vector3(0f, 1.73f, -0.19f), new Vector3(0.11f, 0.11f, 0.11f), z));
                    blobs.Add(new Blob(new Vector3(0f, 1.56f, -0.25f), new Vector3(0.10f, 0.34f, 0.10f), new Vector3(-12f, 0f, 0f)));
                    break;
                case "buns":
                    blobs.Add(new Blob(new Vector3(0.125f, 1.89f, -0.05f), new Vector3(0.13f, 0.13f, 0.13f), z));
                    blobs.Add(new Blob(new Vector3(-0.125f, 1.89f, -0.05f), new Vector3(0.13f, 0.13f, 0.13f), z));
                    break;
            }
            var combine = new CombineInstance[blobs.Count];
            for (int i = 0; i < blobs.Count; i++)
            {
                var b = blobs[i];
                combine[i] = new CombineInstance
                {
                    mesh = sphere,
                    transform = head.worldToLocalMatrix * Matrix4x4.TRS(b.pos, Quaternion.Euler(b.euler), new Vector3(b.size.x / unit.x, b.size.y / unit.y, b.size.z / unit.z))
                };
            }
            var mesh = new Mesh { name = "hair_" + s.prefab };
            mesh.CombineMeshes(combine, true, true);
            mesh.RecalculateBounds();
            Directory.CreateDirectory("Assets/Characters/Meshes");
            string mp = "Assets/Characters/Meshes/hair_" + s.prefab + ".asset";
            AssetDatabase.DeleteAsset(mp);
            AssetDatabase.CreateAsset(mesh, mp);

            var go = new GameObject("Cabelo");
            go.transform.SetParent(head, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = HairMat("Hair_" + s.prefab, s.hairColor);
        }

        static GameObject Load(string name)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(FbxDir + name + ".fbx");
            if (go == null) Debug.LogError("FBX não encontrado: " + name);
            return go;
        }

        static void Build(Spec s)
        {
            var root = (GameObject)UnityEngine.Object.Instantiate(Load("Body_010"));
            root.name = s.prefab;

            var map = new Dictionary<string, Transform>();
            foreach (var t in root.GetComponentsInChildren<Transform>()) map[t.name] = t;
            var rootBone = map["Root"];

            var actor = root.AddComponent<CharacterActor>();
            actor.displayName = s.display;

            foreach (var part in s.parts)
            {
                var src = (GameObject)UnityEngine.Object.Instantiate(Load(part));
                var smr = src.GetComponentInChildren<SkinnedMeshRenderer>();
                var bones = smr.bones;
                var nb = new Transform[bones.Length];
                for (int i = 0; i < bones.Length; i++)
                    nb[i] = bones[i] != null && map.ContainsKey(bones[i].name) ? map[bones[i].name] : rootBone;

                var go = new GameObject(part);
                go.transform.SetParent(root.transform, false);
                var n = go.AddComponent<SkinnedMeshRenderer>();
                n.sharedMesh = smr.sharedMesh;
                n.sharedMaterials = smr.sharedMaterials;
                n.bones = nb;
                n.rootBone = rootBone;
                n.localBounds = smr.localBounds;
                n.quality = SkinQuality.Bone4;
                UnityEngine.Object.DestroyImmediate(src);

                if (part.StartsWith("Male_emotion_usual")) actor.faceNeutral = go;
                else if (part.StartsWith("Male_emotion_angry")) actor.faceAngry = go;
                else if (part.StartsWith("Male_emotion_happy")) actor.faceHappy = go;
            }
            if (s.hair != null) AddHair(root, map["Head"], s);
            if (actor.faceAngry) actor.faceAngry.SetActive(false);
            if (actor.faceHappy) actor.faceHappy.SetActive(false);

            root.transform.localScale = Vector3.one * s.scale;
            PrefabUtility.SaveAsPrefabAsset(root, OutDir + s.prefab + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
