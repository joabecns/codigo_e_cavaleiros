using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CodigoECavaleiros.EditorTools
{
    /// <summary>Cria o Animator Controller (Idle, Dor, Raiva, Comemorar, Lamentar).</summary>
    public static class AnimatorBuilder
    {
        const string Dir = "Assets/Animations/KayKit/Rig_Medium/";
        const string GenDir = "Assets/Animations/Generated/";
        const string Path = "Assets/Animations/CharacterAnimator.controller";

        static AnimationClip Find(string file, string clipName)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Dir + file))
                if (o is AnimationClip c && c.name == clipName) return c;
            Debug.LogError("Clipe não encontrado: " + clipName);
            return null;
        }

        static AnimationClip Gen(string name)
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(GenDir + name + ".anim");
        }

        static void AnyTo(AnimatorController ctrl, AnimatorState target, string trigger)
        {
            var t = ctrl.layers[0].stateMachine.AddAnyStateTransition(target);
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            t.hasExitTime = false; t.duration = 0.15f; t.canTransitionToSelf = false;
        }

        [MenuItem("Tools/Código & Cavaleiros/Construir Animator")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Animations");
            if (File.Exists(Path)) AssetDatabase.DeleteAsset(Path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(Path);
            foreach (var p in new[] { "Idle", "Dor", "Raiva", "Comemorar", "Lamentar" })
                ctrl.AddParameter(p, AnimatorControllerParameterType.Trigger);
            var sm = ctrl.layers[0].stateMachine;

            var idle = sm.AddState("Idle"); idle.motion = Find("Rig_Medium_General.fbx", "Idle_A");
            var dor = sm.AddState("Dor"); dor.motion = Find("Rig_Medium_General.fbx", "Hit_B");
            var raiva = sm.AddState("Raiva");
            raiva.motion = Gen("Rage") ?? Find("Rig_Medium_Special.fbx", "Skeletons_Taunt");
            var comemorar = sm.AddState("Comemorar"); comemorar.motion = Find("Rig_Medium_Simulation.fbx", "Cheering");
            var lamDown = sm.AddState("Lamentar_Descer");
            lamDown.motion = Gen("Lament_Down") ?? Find("Rig_Medium_Simulation.fbx", "Sit_Floor_Down");
            var lamLoop = sm.AddState("Lamentar_Loop");
            lamLoop.motion = Gen("Lament_Loop") ?? Find("Rig_Medium_Simulation.fbx", "Sit_Floor_Idle");
            sm.defaultState = idle;

            var back = dor.AddTransition(idle);
            back.hasExitTime = true; back.exitTime = 0.95f; back.duration = 0.15f;
            var lam = lamDown.AddTransition(lamLoop);
            lam.hasExitTime = true; lam.exitTime = 0.98f; lam.duration = 0.1f;

            AnyTo(ctrl, idle, "Idle");
            AnyTo(ctrl, dor, "Dor");
            AnyTo(ctrl, raiva, "Raiva");
            AnyTo(ctrl, comemorar, "Comemorar");
            AnyTo(ctrl, lamDown, "Lamentar");

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();

            // aplica o controller nos prefabs
            foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Characters" }))
            {
                var pp = AssetDatabase.GUIDToAssetPath(g);
                var root = PrefabUtility.LoadPrefabContents(pp);
                root.GetComponent<Animator>().runtimeAnimatorController = ctrl;
                PrefabUtility.SaveAsPrefabAsset(root, pp);
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("[AnimatorBuilder] Controller criado e aplicado aos prefabs.");
        }
    }
}
