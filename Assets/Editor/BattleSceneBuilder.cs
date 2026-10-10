using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CodigoECavaleiros.Combat;
using CodigoECavaleiros.Entities;

namespace CodigoECavaleiros.EditorTools
{
    /// <summary>Monta a cena "Batalha": plataformas, luz, câmera diagonal estilo Pokémon e o BattleController.</summary>
    public static class BattleSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Batalha.unity";
        const string MatDir = "Assets/Materials/Battle/";
        static readonly string[] Heroes = { "Hero_Leo", "Hero_Theo", "Hero_Bruno", "Hero_Max", "Hero_Caio" };

        static Material Mat(string name, Color c)
        {
            Directory.CreateDirectory(MatDir);
            string path = MatDir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(m);
            return m;
        }

        [MenuItem("Tools/Código & Cavaleiros/Construir Cena de Batalha")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // luz
            var light = Object.FindAnyObjectByType<Light>();
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;

            // chão e plataformas
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Chao";
            ground.transform.localScale = new Vector3(5, 1, 5);
            ground.GetComponent<MeshRenderer>().sharedMaterial = Mat("Chao", new Color(0.18f, 0.32f, 0.28f));

            Vector3 heroPos = new Vector3(-1.9f, 0.1f, -1.9f);
            Vector3 enemyPos = new Vector3(2.5f, 0.1f, 2.5f);
            MakePlatform("PlataformaHeroi", heroPos, 1.9f, Mat("PlataformaHeroi", new Color(0.30f, 0.45f, 0.85f)));
            MakePlatform("PlataformaVilao", enemyPos, 2.3f, Mat("PlataformaVilao", new Color(0.65f, 0.25f, 0.30f)));

            var heroSpot = new GameObject("HeroSpot").transform;
            var enemySpot = new GameObject("EnemySpot").transform;
            heroSpot.position = heroPos; enemySpot.position = enemyPos;
            Vector3 dir = (enemyPos - heroPos); dir.y = 0; dir.Normalize();
            heroSpot.rotation = Quaternion.LookRotation(dir);
            enemySpot.rotation = Quaternion.LookRotation(-dir);

            // câmera diagonal, na altura do ombro do herói, olhando por cima dele
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            var cam = Camera.main;
            cam.fieldOfView = 42f;
            cam.transform.position = heroPos - dir * 4.1f + side * 1.3f + Vector3.up * 1.5f;
            Vector3 target = Vector3.Lerp(heroPos, enemyPos, 0.6f) + Vector3.up * 0.6f;
            cam.transform.LookAt(target);
            cam.backgroundColor = new Color(0.45f, 0.62f, 0.85f);

            // controlador
            var go = new GameObject("BattleController");
            var bc = go.AddComponent<BattleController>();
            bc.heroSpot = heroSpot; bc.enemySpot = enemySpot;
            bc.heroPrefabs = new GameObject[Heroes.Length];
            for (int i = 0; i < Heroes.Length; i++)
                bc.heroPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/" + Heroes[i] + ".prefab");
            bc.enemyPrefabs = new GameObject[Settings.TotalStages];
            for (int i = 0; i < Settings.TotalStages; i++)
                bc.enemyPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyCatalog.PrefabPath(i));

            EditorSceneManager.SaveScene(scene, ScenePath);

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == ScenePath)) list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[BattleSceneBuilder] Cena criada: " + ScenePath);
        }

        static void MakePlatform(string name, Vector3 pos, float radius, Material mat)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p.name = name;
            p.transform.position = new Vector3(pos.x, 0.05f, pos.z);
            p.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
            p.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(p.GetComponent<Collider>());
        }
    }
}
