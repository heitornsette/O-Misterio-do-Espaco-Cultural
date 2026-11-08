using Misterio.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace Misterio.EditorTools
{
    public static class GuardGameplaySceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Gameplay_Prototype.unity";
        const string ArtPath = "Assets/Art/CC01/";

        [MenuItem("Misterio/Build Gameplay Prototype")]
        public static void Build()
        {
            Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Backgrounds/foyer_night_384x216.png");
            if (backgroundSprite == null)
                throw new System.InvalidOperationException("Could not load the foyer background sprite.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Camera - Gameplay");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.035f, 0.09f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();

            var backgroundObject = new GameObject("Saguão - fundo");
            backgroundObject.transform.position = new Vector3(0f, 0f, 5f);
            var backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = backgroundSprite;
            backgroundRenderer.sortingOrder = -10;

            var guardObject = new GameObject("Segurança - jogador");
            guardObject.transform.position = new Vector3(-0.55f, -0.34f, 0f);
            guardObject.transform.localScale = new Vector3(0.48f, 0.48f, 1f);
            var guardRenderer = guardObject.AddComponent<SpriteRenderer>();
            guardRenderer.sprite = LoadFrame("GuardIdleFront", 0);
            guardRenderer.sortingOrder = 10;

            var movement = guardObject.AddComponent<GuardMovementController>();
            var serialized = new SerializedObject(movement);
            serialized.FindProperty("inputActions").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            serialized.FindProperty("spriteRenderer").objectReferenceValue = guardRenderer;
            serialized.FindProperty("movementMin").vector2Value = new Vector2(-1.65f, -0.8f);
            serialized.FindProperty("movementMax").vector2Value = new Vector2(1.65f, 0.45f);
            SetFrames(serialized.FindProperty("idleFront"), "GuardIdleFront", 5);
            SetFrames(serialized.FindProperty("idleBack"), "GuardIdleBack", 5);
            SetFrames(serialized.FindProperty("walkFront"), "GuardWalkFront", 8);
            SetFrames(serialized.FindProperty("walkBack"), "GuardWalkBack", 8);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();

            var scenes = EditorBuildSettings.scenes;
            bool alreadyIncluded = false;
            foreach (var scene in scenes)
                if (scene.path == ScenePath) alreadyIncluded = true;
            if (!alreadyIncluded)
            {
                var updated = new EditorBuildSettingsScene[scenes.Length + 1];
                scenes.CopyTo(updated, 0);
                updated[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
                EditorBuildSettings.scenes = updated;
            }

            AssetDatabase.Refresh();
            Debug.Log("Gameplay prototype created. Use WASD, arrow keys, or the gamepad left stick to move the guard.");
        }

        static Sprite LoadFrame(string prefix, int index)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                $"{ArtPath}{prefix}_{index:00}.asset");
            if (sprite == null)
                throw new System.InvalidOperationException($"Could not load sprite {prefix}_{index:00}.");
            return sprite;
        }

        static void SetFrames(SerializedProperty property, string prefix, int count)
        {
            property.arraySize = count;
            for (int i = 0; i < count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = LoadFrame(prefix, i);
        }
    }
}
