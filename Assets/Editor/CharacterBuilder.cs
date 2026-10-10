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
            if (actor.faceAngry) actor.faceAngry.SetActive(false);
            if (actor.faceHappy) actor.faceHappy.SetActive(false);

            root.transform.localScale = Vector3.one * s.scale;
            PrefabUtility.SaveAsPrefabAsset(root, OutDir + s.prefab + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
