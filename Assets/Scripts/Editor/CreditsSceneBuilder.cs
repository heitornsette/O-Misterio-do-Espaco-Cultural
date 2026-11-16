#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Misterio.Credits;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Misterio.EditorTools
{
    public static class CreditsSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Credits.unity";
        const string CC01ArtPath = "Assets/Art/CC01";
        static Font uiFont;

        [MenuItem("Misterio/Build Credits Scene")]
        public static void Build()
        {
            EnsureFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Camera - Credits diorama");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.035f, 0.09f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();

            var bg = SpriteRendererObject("Saguão - arte de fundo",
                LoadSprite("Assets/Art/Backgrounds/foyer_night_384x216.png"),
                new Vector3(0f, 0f, 5f), 0, 108f);

            Sprite mothSprite = LoadSprite($"{CC01ArtPath}/moth.asset");
            Sprite mothGlowSprite = LoadSprite($"{CC01ArtPath}/moth_glow.asset");
            Sprite dustSprite = LoadSprite($"{CC01ArtPath}/dust_mote.asset");
            Sprite vignetteSprite = LoadSprite($"{CC01ArtPath}/cinematic_vignette.asset");

            var moth = SpriteRendererObject("Mariposa - deriva dos créditos", mothSprite,
                new Vector3(-1.6f, 0.35f, -0.1f), 12, 108f);
            moth.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            var mothGlow = SpriteRendererObject("Mariposa - brilho dos créditos", mothGlowSprite,
                new Vector3(-1.6f, 0.35f, -0.12f), 11, 108f);
            mothGlow.transform.localScale = new Vector3(0.60f, 0.60f, 1f);

            var dustMotes = new SpriteRenderer[6];
            for (int i = 0; i < dustMotes.Length; i++)
            {
                var pos = new Vector3(-1.5f + (i % 3) * 1.1f, -0.4f + (i / 3) * 0.9f, -0.18f);
                dustMotes[i] = SpriteRendererObject($"Poeira créditos {i + 1:00}", dustSprite, pos, 6, 108f);
                dustMotes[i].transform.localScale = Vector3.one * 0.34f;
                dustMotes[i].color = new Color(0.82f, 0.84f, 0.76f, 0.18f);
            }

            uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasObject = new GameObject("Credits UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(384f, 216f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var vignette = MakeImage(canvas.transform, "Cinematic vignette",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            vignette.sprite = vignetteSprite;
            vignette.raycastTarget = false;
            vignette.transform.SetAsFirstSibling();

            var darkOverlay = MakeImage(canvas.transform, "Dark overlay for readability",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.015f, 0.035f, 0.09f, 0.45f));
            darkOverlay.raycastTarget = false;

            var creditsRoot = new GameObject("Credits root", typeof(RectTransform));
            creditsRoot.transform.SetParent(canvas.transform, false);
            var rootRect = creditsRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(360f, 420f);
            rootRect.anchoredPosition = new Vector2(0f, -320f);

            var ivory = new Color(0.925f, 0.91f, 0.87f, 1f);     // #ece8de
            var cream = new Color(0.988f, 0.964f, 0.866f, 1f);   // #fcf6de
            var gold = new Color(0.733f, 0.533f, 0.396f, 1f);    // #bb8865
            var light = new Color(0.808f, 0.784f, 0.737f, 1f);   // #cec8bc
            var muted = new Color(0.620f, 0.435f, 0.250f, 1f);   // #9b6540

            var title = MakeCreditsText(rootRect, "Title", 30, 46, 320, 15, ivory, TextAnchor.MiddleCenter, true);
            title.text = "O MISTÉRIO DO\nESPAÇO CULTURAL";
            var titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0.165f, 0.2f, 0.357f, 0.9f); // #2a335b
            titleShadow.effectDistance = new Vector2(1f, -1f);

            MakeCreditsText(rootRect, "Subtitle", 82, 14, 220, 8, muted, TextAnchor.MiddleCenter, false).text = "uma produção";

            MakeCreditsText(rootRect, "Section - Equipe", 108, 16, 220, 10, gold, TextAnchor.MiddleCenter, true).text = "EQUIPE";
            MakeCreditsText(rootRect, "Savas", 128, 14, 340, 9, light, TextAnchor.MiddleCenter, false).text = "Savas Neto — código e cutscenes";
            MakeCreditsText(rootRect, "Levi", 146, 14, 340, 9, light, TextAnchor.MiddleCenter, false).text = "Levi de Pontes — ambientação, efeitos sonoros e visuais";
            MakeCreditsText(rootRect, "Heitor", 164, 14, 340, 9, light, TextAnchor.MiddleCenter, false).text = "Heitor Sette — arte e roteiro";
            MakeCreditsText(rootRect, "Natalia", 182, 14, 340, 9, light, TextAnchor.MiddleCenter, false).text = "Natalia Pacheco — desenvolvimento de cenários";

            MakeCreditsText(rootRect, "Section - Disciplina", 212, 16, 220, 10, gold, TextAnchor.MiddleCenter, true).text = "DISCIPLINA";
            MakeCreditsText(rootRect, "Course", 232, 14, 340, 9, light, TextAnchor.MiddleCenter, false).text = "N394-50 — Computação Gráfica e Realidade Virtual";
            MakeCreditsText(rootRect, "University", 250, 14, 340, 9, light, TextAnchor.MiddleCenter, false).text = "Universidade de Fortaleza · 2026";

            MakeCreditsText(rootRect, "Section - Orientacao", 280, 16, 220, 10, gold, TextAnchor.MiddleCenter, true).text = "ORIENTAÇÃO";
            MakeCreditsText(rootRect, "Professor", 300, 14, 340, 9, light, TextAnchor.MiddleCenter, false).text = "Prof. Thiago Narak";

            var thanks = MakeCreditsText(rootRect, "Thanks", 340, 20, 320, 12, gold, TextAnchor.MiddleCenter, true);
            thanks.text = "OBRIGADO POR JOGAR";
            var thanksShadow = thanks.gameObject.AddComponent<Shadow>();
            thanksShadow.effectColor = new Color(0.165f, 0.2f, 0.357f, 0.8f);
            thanksShadow.effectDistance = new Vector2(1f, -1f);

            var hint = MakeText(canvas.transform, "Skip hint",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-120f, 8f), new Vector2(120f, 20f),
                8, TextAnchor.MiddleCenter, muted);
            hint.text = "clique ou ESC para pular";
            hint.raycastTarget = false;
            hint.enabled = false;

            var endCard = MakeText(canvas.transform, "End card - O misterio continua",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-160f, -16f), new Vector2(160f, 16f),
                13, TextAnchor.MiddleCenter, cream);
            endCard.text = "O MISTÉRIO CONTINUA...";
            endCard.fontStyle = FontStyle.Bold;
            var endShadow = endCard.gameObject.AddComponent<Shadow>();
            endShadow.effectColor = new Color(0.733f, 0.533f, 0.396f, 0.85f);
            endShadow.effectDistance = new Vector2(1f, -1f);
            endCard.raycastTarget = false;
            endCard.enabled = false;

            var fade = MakeImage(canvas.transform, "Opening fade to black",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black);
            fade.transform.SetAsLastSibling();
            endCard.transform.SetAsLastSibling();

            var audioObject = new GameObject("Credits audio slots");
            var ambientSource = audioObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            var musicSource = audioObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;

            var controllerObject = new GameObject("Credits - rolagem cinematográfica");
            var controller = controllerObject.AddComponent<CreditsRollController>();
            SetObject(controller, "sceneCamera", camera);
            SetObject(controller, "creditsRoot", rootRect);
            SetObject(controller, "moth", moth);
            SetObject(controller, "mothGlow", mothGlow);
            SetObjectArray(controller, "dustMotes", dustMotes);
            SetObject(controller, "hint", hint);
            SetObject(controller, "endCard", endCard);
            SetObject(controller, "vignetteImage", vignette);
            SetObject(controller, "fadeImage", fade);
            SetObject(controller, "ambientSource", ambientSource);
            SetObject(controller, "musicSource", musicSource);

            EditorSceneManager.SaveScene(scene, ScenePath);
            // Append the credits scene without touching the existing build list.
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == ScenePath))
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Credits scene built at Assets/Scenes/Credits.unity. Press Play to preview; onCreditsFinished is ready for a future main menu.");
        }

        static void EnsureFolders()
        {
            foreach (var path in new[] { "Assets/Scripts/Credits", "Assets/Scenes" })
                if (!AssetDatabase.IsValidFolder(path))
                {
                    var parent = Path.GetDirectoryName(path).Replace('\\', '/');
                    var name = Path.GetFileName(path);
                    if (!AssetDatabase.IsValidFolder(parent)) Directory.CreateDirectory(parent);
                    AssetDatabase.CreateFolder(parent, name);
                }
        }

        static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        static SpriteRenderer SpriteRendererObject(string name, Sprite sprite, Vector3 position, int order, float ppu)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        static Text MakeText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int size, TextAnchor align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            var text = go.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = size;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        static Text MakeCreditsText(RectTransform parent, string name, int yFromTop, int height, int width, int size, Color color, TextAnchor align, bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(-width / 2f, -yFromTop - height);
            rect.offsetMax = new Vector2(width / 2f, -yFromTop);
            var text = go.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = size;
            text.alignment = align;
            text.color = color;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        static Image MakeImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void SetObject<T>(Object target, string fieldName, T value) where T : Object
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetObjectArray<T>(Object target, string fieldName, T[] values) where T : Object
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null) return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
