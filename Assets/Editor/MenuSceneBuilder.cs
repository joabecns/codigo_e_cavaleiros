using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CodigoECavaleiros.UI;

namespace CodigoECavaleiros.EditorTools
{
    /// <summary>Monta a cena "Menu" (pódio + herói girando + MenuController) e ajusta o Build Settings.</summary>
    public static class MenuSceneBuilder
    {
        const string MenuPath = "Assets/Scenes/Menu.unity";
        const string BattlePath = "Assets/Scenes/Batalha.unity";
        static readonly string[] Heroes = { "Hero_Leo", "Hero_Theo", "Hero_Bruno", "Hero_Max", "Hero_Caio" };

        [MenuItem("Tools/Código & Cavaleiros/Construir Cena de Menu")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var light = Object.FindAnyObjectByType<Light>();
            light.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;

            // pódio
            var pod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pod.name = "Podio";
            pod.transform.position = new Vector3(0, -0.05f, 0);
            pod.transform.localScale = new Vector3(2.6f, 0.05f, 2.6f);
            pod.GetComponent<MeshRenderer>().sharedMaterial = LoadMat("PlataformaHeroi");
            Object.DestroyImmediate(pod.GetComponent<Collider>());
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "PodioBase";
            ring.transform.position = new Vector3(0, -0.15f, 0);
            ring.transform.localScale = new Vector3(3.1f, 0.05f, 3.1f);
            ring.GetComponent<MeshRenderer>().sharedMaterial = LoadMat("PlataformaVilao");
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            var spot = new GameObject("PodiumSpot").transform;
            spot.position = Vector3.zero;
            spot.rotation = Quaternion.identity; // frente do modelo (+Z) voltada para a câmera

            var cam = Camera.main;
            cam.transform.position = new Vector3(0f, 1.25f, 5.6f);
            cam.transform.rotation = Quaternion.Euler(6f, 180f, 0f);
            cam.fieldOfView = 38f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.10f, 0.22f);

            var go = new GameObject("MenuController");
            var mc = go.AddComponent<MenuController>();
            mc.podium = spot; mc.cam = cam;
            mc.heroPrefabs = new GameObject[Heroes.Length];
            for (int i = 0; i < Heroes.Length; i++)
                mc.heroPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/" + Heroes[i] + ".prefab");

            EditorSceneManager.SaveScene(scene, MenuPath);

            // Build Settings: Menu (0) e Batalha (1)
            var list = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(BattlePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();

            PlayerSettings.productName = "Caçadores de Bugs";
            AssetDatabase.SaveAssets();
            Debug.Log("[MenuSceneBuilder] Cena de menu criada e Build Settings atualizado.");
        }

        static Material LoadMat(string n)
        {
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Battle/" + n + ".mat");
        }
    }
}
