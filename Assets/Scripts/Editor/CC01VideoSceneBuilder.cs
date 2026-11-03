using System.IO;
using Misterio.Cutscenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Misterio.EditorTools
{
    public static class CC01VideoSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/CC01_A_Madrugada_3D.unity";

        [MenuItem("Misterio/Build CC01 - vídeo 3D")]
        public static void Build()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/StreamingAssets");
            EnsureFolder("Assets/StreamingAssets/Cinematics");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("CC01 Video Camera", typeof(Camera), typeof(AudioListener), typeof(VideoPlayer));
            var camera = cameraObject.GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var player = cameraObject.GetComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = false;
            player.waitForFirstFrame = true;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.renderMode = VideoRenderMode.CameraNearPlane;
            player.targetCamera = camera;
            player.targetCameraAlpha = 1f;
            player.aspectRatio = VideoAspectRatio.FitInside;

            var canvasObject = new GameObject("CC01 Captions", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            canvas.transform.localScale = Vector3.one;

            var backingObject = new GameObject("Subtitle backing", typeof(RectTransform), typeof(Image));
            backingObject.transform.SetParent(canvas.transform, false);
            var backingRect = backingObject.GetComponent<RectTransform>();
            backingRect.anchorMin = new Vector2(0f, 0f);
            backingRect.anchorMax = new Vector2(1f, 0.17f);
            backingRect.offsetMin = Vector2.zero;
            backingRect.offsetMax = Vector2.zero;
            backingObject.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.055f, 0.68f);

            var textObject = new GameObject("Dialogue subtitles", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(canvas.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.035f, 0.015f);
            textRect.anchorMax = new Vector2(0.965f, 0.15f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var subtitle = textObject.GetComponent<Text>();
            subtitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subtitle.fontSize = 27;
            subtitle.fontStyle = FontStyle.Normal;
            subtitle.alignment = TextAnchor.MiddleCenter;
            subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitle.verticalOverflow = VerticalWrapMode.Overflow;
            subtitle.supportRichText = true;
            subtitle.color = new Color(0.94f, 0.97f, 1f);
            subtitle.raycastTarget = false;
            var shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            shadow.effectDistance = new Vector2(2f, -2f);

            var voiceObject = new GameObject("Dialogue voice source", typeof(AudioSource));
            voiceObject.transform.SetParent(cameraObject.transform, false);
            var voice = voiceObject.GetComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            var ambient = cameraObject.AddComponent<AudioSource>();
            var effects = cameraObject.AddComponent<AudioSource>();
            var music = cameraObject.AddComponent<AudioSource>();
            ambient.playOnAwake = false;
            effects.playOnAwake = false;
            music.playOnAwake = false;

            var controller = cameraObject.AddComponent<CC01VideoCutsceneController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("subtitleText").objectReferenceValue = subtitle;
            serialized.FindProperty("voiceSource").objectReferenceValue = voice;
            serialized.FindProperty("ambientSource").objectReferenceValue = ambient;
            serialized.FindProperty("effectsSource").objectReferenceValue = effects;
            serialized.FindProperty("musicSource").objectReferenceValue = music;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CC01 video scene built. Place the exported VP8 WebM at Assets/StreamingAssets/Cinematics/CC01_A_Madrugada_Comeca.webm.");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
