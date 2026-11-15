#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Misterio.Cutscenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Misterio.EditorTools
{
    public static class CC04ASceneBuilder
    {
        const string ScenePath = "Assets/Scenes/CC04A_Alerta_de_Moema.unity";
        const string ArtPath = "Assets/Art/CC04A";
        const string AudioPath = "Assets/Audio/CC04A";
        const string CharacterPath = "Assets/Art/Characters";
        const int SampleRate = 22050;
        static Font uiFont;

        [MenuItem("Misterio/Build CC04A - Alerta de Moema")]
        public static void Build()
        {
            EnsureFolders();
            ImportPixelArt();

            Sprite background = LoadSprite(ArtPath + "/fundo_andar3.png");
            Sprite vitrine = LoadSprite(ArtPath + "/vitrine.png");
            Sprite plankResting = LoadSprite(ArtPath + "/tabua_solta.png");
            Sprite plankPressed = LoadSprite(ArtPath + "/tabua_solta_pisada.png");
            Sprite[] guardIdleFront = LoadGuardFrames();
            Sprite[] moemaIdleFront = LoadMoemaFrames("Moema_Idle_Frente", 4);
            Sprite[] moemaWalkFront = LoadMoemaFrames("Moema_Andar_Frente", 7);
            Sprite[] moemaAlertFront = LoadMoemaFrames("Moema_Alerta_Frente", 5);
            Sprite[] moemaWalkBack = LoadMoemaFrames("Moema_Andar_Costas", 8);
            if (background == null || vitrine == null || plankResting == null || plankPressed == null ||
                guardIdleFront[0] == null || moemaIdleFront[0] == null || moemaWalkFront[0] == null ||
                moemaAlertFront[0] == null || moemaWalkBack[0] == null)
                throw new InvalidOperationException("CC04A requires the floor-three archive assets and the aligned Moema/guard frames.");

            Sprite spotlightSprite = CreatePixelSprite("cc04a_sweeping_spotlight", 192, 112, DrawSpotlight, 64f);
            Sprite shadowSprite = CreatePixelSprite("cc04a_ground_shadow", 36, 10, DrawGroundShadow, 64f);
            Sprite shardSprite = CreatePixelSprite("cc04a_glass_shards", 36, 18, DrawGlassShards, 64f);
            Sprite dustSprite = CreatePixelSprite("cc04a_trot_dust", 4, 4, DrawDustPixel, 64f);
            Sprite whitePixel = CreatePixelSprite("cc04a_white_pixel", 1, 1, (x, y) => Color.white, 1f);

            AudioClip ambience = CreateWaveAsset("cc04a_ambiencia_acervo.wav", 4f, ArchiveAmbienceSample);
            AudioClip music = CreateWaveAsset("cc04a_pausa_musical.wav", 4f, QuietMusicSample);
            AudioClip plank = CreateWaveAsset("cc04a_estalo_tabua.wav", 0.22f, PlankCreakSample);
            AudioClip feathers = CreateWaveAsset("cc04a_penas_rocam.wav", 0.46f, FeatherRustleSample);
            AudioClip step = CreateWaveAsset("cc04a_passos_trote.wav", 0.15f, TrotStepSample);
            AudioClip xylophone = CreateWaveAsset("cc04a_acento_xilofone.wav", 0.32f, XylophoneSample);
            AudioClip restart = CreateWaveAsset("cc04a_nota_reinicio.wav", 0.24f, RestartNoteSample);

            uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Câmera - Acervo do terceiro andar 640x360");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 2.35f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.15f);
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = Vector3.up;
            camera.transform.position = new Vector3(-0.35f, -0.20f, -10f);
            cameraObject.AddComponent<AudioListener>();

            Transform archiveRoot = new GameObject("CC04A - Acervo de Artefatos, terceiro andar").transform;
            MakeWorldSprite(archiveRoot, "Piso e paredes do terceiro andar", background,
                Vector3.zero, -100, Vector3.one);

            MakeWorldSprite(archiveRoot, "Vitrine intacta com peças do acervo", vitrine,
                new Vector3(-1.16f, -0.30f, -0.02f), 4, Vector3.one);

            SpriteRenderer loosePlank = MakeWorldSprite(archiveRoot, "Tábua solta - ruído do gatilho", plankResting,
                new Vector3(-2.42f, -0.76f, -0.10f), 5, Vector3.one);
            SpriteRenderer secondPlank = MakeWorldSprite(archiveRoot, "Tábua solta no trecho livre", plankResting,
                new Vector3(0.82f, -1.40f, -0.10f), 5, new Vector3(0.88f, 0.88f, 1f));
            secondPlank.color = new Color(1f, 1f, 1f, 0.88f);

            SpriteRenderer shards = MakeWorldSprite(archiveRoot, "Cacos já presentes no piso - sem novo dano", shardSprite,
                new Vector3(0.15f, -1.42f, -0.12f), 5, Vector3.one);
            shards.color = new Color(0.76f, 0.88f, 1f, 0.72f);

            Transform spotlight = new GameObject("Holofote - passagem contínua e sem flash").transform;
            spotlight.SetParent(archiveRoot, true);
            spotlight.position = new Vector3(-2.15f, -0.92f, -0.16f);
            spotlight.rotation = Quaternion.Euler(0f, 0f, -7f);
            spotlight.localScale = new Vector3(1.55f, 1.12f, 1f);
            SpriteRenderer spotlightRenderer = spotlight.gameObject.AddComponent<SpriteRenderer>();
            spotlightRenderer.sprite = spotlightSprite;
            spotlightRenderer.sortingOrder = 9;
            spotlightRenderer.color = new Color(0.90f, 0.93f, 1f, 0.62f);

            SpriteRenderer guardShadow = MakeWorldSprite(archiveRoot, "Sombra curta do segurança", shadowSprite,
                new Vector3(-2.70f, -0.75f, 0.01f), 6, new Vector3(1.14f, 1f, 1f));
            guardShadow.color = new Color(0.025f, 0.03f, 0.045f, 0.43f);
            GameObject guardObject = new GameObject("Segurança - para ao ouvir a tábua");
            guardObject.transform.position = new Vector3(-2.70f, -0.72f, -0.08f);
            guardObject.transform.localScale = Vector3.one * 1.10f;
            guardObject.transform.SetParent(archiveRoot, true);
            SpriteRenderer guardRenderer = guardObject.AddComponent<SpriteRenderer>();
            guardRenderer.sprite = guardIdleFront[0];
            guardRenderer.sortingOrder = 7;

            SpriteRenderer moemaShadow = MakeWorldSprite(archiveRoot, "Sombra curta de Moema", shadowSprite,
                new Vector3(1.90f, -1.80f, 0.01f), 6, new Vector3(1.32f, 1.08f, 1f));
            moemaShadow.color = new Color(0.025f, 0.03f, 0.045f, 0.44f);
            GameObject moemaObject = new GameObject("Moema - patrulha, alerta e fuga");
            moemaObject.transform.position = new Vector3(1.90f, -1.77f, -0.08f);
            moemaObject.transform.localScale = Vector3.one * 0.62f;
            moemaObject.transform.SetParent(archiveRoot, true);
            SpriteRenderer moemaRenderer = moemaObject.AddComponent<SpriteRenderer>();
            moemaRenderer.sprite = moemaIdleFront[0];
            moemaRenderer.sortingOrder = 8;

            SpriteRenderer[] dustMotes = new SpriteRenderer[12];
            for (int i = 0; i < dustMotes.Length; i++)
            {
                dustMotes[i] = MakeWorldSprite(archiveRoot, $"Poeira baixa do trote {i + 1:00}", dustSprite,
                    new Vector3(0f, -4f, 0f), 10, Vector3.one * 0.54f);
                dustMotes[i].enabled = false;
            }

            Canvas canvas = MakeCanvas("CC04A - Legendas e fade simples");
            Image subtitleBackground = MakeImage(canvas.transform, "Fundo discreto das legendas",
                new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 68f),
                new Color(0.015f, 0.025f, 0.055f, 0.82f));
            subtitleBackground.sprite = whitePixel;
            Text subtitleText = MakeText(canvas.transform, "Legenda da narrativa", new Vector2(0.045f, 0f),
                new Vector2(0.955f, 0f), new Vector2(0f, 5f), new Vector2(0f, 63f), 14,
                TextAnchor.MiddleCenter, Color.white);
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
            subtitleText.lineSpacing = 0.92f;
            subtitleText.fontStyle = FontStyle.Bold;

            Image fade = MakeImage(canvas.transform, "Fade preto de retorno ao puzzle", Vector2.zero,
                Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            fade.sprite = whitePixel;
            fade.transform.SetAsLastSibling();

            GameObject audioObject = new GameObject("CC04A - Som do acervo");
            AudioSource voiceSource = MakeAudioSource(audioObject, "Voz - pronto para gravação");
            AudioSource ambienceSource = MakeAudioSource(audioObject, "Ambiência noturna do acervo");
            AudioSource effectsSource = MakeAudioSource(audioObject, "Tábua, passos, penas e acento");
            AudioSource musicSource = MakeAudioSource(audioObject, "Trilha breve com pausa musical");

            GameObject controllerObject = new GameObject("CC04A - Alerta de Moema (4s)");
            CC04ACutsceneController controller = controllerObject.AddComponent<CC04ACutsceneController>();
            SetObject(controller, "sceneCamera", camera);
            SetObject(controller, "spotlightRenderer", spotlightRenderer);
            SetObject(controller, "spotlight", spotlight);
            SetObject(controller, "guard", guardObject.transform);
            SetObject(controller, "guardRenderer", guardRenderer);
            SetObjectArray(controller, "guardIdleFront", guardIdleFront);
            SetObject(controller, "guardShadow", guardShadow);
            SetObject(controller, "moema", moemaObject.transform);
            SetObject(controller, "moemaRenderer", moemaRenderer);
            SetObjectArray(controller, "moemaIdleFront", moemaIdleFront);
            SetObjectArray(controller, "moemaWalkFront", moemaWalkFront);
            SetObjectArray(controller, "moemaAlertFront", moemaAlertFront);
            SetObjectArray(controller, "moemaWalkBack", moemaWalkBack);
            SetObject(controller, "moemaShadow", moemaShadow);
            SetObject(controller, "plank", loosePlank);
            SetObject(controller, "plankResting", plankResting);
            SetObject(controller, "plankPressed", plankPressed);
            SetObject(controller, "shards", shards);
            SetObjectArray(controller, "escapeDust", dustMotes);
            SetObject(controller, "subtitleText", subtitleText);
            SetObject(controller, "subtitleBackground", subtitleBackground);
            SetObject(controller, "fadeImage", fade);
            SetObject(controller, "voiceSource", voiceSource);
            SetObject(controller, "ambienceSource", ambienceSource);
            SetObject(controller, "effectsSource", effectsSource);
            SetObject(controller, "musicSource", musicSource);
            SetObject(controller, "archiveAmbience", ambience);
            SetObject(controller, "restrainedMusic", music);
            SetObject(controller, "plankCreak", plank);
            SetObject(controller, "featherRustle", feathers);
            SetObject(controller, "trotStep", step);
            SetObject(controller, "xylophoneAccent", xylophone);
            SetObject(controller, "restartNote", restart);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CC04A built with the 3rd-floor pixel-art scene, aligned Moema animations, a restrained alert, a fixed escape reframe and an attempt-reset event.");
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder(ArtPath);
            EnsureFolder("Assets/Audio");
            EnsureFolder(AudioPath);
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Scripts/CC04A");
            EnsureFolder("Assets/Scripts/Editor");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static void ImportPixelArt()
        {
            AssetDatabase.Refresh();
            ConfigureSprite(ArtPath + "/fundo_andar3.png", 64f, false, new Vector2(0.5f, 0.5f));
            ConfigureSprite(ArtPath + "/vitrine.png", 64f, true, new Vector2(0.5f, 0.5f));
            ConfigureSprite(ArtPath + "/tabua_solta.png", 64f, true, new Vector2(0.5f, 0.5f));
            ConfigureSprite(ArtPath + "/tabua_solta_pisada.png", 64f, true, new Vector2(0.5f, 0.5f));

            string[] moemaFrames = Directory.GetFiles(CharacterPath, "Moema_*_Frame_*.png");
            foreach (string absolute in moemaFrames)
            {
                ConfigureCharacterFrameTexture(absolute.Replace('\\', '/'));
            }
            AssetDatabase.Refresh();
        }

        static void ConfigureSprite(string path, float pixelsPerUnit, bool transparent, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing CC04A sprite: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spritePivot = pivot;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = transparent;
            importer.SaveAndReimport();
        }

        static void ConfigureCharacterFrameTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing Moema frame: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        static Sprite[] LoadGuardFrames()
        {
            return new[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC01/GuardIdleFront_00.asset"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC01/GuardIdleFront_01.asset"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC01/GuardIdleFront_02.asset"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC01/GuardIdleFront_03.asset"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC01/GuardIdleFront_04.asset")
            };
        }

        static Sprite[] LoadMoemaFrames(string animation, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                string path = CharacterPath + "/" + animation + "_Frame_" + i.ToString("00") + ".png";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) continue;
                string spritePath = ArtPath + "/" + animation + "_Frame_" + i.ToString("00") + ".asset";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, 128f, 128f),
                        new Vector2(0.5f, 5f / 128f), 64f, 0, SpriteMeshType.FullRect);
                    sprite.name = animation + "_Frame_" + i.ToString("00");
                    AssetDatabase.CreateAsset(sprite, spritePath);
                }
                frames[i] = sprite;
            }
            return frames;
        }

        static SpriteRenderer MakeWorldSprite(Transform parent, string name, Sprite sprite, Vector3 position,
            int sortingOrder, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            if (parent != null) go.transform.SetParent(parent, true);
            return renderer;
        }

        static Canvas MakeCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        static Image MakeImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text MakeText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, int size, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Text text = go.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = false;
            text.raycastTarget = false;
            return text;
        }

        static AudioSource MakeAudioSource(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        static Sprite CreatePixelSprite(string name, int width, int height, Func<int, int, Color> pixel, float pixelsPerUnit)
        {
            string texturePath = ArtPath + "/" + name + "_texture.asset";
            string spritePath = ArtPath + "/" + name + ".asset";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null && (texture.width != width || texture.height != height))
            {
                AssetDatabase.DeleteAsset(spritePath);
                AssetDatabase.DeleteAsset(texturePath);
                texture = null;
            }
            if (texture == null)
            {
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.name = name + " texture";
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                AssetDatabase.CreateAsset(texture, texturePath);
            }
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) pixels[y * width + x] = pixel(x, y);
            texture.SetPixels(pixels);
            texture.Apply();
            EditorUtility.SetDirty(texture);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = name;
                AssetDatabase.CreateAsset(sprite, spritePath);
            }
            return sprite;
        }

        static Color DrawSpotlight(int x, int y)
        {
            float nx = (x + 0.5f - 96f) / 96f;
            float ny = (y + 0.5f - 56f) / 56f;
            float radius = Mathf.Sqrt(nx * nx * 0.84f + ny * ny * 1.08f);
            float value = Mathf.Clamp01(1f - radius);
            float steps = Mathf.Floor(value * 6f) / 6f;
            float alpha = steps * steps * 0.30f;
            return new Color(0.76f, 0.84f, 1f, alpha);
        }

        static Color DrawGroundShadow(int x, int y)
        {
            float nx = (x + 0.5f - 18f) / 18f;
            float ny = (y + 0.5f - 5f) / 5f;
            float radius = nx * nx * 0.72f + ny * ny;
            if (radius >= 1f) return Color.clear;
            float level = Mathf.Floor(Mathf.Clamp01((1f - radius) * 3f) * 3f) / 3f;
            return new Color(0.02f, 0.028f, 0.045f, level * 0.68f);
        }

        static Color DrawGlassShards(int x, int y)
        {
            bool left = InTriangle(x, y, 3, 5, 13, 15, 18, 8);
            bool middle = InTriangle(x, y, 13, 3, 25, 12, 29, 6);
            bool right = InTriangle(x, y, 24, 5, 31, 11, 34, 3);
            if (!left && !middle && !right) return Color.clear;
            bool edge = (left && (x <= 5 || y <= 6 || x + y >= 25)) ||
                        (middle && (y <= 4 || x >= 27 || x - y <= 10)) ||
                        (right && (x >= 32 || y <= 6 || x + y >= 42));
            if (edge) return new Color(0.11f, 0.18f, 0.25f, 0.92f);
            bool highlight = (x + 2 * y) % 7 == 0 || (left && x > 12 && y > 10);
            return highlight
                ? new Color(0.80f, 0.91f, 0.98f, 0.94f)
                : new Color(0.45f, 0.63f, 0.72f, 0.88f);
        }

        static bool InTriangle(int x, int y, int x0, int y0, int x1, int y1, int x2, int y2)
        {
            float d1 = Sign(x, y, x0, y0, x1, y1);
            float d2 = Sign(x, y, x1, y1, x2, y2);
            float d3 = Sign(x, y, x2, y2, x0, y0);
            bool negative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool positive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(negative && positive);
        }

        static float Sign(int x, int y, int ax, int ay, int bx, int by) =>
            (x - bx) * (ay - by) - (ax - bx) * (y - by);

        static Color DrawDustPixel(int x, int y)
        {
            bool pixel = (x == 1 && y == 1) || (x == 2 && y == 1) || (x == 2 && y == 2);
            return pixel ? new Color(0.72f, 0.78f, 0.82f, 0.82f) : Color.clear;
        }

        static AudioClip CreateWaveAsset(string fileName, float seconds, Func<float, float> sample)
        {
            string path = AudioPath + "/" + fileName;
            int count = Mathf.CeilToInt(seconds * SampleRate);
            int dataBytes = count * 2;
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataBytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataBytes);
                for (int i = 0; i < count; i++)
                    writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample(i / (float)SampleRate), -1f, 1f) * 32767f));
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                importer.forceToMono = true;
                importer.loadInBackground = false;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static float ArchiveAmbienceSample(float t)
        {
            float hum = 0.0065f * Mathf.Sin(2f * Mathf.PI * 50f * t) +
                        0.0025f * Mathf.Sin(2f * Mathf.PI * 100f * t + 0.2f);
            float air = 0.0018f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
            return hum + air;
        }

        static float QuietMusicSample(float t)
        {
            float fadeIn = Mathf.Clamp01(t * 12f);
            float fadeOut = Mathf.Clamp01((4f - t) * 16f);
            return fadeIn * fadeOut * (0.0045f * Mathf.Sin(2f * Mathf.PI * 146.83f * t) +
                                       0.0018f * Mathf.Sin(2f * Mathf.PI * 220f * t + 0.3f));
        }

        static float PlankCreakSample(float t)
        {
            float envelope = Mathf.Exp(-t * 15f) * Mathf.Clamp01(t * 38f);
            float body = 0.048f * Mathf.Sin(2f * Mathf.PI * (92f - 22f * t) * t) +
                         0.019f * Mathf.Sin(2f * Mathf.PI * 183f * t + 0.2f);
            float grain = 0.028f * (Mathf.PerlinNoise(8f + t * 310f, 0.36f) - 0.5f);
            return envelope * (body + grain);
        }

        static float FeatherRustleSample(float t)
        {
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.46f));
            float flutter = Mathf.Sin(2f * Mathf.PI * 18f * t) * 0.38f + 0.62f;
            float noise = Mathf.PerlinNoise(3.4f + t * 155f, 0.73f) - 0.5f;
            return envelope * flutter * noise * 0.082f;
        }

        static float TrotStepSample(float t)
        {
            float envelope = Mathf.Exp(-t * 34f) * Mathf.Clamp01(t * 100f);
            float low = 0.090f * Mathf.Sin(2f * Mathf.PI * 78f * t);
            float light = 0.024f * Mathf.Sin(2f * Mathf.PI * 250f * t + 0.15f);
            return envelope * (low + light);
        }

        static float XylophoneSample(float t)
        {
            float envelope = Mathf.Exp(-t * 9.5f) * Mathf.Clamp01(t * 85f);
            float note = Mathf.Sin(2f * Mathf.PI * 587.33f * t) +
                         0.31f * Mathf.Sin(2f * Mathf.PI * 1174.66f * t + 0.18f);
            return 0.075f * envelope * note;
        }

        static float RestartNoteSample(float t)
        {
            float envelope = Mathf.Exp(-t * 11f) * Mathf.Clamp01(t * 72f);
            return 0.058f * envelope * (Mathf.Sin(2f * Mathf.PI * 493.88f * t) +
                                        0.18f * Mathf.Sin(2f * Mathf.PI * 987.76f * t));
        }

        static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene scene in scenes)
                if (scene.path == scenePath) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetObjectArray(UnityEngine.Object target, string propertyName, UnityEngine.Object[] values)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
