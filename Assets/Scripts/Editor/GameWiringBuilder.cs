using System.Collections.Generic;
using Misterio.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Misterio.EditorTools
{
    /// <summary>
    /// Adds the persistent bootstrap to every scene and rewrites the build list in play order, so the
    /// whole game runs from CC01 to the credits without any manual wiring.
    /// </summary>
    public static class GameWiringBuilder
    {
        /// <summary>Play order: the success route plus the two time-out endings.</summary>
        static readonly string[] Order =
        {
            GameFlow.Exterior,
            GameFlow.Cutscene01,
            GameFlow.Floor1,
            GameFlow.Cutscene02,
            GameFlow.Floor2,
            GameFlow.Cutscene03,
            GameFlow.Floor3,
            GameFlow.Cutscene04A,
            GameFlow.Cutscene04,
            GameFlow.Cutscene05,
            GameFlow.Floor4,
            GameFlow.Cutscene06,
            GameFlow.Cutscene07,
            GameFlow.Cutscene08,
            GameFlow.Cutscene09,
            GameFlow.Cutscene10,
            GameFlow.Credits
        };

        [MenuItem("Misterio/Wire Game (bootstrap + build order)")]
        public static void Wire()
        {
            foreach (string sceneName in Order)
            {
                string path = "Assets/Scenes/" + sceneName + ".unity";
                if (!System.IO.File.Exists(path))
                {
                    Debug.LogWarning("[Wire] missing scene " + path);
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                EnsureBootstrap(sceneName);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, path);
                Debug.Log("[Wire] wired " + sceneName);
            }

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string sceneName in Order)
                scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/" + sceneName + ".unity", true));
            // Keep the standalone prototype available, disabled, for quick movement checks.
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Gameplay_Prototype.unity", false));
            EditorBuildSettings.scenes = scenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Wire] build order updated: " + Order.Length + " scenes");
            EditorApplication.Exit(0);
        }

        static void EnsureBootstrap(string sceneName)
        {
            GameBootstrap existing = null;
            foreach (var behaviour in Object.FindObjectsByType<GameBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                existing = behaviour;
                break;
            }

            if (existing == null)
            {
                var host = new GameObject("Game Bootstrap");
                existing = host.AddComponent<GameBootstrap>();
            }

            var serialized = new SerializedObject(existing);
            var startNew = serialized.FindProperty("startNewRunOnAwake");
            if (startNew != null) startNew.boolValue = sceneName == GameFlow.Cutscene01;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
