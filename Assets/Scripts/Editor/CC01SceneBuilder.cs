#if UNITY_EDITOR
using System.IO;
using Misterio.Cutscenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Misterio.EditorTools
{
    public static class CC01SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/CC01_A_Madrugada_Comeca.unity";
        const string ArtPath = "Assets/Art/CC01";
        const string Characters = "Assets/Art/Characters";
        static Font uiFont;

        [MenuItem("Misterio/Build CC01 - A madrugada começa")]
        public static void Build()
        {
            EnsureFolders();
            ImportPixelArt();
            Sprite[] idleFront = SliceSheet("guarda_idle_frente_sheet.png", 5, "GuardIdleFront");
            Sprite[] idleBack = SliceSheet("guarda_idle_costas_sheet.png", 5, "GuardIdleBack");
            Sprite[] walkFront = SliceSheet("guarda_andar_frente_sheet.png", 8, "GuardWalkFront");
            Sprite[] walkBack = SliceSheet("guarda_andar_costas_sheet.png", 8, "GuardWalkBack");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Camera - Fixed diorama 384x216");
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.035f, 0.09f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();

            var bg = SpriteRendererObject("Saguão - arte de fundo", LoadSprite("Assets/Art/Backgrounds/foyer_night_384x216.png"), Vector3.zero, 0, 108f);
            bg.transform.position = new Vector3(0f, 0f, 5f);
            bg.transform.localScale = Vector3.one;

            Sprite footprintSprite = CreatePixelSprite("footprint_three_toe_v4", 44, 36, (x, y) => DrawThreeToedFootprint(x, y));
            Sprite mothSprite = CreatePixelSprite("moth", 24, 16, (x, y) => DrawMothPixels(x, y));
            Sprite mothGlowSprite = CreatePixelSprite("moth_glow", 24, 24, (x, y) => DrawMothGlowPixels(x, y));
            Sprite featherSprite = CreatePixelSprite("feather", 20, 32, (x, y) => DrawFeatherPixels(x, y));
            Sprite coffeeTableSprite = CreatePixelSprite("coffee_table", 48, 32, (x, y) => DrawCoffeeTablePixels(x, y));
            Sprite cupSprite = CreatePixelSprite("coffee_cup", 24, 20, (x, y) => DrawCupPixels(x, y));
            Sprite steamSprite = CreatePixelSprite("coffee_steam", 6, 14, (x, y) => DrawSteamPixels(x, y));
            Sprite dustSprite = CreatePixelSprite("dust_mote", 4, 4, (x, y) => DrawDustPixels(x, y));
            Sprite beamSprite = CreatePixelSprite("flashlight_cone_v2", 112, 56, (x, y) => DrawBeamPixels(x, y));
            Sprite vignetteSprite = CreatePixelSprite("cinematic_vignette", 384, 216, (x, y) => DrawVignettePixels(x, y));
            Sprite glowSprite = CreatePixelSprite("door_warm_glow", 48, 64, (x, y) => DrawGlowPixels(x, y));
            Sprite stairFlashSprite = CreatePixelSprite("stair_lamp_flash", 24, 10, (x, y) => Inside(x, y, 1, 2, 22, 7) ? new Color(0.62f, 0.86f, 1f, 1f) : Color.clear);

            var trackPositions = new[]
            {
                new Vector3(-0.24f, -0.63f, 0f), new Vector3(0.15f, -0.49f, 0f),
                new Vector3(0.55f, -0.29f, 0f), new Vector3(0.96f, -0.10f, 0f)
            };
            var tracks = new SpriteRenderer[trackPositions.Length];
            for (int i = 0; i < tracks.Length; i++)
            {
                tracks[i] = SpriteRendererObject($"Pista - pegada de três dedos {i + 1}", footprintSprite, trackPositions[i], 8, 108f);
                tracks[i].transform.localScale = new Vector3(0.35f, 0.35f, 1f);
                tracks[i].transform.rotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? -26f : -36f);
                tracks[i].color = new Color(0.70f, 0.68f, 0.62f, 0.82f);
                tracks[i].enabled = false;
            }
            var feather = SpriteRendererObject("Pista - pena cinza", featherSprite, new Vector3(1.24f, -0.37f, 0f), 7, 108f);
            feather.transform.localScale = new Vector3(0.36f, 0.36f, 1f);
            feather.enabled = false;
            var moth = SpriteRendererObject("Mariposa - passagem breve", mothSprite, Vector3.zero, 12, 108f);
            moth.transform.localScale = new Vector3(0.50f, 0.50f, 1f);
            moth.enabled = false;
            var mothGlow = SpriteRendererObject("Mariposa - brilho mínimo", mothGlowSprite, Vector3.zero, 11, 108f);
            mothGlow.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            mothGlow.enabled = false;
            var coffeeTable = SpriteRendererObject("Mesa lateral para o café", coffeeTableSprite, new Vector3(-1.12f, -0.31f, 0.05f), 6, 108f);
            coffeeTable.transform.localScale = new Vector3(0.82f, 0.82f, 1f);
            var cup = SpriteRendererObject("Pista de café", cupSprite, new Vector3(-1.12f, -0.185f, -0.02f), 7, 108f);
            cup.transform.localScale = new Vector3(0.54f, 0.54f, 1f);
            cup.color = new Color(0.94f, 0.84f, 0.67f, 1f);
            var steam = new SpriteRenderer[3];
            for (int i = 0; i < steam.Length; i++)
            {
                steam[i] = SpriteRendererObject($"Café - vapor {i + 1}", steamSprite,
                    new Vector3(-1.16f + i * 0.045f, -0.14f, -0.05f), 9, 108f);
                steam[i].transform.localScale = new Vector3(0.60f, 0.60f, 1f);
                steam[i].color = new Color(0.83f, 0.89f, 0.91f, 0.55f);
                steam[i].enabled = false;
            }
            var beam = SpriteRendererObject("Lanterna - facho pixelado", beamSprite, Vector3.zero, 8, 108f);
            beam.transform.localScale = new Vector3(0.60f, 0.30f, 1f);
            beam.enabled = false;
            var doorGlow = SpriteRendererObject("Porta de serviço - luz sutil", glowSprite, new Vector3(1.42f, -0.12f, 0.1f), 6, 108f);
            doorGlow.transform.localScale = new Vector3(0.35f, 0.52f, 1f);
            doorGlow.enabled = false;
            var stairFlash = SpriteRendererObject("Escadaria - oscilação breve da luz", stairFlashSprite, new Vector3(1.12f, 0.53f, 0f), 7, 108f);
            stairFlash.transform.localScale = new Vector3(0.22f, 0.12f, 1f);
            stairFlash.enabled = false;

            var guardObject = new GameObject("Segurança - protagonist sprite");
            guardObject.transform.position = new Vector3(-0.55f, -0.34f, 0f);
            var guardRenderer = guardObject.AddComponent<SpriteRenderer>();
            guardRenderer.sprite = idleFront[0];
            guardRenderer.sortingOrder = 10;
            guardObject.transform.localScale = new Vector3(0.48f, 0.48f, 1f);

            var dustMotes = MakeMotes("Poeira ambiente", dustSprite, 10, 6, i => new Vector3(-1.65f + (i % 5) * 0.78f, -0.42f + (i / 5) * 0.92f, -0.18f), 0.34f);
            var beamDust = MakeMotes("Poeira no facho", dustSprite, 6, 9, i => new Vector3(-0.2f + i * 0.22f, -0.34f + (i % 2) * 0.12f, -0.2f), 0.52f);

            uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Cutscene UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(384f, 216f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var vignette = MakeImage(canvas.transform, "Cinematic vignette", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            vignette.sprite = vignetteSprite;
            vignette.raycastTarget = false;
            vignette.transform.SetAsFirstSibling();

            var clockPlate = MakeImage(canvas.transform, "Digital clock backing", new Vector2(0f, 1f), new Vector2(0.30f, 1f), new Vector2(8f, -34f), new Vector2(-8f, -6f), new Color(0.015f, 0.035f, 0.09f, 0.88f));
            var clockOutline = clockPlate.gameObject.AddComponent<Outline>();
            clockOutline.effectColor = new Color(0.56f, 0.75f, 0.94f, 0.9f);
            clockOutline.effectDistance = new Vector2(1f, -1f);
            var clock = MakeText(canvas.transform, "Clock - 02:00", new Vector2(0f, 1f), new Vector2(0.30f, 1f), new Vector2(12f, -32f), new Vector2(-12f, -8f), 13, TextAnchor.MiddleCenter, new Color(0.85f, 0.94f, 1f));
            clock.text = "02:00";
            clock.color = new Color(0.88f, 0.96f, 1f);
            clock.fontStyle = FontStyle.Bold;
            clock.horizontalOverflow = HorizontalWrapMode.Overflow;
            clock.raycastTarget = false;

            var captionBg = MakeImage(canvas.transform, "Subtitle dark backing", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 48f), new Color(0.015f, 0.035f, 0.09f, 0.88f));
            var caption = MakeText(canvas.transform, "Subtitles - Narrator and Guard", new Vector2(0.04f, 0f), new Vector2(0.96f, 0f), new Vector2(0f, 6f), new Vector2(0f, 39f), 10, TextAnchor.MiddleCenter, Color.white);
            caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            caption.verticalOverflow = VerticalWrapMode.Overflow;
            caption.supportRichText = true;
            caption.raycastTarget = false;

            var title = MakeText(canvas.transform, "Title - O Mistério do Espaço Cultural", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-160f, -25f), new Vector2(160f, 25f), 14, TextAnchor.MiddleCenter, Color.white);
            title.text = "O MISTÉRIO DO\nESPAÇO CULTURAL";
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.92f, 0.97f, 1f);
            var titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0.18f, 0.44f, 0.76f, 0.9f);
            titleShadow.effectDistance = new Vector2(1f, -1f);
            title.enabled = false;
            title.raycastTarget = false;

            var fade = MakeImage(canvas.transform, "Opening fade to picture", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black);
            fade.transform.SetAsLastSibling();
            title.transform.SetAsLastSibling();

            var audioObject = new GameObject("Cutscene audio slots");
            var voiceSource = audioObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            var ambientSource = audioObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            var effectsSource = audioObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            var musicSource = audioObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;

            var controllerObject = new GameObject("CC01 - A madrugada começa (35.7s)");
            var controller = controllerObject.AddComponent<CC01CutsceneController>();
            SetObject(controller, "sceneCamera", camera);
            SetObject(controller, "guard", guardObject.transform);
            SetObject(controller, "guardRenderer", guardRenderer);
            SetObjectArray(controller, "guardIdleFront", idleFront);
            SetObjectArray(controller, "guardIdleBack", idleBack);
            SetObjectArray(controller, "guardWalkFront", walkFront);
            SetObjectArray(controller, "guardWalkBack", walkBack);
            SetObject(controller, "flashlightBeam", beam);
            SetObjectArray(controller, "footprints", tracks);
            SetObject(controller, "feather", feather);
            SetObject(controller, "moth", moth);
            SetObject(controller, "mothGlow", mothGlow);
            SetObject(controller, "coffeeCup", cup);
            SetObjectArray(controller, "coffeeSteam", steam);
            SetObjectArray(controller, "dustMotes", dustMotes);
            SetObjectArray(controller, "beamDust", beamDust);
            SetObject(controller, "serviceDoorGlow", doorGlow);
            SetObject(controller, "stairLightFlash", stairFlash);
            SetObject(controller, "clockText", clock);
            SetObject(controller, "subtitleText", caption);
            SetObject(controller, "titleText", title);
            SetObject(controller, "vignetteImage", vignette);
            SetObject(controller, "fadeImage", fade);
            SetObject(controller, "voiceSource", voiceSource);
            SetObject(controller, "ambientSource", ambientSource);
            SetObject(controller, "effectsSource", effectsSource);
            SetObject(controller, "musicSource", musicSource);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CC01 scene built. Press Play to preview the 35.7-second scene; narrator/guard voice fields are intentionally empty.");
        }

        static void EnsureFolders()
        {
            foreach (var path in new[] { "Assets/Art/CC01", "Assets/Fonts", "Assets/Scripts/Editor", "Assets/Scenes" })
                if (!AssetDatabase.IsValidFolder(path))
                {
                    var parent = Path.GetDirectoryName(path).Replace('\\', '/');
                    var name = Path.GetFileName(path);
                    if (!AssetDatabase.IsValidFolder(parent)) Directory.CreateDirectory(parent);
                    AssetDatabase.CreateFolder(parent, name);
                }
        }

        static void ImportPixelArt()
        {
            var paths = new[]
            {
                "Assets/Art/Backgrounds/foyer_night_384x216.png",
                "Assets/Art/Characters/guarda_idle_frente_sheet.png",
                "Assets/Art/Characters/guarda_idle_costas_sheet.png",
                "Assets/Art/Characters/guarda_andar_frente_sheet.png",
                "Assets/Art/Characters/guarda_andar_costas_sheet.png"
            };
            foreach (string path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = path.Contains("foyer") ? 108 : 128;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            string fontSource = "/home/savas/Unity/Hub/Editor/6000.6.0f1/Editor/Data/Resources/Fonts/RobotoMono-Bold.ttf";
            if (File.Exists(fontSource) && !File.Exists("Assets/Fonts/RobotoMono-Bold.ttf"))
                File.Copy(fontSource, "Assets/Fonts/RobotoMono-Bold.ttf");
            AssetDatabase.Refresh();
        }

        static Sprite[] SliceSheet(string fileName, int count, string prefix)
        {
            string path = Characters + "/" + fileName;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var result = new Sprite[count];
            if (texture == null) return result;
            int frameWidth = texture.width / count;
            for (int i = 0; i < count; i++)
            {
                string spritePath = $"{ArtPath}/{prefix}_{i:00}.asset";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    sprite = Sprite.Create(texture, new Rect(i * frameWidth, 0, frameWidth, texture.height), new Vector2(0.5f, 0.03f), 128f, 0, SpriteMeshType.FullRect);
                    sprite.name = $"{prefix}_{i:00}";
                    AssetDatabase.CreateAsset(sprite, spritePath);
                }
                result[i] = sprite;
            }
            return result;
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

        static SpriteRenderer[] MakeMotes(string rootName, Sprite sprite, int count, int order, System.Func<int, Vector3> position, float scale)
        {
            var renderers = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                renderers[i] = SpriteRendererObject($"{rootName} {i + 1:00}", sprite, position(i), order, 108f);
                renderers[i].transform.localScale = Vector3.one * scale;
                renderers[i].color = new Color(0.82f, 0.84f, 0.76f, 0.22f);
                renderers[i].enabled = false;
            }
            return renderers;
        }

        static Sprite CreatePixelSprite(string name, int width, int height, System.Func<int, int, Color> pixel)
        {
            string texturePath = $"{ArtPath}/{name}_texture.asset";
            string spritePath = $"{ArtPath}/{name}.asset";
            var old = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (old != null) return old;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = name + " texture";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) pixels[y * width + x] = pixel(x, y);
            texture.SetPixels(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, texturePath);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 108f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            AssetDatabase.CreateAsset(sprite, spritePath);
            return sprite;
        }

        static Color DrawTrackPixels(int x, int y)
        {
            Color dust = new Color(0.72f, 0.68f, 0.55f, 0.58f);
            // Four small three-toed tracks, staggered along a path toward the service door.
            for (int i = 0; i < 4; i++)
            {
                int cx = 13 + i * 29;
                int cy = 20 + (i % 2 == 0 ? 0 : 4);
                if (Inside(x, y, cx - 2, cy - 1, cx + 2, cy + 4) ||
                    Inside(x, y, cx - 6, cy + 4, cx - 3, cy + 8) ||
                    Inside(x, y, cx - 1, cy + 5, cx + 2, cy + 9) ||
                    Inside(x, y, cx + 4, cy + 3, cx + 7, cy + 7)) return dust;
            }
            return Color.clear;
        }

        static Color DrawThreeToedFootprint(int x, int y)
        {
            Color dust = new Color(0.30f, 0.29f, 0.27f, 1f);
            Color edge = new Color(0.55f, 0.53f, 0.49f, 0.95f);
            bool heel = Inside(x, y, 17, 3, 27, 13) || Inside(x, y, 16, 6, 28, 10);
            bool centerToe = Inside(x, y, 19, 10, 25, 29) || Inside(x, y, 20, 27, 24, 32);
            bool leftToe = Inside(x, y, 14, 10, 19, 16) || Inside(x, y, 11, 15, 16, 21) || Inside(x, y, 8, 20, 13, 26);
            bool rightToe = Inside(x, y, 25, 10, 30, 16) || Inside(x, y, 28, 15, 33, 21) || Inside(x, y, 31, 20, 36, 26);
            if (heel || centerToe || leftToe || rightToe) return dust;
            bool toeEdge = Inside(x, y, 18, 9, 26, 33) && (x == 18 || x == 26 || y == 9 || y == 33) ||
                           Inside(x, y, 13, 9, 20, 17) && (x == 13 || x == 20 || y == 9 || y == 17) ||
                           Inside(x, y, 10, 14, 17, 22) && (x == 10 || x == 17 || y == 14 || y == 22) ||
                           Inside(x, y, 7, 19, 14, 27) && (x == 7 || x == 14 || y == 19 || y == 27) ||
                           Inside(x, y, 24, 9, 31, 17) && (x == 24 || x == 31 || y == 9 || y == 17) ||
                           Inside(x, y, 27, 14, 34, 22) && (x == 27 || x == 34 || y == 14 || y == 22) ||
                           Inside(x, y, 30, 19, 37, 27) && (x == 30 || x == 37 || y == 19 || y == 27);
            if (toeEdge) return edge;
            return Color.clear;
        }

        static Color DrawMothPixels(int x, int y)
        {
            Color wing = new Color(0.87f, 0.81f, 0.62f, 1f), body = new Color(0.30f, 0.26f, 0.22f, 1f);
            if (Inside(x, y, 10, 4, 13, 12)) return body;
            if (Inside(x, y, 4, 3, 9, 8) || Inside(x, y, 14, 3, 19, 8) ||
                Inside(x, y, 6, 9, 9, 11) || Inside(x, y, 14, 9, 17, 11)) return wing;
            return Color.clear;
        }

        static Color DrawMothGlowPixels(int x, int y)
        {
            float dx = (x - 11.5f) / 11.5f, dy = (y - 11.5f) / 11.5f;
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            float alpha = Mathf.Clamp01(1f - radius) * 0.32f;
            return new Color(1f, 0.78f, 0.42f, alpha);
        }

        static Color DrawFeatherPixels(int x, int y)
        {
            float t = y / 31f;
            int cx = Mathf.RoundToInt(5 + t * 9);
            int radius = Mathf.RoundToInt(1 + Mathf.Sin(t * Mathf.PI) * 4);
            if (Mathf.Abs(x - cx) <= radius && y > 1) return new Color(0.67f, 0.72f, 0.72f, 1f);
            if (Mathf.Abs(x - (5 + y * 9 / 31)) <= 1 && y < 27) return new Color(0.84f, 0.87f, 0.84f, 1f);
            return Color.clear;
        }

        static Color DrawCupPixels(int x, int y)
        {
            Color cup = new Color(0.86f, 0.90f, 0.94f, 1f), dark = new Color(0.06f, 0.10f, 0.20f, 1f), coffee = new Color(0.24f, 0.13f, 0.08f, 1f);
            if (Inside(x, y, 4, 4, 17, 14)) return cup;
            if (Inside(x, y, 6, 12, 15, 14)) return coffee;
            if (Inside(x, y, 17, 7, 21, 11)) return cup;
            if (Inside(x, y, 2, 2, 20, 3)) return dark;
            if ((x == 9 || x == 13) && y >= 16 && y <= 18) return new Color(0.70f, 0.82f, 0.88f, 0.8f);
            return Color.clear;
        }

        static Color DrawCoffeeTablePixels(int x, int y)
        {
            Color darkWood = new Color(0.10f, 0.075f, 0.055f, 1f);
            Color wood = new Color(0.30f, 0.20f, 0.12f, 1f);
            Color edge = new Color(0.16f, 0.11f, 0.075f, 1f);
            Color highlight = new Color(0.47f, 0.33f, 0.20f, 1f);

            if (Inside(x, y, 2, 22, 45, 27))
            {
                if (y >= 26 && x >= 5 && x <= 42) return highlight;
                if (y == 22 || x <= 3 || x >= 44) return edge;
                return wood;
            }
            if (Inside(x, y, 6, 16, 41, 21))
            {
                if (Inside(x, y, 19, 18, 28, 19)) return edge;
                if (x == 23 && y == 18) return highlight;
                return darkWood;
            }
            if (Inside(x, y, 8, 3, 12, 16) || Inside(x, y, 36, 3, 40, 16))
            {
                if (x == 8 || x == 36) return edge;
                if (y <= 4) return darkWood;
                return wood;
            }
            if (Inside(x, y, 13, 9, 35, 11)) return edge;
            return Color.clear;
        }

        static Color DrawSteamPixels(int x, int y)
        {
            Color vapor = new Color(0.76f, 0.88f, 0.95f, 0.72f);
            if ((y >= 2 && y <= 4 && x == 2) || (y >= 5 && y <= 7 && x == 3) ||
                (y >= 8 && y <= 10 && x == 2) || (y >= 11 && y <= 13 && x == 3)) return vapor;
            return Color.clear;
        }

        static Color DrawDustPixels(int x, int y)
        {
            if ((x == 1 && y == 1) || (x == 2 && y == 1) || (x == 1 && y == 2))
                return new Color(0.88f, 0.86f, 0.74f, 0.95f);
            return Color.clear;
        }

        static Color DrawBeamPixels(int x, int y)
        {
            float half = 2.5f + x * 0.17f;
            if (x < 2 || x > 109 || Mathf.Abs(y - 27.5f) > half) return Color.clear;
            float core = 1f - Mathf.Abs(y - 27.5f) / half;
            float near = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2f, 14f, x));
            float alpha = (0.025f + 0.19f * core * core) * near;
            return new Color(1f, 0.96f, 0.82f, alpha);
        }

        static Color DrawVignettePixels(int x, int y)
        {
            float nx = (x - 191.5f) / 191.5f;
            float ny = (y - 107.5f) / 107.5f;
            float radius = Mathf.Sqrt(nx * nx * 0.72f + ny * ny * 0.58f);
            float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 1.06f, radius)) * 0.28f;
            return new Color(0.005f, 0.012f, 0.035f, alpha);
        }

        static Color DrawGlowPixels(int x, int y)
        {
            float edge = Mathf.Min(x, 47 - x);
            float vertical = Mathf.Min(y, 63 - y);
            float alpha = Mathf.Clamp01(Mathf.Min(edge / 10f, vertical / 14f)) * 0.24f;
            return new Color(1f, 0.63f, 0.24f, alpha);
        }

        static bool Inside(int x, int y, int left, int bottom, int right, int top) => x >= left && x <= right && y >= bottom && y <= top;

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
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = true;
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
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
