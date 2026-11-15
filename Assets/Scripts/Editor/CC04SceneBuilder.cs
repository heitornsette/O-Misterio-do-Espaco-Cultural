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
    public static class CC04SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/CC04_Passagem_Silenciosa.unity";
        const string ArtPath = "Assets/Art/CC04";
        const string AudioPath = "Assets/Audio/CC04";
        const string CharacterArtPath = "Assets/Art/Characters";
        const string CharacterFramesPath = "Assets/Art/CC01";
        const int SampleRate = 22050;
        static Font uiFont;

        [MenuItem("Misterio/Build CC04 - Passagem silenciosa")]
        public static void Build()
        {
            EnsureFolders();
            ImportSceneArt();
            uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "/archive_quiet.png");
            Sprite moemaSprite = LoadNamedSprite(CharacterArtPath + "/moema_base_frente.png", "moema_base_frente_0");
            Sprite[] idleBack = LoadFrames("GuardIdleBack", 5);
            Sprite[] idleFront = LoadFrames("GuardIdleFront", 5);
            Sprite[] walkBack = LoadFrames("GuardWalkBack", 8);
            if (background == null || moemaSprite == null || idleBack[0] == null || idleFront[0] == null || walkBack[0] == null)
                throw new InvalidOperationException("CC04 needs its archive background and the shared guard and Moema sprites.");

            Sprite spotlightSprite = CreatePixelSprite("cc04_security_spotlight", 192, 80, DrawSpotlight, 160f);
            Sprite shadowSprite = CreatePixelSprite("cc04_character_shadow", 96, 24, DrawShadow, 160f);
            Sprite shardGlintSprite = CreatePixelSprite("cc04_shard_glint", 20, 20, DrawGlint, 128f);
            Sprite archivePieceSprite = CreatePixelSprite("cc04_archive_piece", 28, 18, DrawArchivePiece, 128f);
            Sprite nearbyObjectSprite = CreatePixelSprite("cc04_bird_object", 32, 22, DrawNearbyObject, 128f);
            Sprite hushFingerSprite = CreatePixelSprite("cc04_hush_gesture", 20, 32, DrawHushGesture, 128f);
            Sprite elevatorDoorSprite = CreatePixelSprite("cc04_elevator_door", 128, 216, DrawElevatorDoor, 100f);
            Sprite indicatorGlowSprite = CreatePixelSprite("cc04_indicator_glow", 64, 64, DrawIndicatorGlow, 64f);
            Sprite whitePixel = CreatePixelSprite("cc04_white_pixel", 1, 1, (x, y) => Color.white, 1f);

            AudioClip ambient = CreateWaveAsset("cc04_ambiente_acervo.wav", 4f, ArchiveAmbientSample);
            AudioClip music = CreateWaveAsset("cc04_frase_silenciosa.wav", 9f, QuietPhraseSample);
            AudioClip cloth = CreateWaveAsset("cc04_tecido_e_respiracao.wav", 0.72f, ClothAndBreathSample);
            AudioClip creak = CreateWaveAsset("cc04_rangido_leve.wav", 0.44f, WoodCreakSample);
            AudioClip step = CreateWaveAsset("cc04_passo_abafado.wav", 0.24f, SoftStepSample);
            AudioClip motor = CreateWaveAsset("cc04_motor_elevador.wav", 2.8f, ElevatorMotorSample);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Câmera - CC04 acervo 384x216");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 0.76f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.018f, 0.038f);
            camera.transform.position = new Vector3(-0.56f, 0.02f, -10f);
            cameraObject.AddComponent<AudioListener>();

            var roomRoot = new GameObject("CC04 - Acervo de Artefatos");
            MakeWorldSprite(roomRoot.transform, "Fundo - acervo e elevador de serviço", background,
                Vector3.zero, 0, new Vector3(1.015f, 1f, 1f));

            Transform spotlight = new GameObject("Ciclo normal do holofote").transform;
            spotlight.position = new Vector3(-0.04f, -0.24f, 0.04f);
            spotlight.SetParent(roomRoot.transform, true);
            SpriteRenderer spotlightRenderer = spotlight.gameObject.AddComponent<SpriteRenderer>();
            spotlightRenderer.sprite = spotlightSprite;
            spotlightRenderer.sortingOrder = 2;
            spotlightRenderer.color = new Color(0.62f, 0.76f, 1f, 0.18f);
            spotlight.localScale = new Vector3(1.58f, 0.53f, 1f);

            SpriteRenderer shardGlint = MakeWorldSprite(roomRoot.transform, "Brilho mínimo nos cacos - rota de risco", shardGlintSprite,
                new Vector3(-0.06f, -0.60f, -0.03f), 5, Vector3.one);
            shardGlint.color = new Color(0.74f, 0.88f, 1f, 0.20f);

            SpriteRenderer nearbyObject = MakeWorldSprite(roomRoot.transform, "Pequeno objeto que entretém Moema", nearbyObjectSprite,
                new Vector3(-0.41f, -0.30f, -0.055f), 7, new Vector3(0.55f, 0.55f, 1f));

            var moemaObject = new GameObject("Moema - tranquila junto ao objeto");
            moemaObject.transform.position = new Vector3(-0.40f, -0.34f, -0.08f);
            moemaObject.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
            SpriteRenderer moemaRenderer = moemaObject.AddComponent<SpriteRenderer>();
            moemaRenderer.sprite = moemaSprite;
            moemaRenderer.sortingOrder = 8;
            moemaObject.transform.SetParent(roomRoot.transform, true);
            SpriteRenderer moemaShadow = MakeWorldSprite(roomRoot.transform, "Sombra de Moema no piso", shadowSprite,
                new Vector3(-0.30f, -0.35f, -0.01f), 6, new Vector3(0.34f, 0.16f, 1f));
            moemaShadow.color = new Color(0.005f, 0.010f, 0.020f, 0.34f);

            var guardObject = new GameObject("Segurança - recolhe a peça e contorna Moema");
            guardObject.transform.position = new Vector3(-0.72f, -0.44f, -0.08f);
            guardObject.transform.localScale = new Vector3(0.48f, 0.48f, 1f);
            SpriteRenderer guardRenderer = guardObject.AddComponent<SpriteRenderer>();
            guardRenderer.sprite = idleBack[0];
            guardRenderer.sortingOrder = 9;
            guardObject.transform.SetParent(roomRoot.transform, true);

            SpriteRenderer guardShadow = MakeWorldSprite(roomRoot.transform, "Sombra do segurança acompanha os passos", shadowSprite,
                new Vector3(-0.69f, -0.45f, -0.01f), 7, new Vector3(0.25f, 0.055f, 1f));
            guardShadow.color = new Color(0.006f, 0.010f, 0.023f, 0.37f);

            SpriteRenderer archivePiece = MakeWorldSprite(roomRoot.transform, "Última peça do acervo", archivePieceSprite,
                new Vector3(-0.54f, -0.48f, -0.10f), 10, new Vector3(0.85f, 0.85f, 1f));
            SpriteRenderer hushFinger = MakeWorldSprite(guardObject.transform, "Mão discreta pedindo silêncio", hushFingerSprite,
                guardObject.transform.position + new Vector3(0.012f, 0.385f, -0.02f), 11, new Vector3(0.48f, 0.48f, 1f));
            hushFinger.transform.localScale = Vector3.one * 0.74f;
            hushFinger.enabled = false;

            Canvas canvas = MakeCanvas("CC04 - Interface cinematográfica");
            Image vignette = MakeImage(canvas.transform, "Vinheta cinematográfica", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.23f));
            vignette.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC01/cinematic_vignette.asset");
            vignette.transform.SetAsFirstSibling();

            Image leftDoor = MakeImage(canvas.transform, "Porta esquerda do elevador", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-105f, -120f), new Vector2(105f, 120f), Color.white);
            leftDoor.sprite = elevatorDoorSprite;
            leftDoor.type = Image.Type.Simple;
            RectTransform leftDoorRect = leftDoor.rectTransform;
            leftDoorRect.anchoredPosition = new Vector2(-310f, 0f);

            Image rightDoor = MakeImage(canvas.transform, "Porta direita do elevador", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-105f, -120f), new Vector2(105f, 120f), Color.white);
            rightDoor.sprite = elevatorDoorSprite;
            rightDoor.type = Image.Type.Simple;
            RectTransform rightDoorRect = rightDoor.rectTransform;
            rightDoorRect.anchoredPosition = new Vector2(310f, 0f);

            Image leftReflection = MakeImage(canvas.transform, "Reflexo do indicador na porta esquerda", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-2f, -72f), new Vector2(2f, 72f), new Color(0.55f, 0.78f, 1f, 0f));
            leftReflection.sprite = whitePixel;
            Image rightReflection = MakeImage(canvas.transform, "Reflexo do indicador na porta direita", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-2f, -72f), new Vector2(2f, 72f), new Color(0.55f, 0.78f, 1f, 0f));
            rightReflection.sprite = whitePixel;

            Image indicatorGlow = MakeImage(canvas.transform, "Halo azul do indicador", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-19f, 56f), new Vector2(19f, 94f), new Color(0.34f, 0.68f, 1f, 0f));
            indicatorGlow.sprite = indicatorGlowSprite;
            Image indicator = MakeImage(canvas.transform, "Indicador aceso do elevador", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-5f, 72f), new Vector2(5f, 77f), new Color(0.76f, 0.91f, 1f, 0f));
            indicator.sprite = whitePixel;

            Image subtitleBackground = MakeImage(canvas.transform, "Fundo discreto das legendas", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 47f), new Color(0.012f, 0.026f, 0.065f, 0.88f));
            Text subtitle = MakeText(canvas.transform, "Legenda cinematográfica", new Vector2(0.045f, 0f), new Vector2(0.955f, 0f),
                new Vector2(0f, 4f), new Vector2(0f, 44f), 9, TextAnchor.MiddleCenter, Color.white);
            subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitle.verticalOverflow = VerticalWrapMode.Overflow;
            subtitle.supportRichText = true;
            subtitle.raycastTarget = false;
            subtitle.enabled = false;
            subtitleBackground.enabled = false;

            Image fade = MakeImage(canvas.transform, "Abertura em fade", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Color.black);
            fade.transform.SetAsLastSibling();

            var audioObject = new GameObject("CC04 - Áudio do acervo");
            AudioSource voiceSource = MakeAudioSource(audioObject, "Voz - pronto para gravação");
            AudioSource ambientSource = MakeAudioSource(audioObject, "Ambiente silencioso do acervo");
            AudioSource effectsSource = MakeAudioSource(audioObject, "Efeitos sutis");
            AudioSource musicSource = MakeAudioSource(audioObject, "Música reduzida durante a saída");

            var controllerObject = new GameObject("CC04 - Passagem silenciosa (9s)");
            CC04CutsceneController controller = controllerObject.AddComponent<CC04CutsceneController>();
            SetObject(controller, "sceneCamera", camera);
            SetObject(controller, "spotlightPool", spotlight);
            SetObject(controller, "spotlightRenderer", spotlightRenderer);
            SetObject(controller, "shardGlint", shardGlint);
            SetObject(controller, "nearbyObject", nearbyObject);
            SetObject(controller, "guard", guardObject.transform);
            SetObject(controller, "guardRenderer", guardRenderer);
            SetObjectArray(controller, "guardIdleBack", idleBack);
            SetObjectArray(controller, "guardIdleFront", idleFront);
            SetObjectArray(controller, "guardWalkBack", walkBack);
            SetObject(controller, "guardShadow", guardShadow);
            SetObject(controller, "hushFinger", hushFinger);
            SetObject(controller, "archivePiece", archivePiece);
            SetObject(controller, "moema", moemaObject.transform);
            SetObject(controller, "moemaRenderer", moemaRenderer);
            SetObject(controller, "moemaShadow", moemaShadow);
            SetObject(controller, "leftDoor", leftDoorRect);
            SetObject(controller, "rightDoor", rightDoorRect);
            SetObject(controller, "leftDoorReflection", leftReflection);
            SetObject(controller, "rightDoorReflection", rightReflection);
            SetObject(controller, "elevatorIndicator", indicator);
            SetObject(controller, "indicatorGlow", indicatorGlow);
            SetObject(controller, "subtitleText", subtitle);
            SetObject(controller, "subtitleBackground", subtitleBackground);
            SetObject(controller, "vignetteImage", vignette);
            SetObject(controller, "fadeImage", fade);
            SetObject(controller, "voiceSource", voiceSource);
            SetObject(controller, "ambientSource", ambientSource);
            SetObject(controller, "effectsSource", effectsSource);
            SetObject(controller, "musicSource", musicSource);
            SetObject(controller, "archiveNight", ambient);
            SetObject(controller, "quietPhrase", music);
            SetObject(controller, "clothAndBreath", cloth);
            SetObject(controller, "woodCreak", creak);
            SetObject(controller, "softStep", step);
            SetObject(controller, "elevatorMotor", motor);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CC04 scene built: the guard collects the last archive piece, avoids the shards, passes calmly behind Moema and leaves through the service elevator. The ending preserves Moema in the closing-door slit; completion unlocks controls through onCutsceneFinished.");
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder(ArtPath);
            EnsureFolder("Assets/Audio");
            EnsureFolder(AudioPath);
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Scripts/CC04");
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

        static void ImportSceneArt()
        {
            string backgroundPath = ArtPath + "/archive_quiet.png";
            TextureImporter backgroundImporter = AssetImporter.GetAtPath(backgroundPath) as TextureImporter;
            if (backgroundImporter != null)
            {
                backgroundImporter.textureType = TextureImporterType.Sprite;
                backgroundImporter.spriteImportMode = SpriteImportMode.Single;
                backgroundImporter.filterMode = FilterMode.Point;
                backgroundImporter.textureCompression = TextureImporterCompression.Uncompressed;
                backgroundImporter.alphaIsTransparency = false;
                backgroundImporter.spritePixelsPerUnit = 432f;
                backgroundImporter.SaveAndReimport();
            }

            TextureImporter moemaImporter = AssetImporter.GetAtPath(CharacterArtPath + "/moema_base_frente.png") as TextureImporter;
            if (moemaImporter != null)
            {
                moemaImporter.textureType = TextureImporterType.Sprite;
                moemaImporter.spriteImportMode = SpriteImportMode.Multiple;
                moemaImporter.filterMode = FilterMode.Point;
                moemaImporter.textureCompression = TextureImporterCompression.Uncompressed;
                moemaImporter.SaveAndReimport();
            }
            AssetDatabase.Refresh();
        }

        static Sprite[] LoadFrames(string prefix, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(CharacterFramesPath + "/" + prefix + "_" + i.ToString("00") + ".asset");
            return frames;
        }

        static Sprite LoadNamedSprite(string path, string spriteName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (UnityEngine.Object asset in assets)
                if (asset is Sprite sprite && sprite.name == spriteName) return sprite;
            return null;
        }

        static SpriteRenderer MakeWorldSprite(Transform parent, string name, Sprite sprite, Vector3 position, int sortingOrder, Vector3 scale)
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
            scaler.referenceResolution = new Vector2(384f, 216f);
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
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static Color DrawSpotlight(int x, int y)
        {
            float u = (x + 0.5f) / 192f;
            float v = (y + 0.5f) / 80f;
            float core = Mathf.Clamp01(1f - Mathf.Abs(v - 0.52f) / 0.47f);
            float endFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.10f, u)) *
                            (1f - Mathf.SmoothStep(0.88f, 1f, u));
            float band = Mathf.Lerp(0.62f, 1f, Mathf.Sin(u * Mathf.PI));
            return new Color(0.62f, 0.76f, 1f, core * endFade * band * 0.50f);
        }

        static Color DrawShadow(int x, int y)
        {
            float nx = (x + 0.5f - 48f) / 48f;
            float ny = (y + 0.5f - 12f) / 12f;
            float shape = nx * nx * 0.8f + ny * ny;
            if (shape >= 1f) return new Color(0f, 0f, 0f, 0f);
            float alpha = Mathf.Clamp01((1f - shape) * 1.2f) * 0.70f;
            return new Color(0.005f, 0.010f, 0.022f, alpha);
        }

        static Color DrawGlint(int x, int y)
        {
            float dx = Mathf.Abs(x - 9.5f);
            float dy = Mathf.Abs(y - 9.5f);
            bool core = dx <= 1f || dy <= 1f;
            bool tip = (dx <= 0.5f && dy <= 8f) || (dy <= 0.5f && dx <= 8f);
            if (!core && !tip) return new Color(0f, 0f, 0f, 0f);
            return new Color(0.72f, 0.88f, 1f, core ? 0.92f : 0.54f);
        }

        static Color DrawArchivePiece(int x, int y)
        {
            float cx = 13.5f;
            float cy = 8.5f;
            float facet = Mathf.Abs(x - cx) / 9f + Mathf.Abs(y - cy) / 5f;
            if (facet > 1.08f) return new Color(0f, 0f, 0f, 0f);
            if (facet > 0.86f) return new Color(0.11f, 0.12f, 0.16f, 1f);
            if (x > 15 && y > 8) return new Color(0.88f, 0.79f, 0.62f, 1f);
            if (y < 7 || x < 10) return new Color(0.44f, 0.33f, 0.25f, 1f);
            return new Color(0.63f, 0.49f, 0.34f, 1f);
        }

        static Color DrawNearbyObject(int x, int y)
        {
            if (y >= 2 && y <= 5 && x >= 7 && x <= 24)
            {
                if (y == 2 || x == 7 || x == 24) return new Color(0.12f, 0.13f, 0.17f, 1f);
                return y == 5 ? new Color(0.80f, 0.63f, 0.38f, 1f) : new Color(0.36f, 0.25f, 0.19f, 1f);
            }
            float dx = (x - 15.5f) / 8.5f;
            float dy = (y - 12f) / 8f;
            float d = dx * dx + dy * dy;
            if (d > 1.1f || y < 5) return new Color(0f, 0f, 0f, 0f);
            if (d > 0.78f) return new Color(0.13f, 0.13f, 0.17f, 1f);
            if (x >= 14 && y >= 15) return new Color(0.95f, 0.79f, 0.48f, 1f);
            if (x < 13 && y < 11) return new Color(0.43f, 0.30f, 0.23f, 1f);
            return new Color(0.68f, 0.49f, 0.31f, 1f);
        }

        static Color DrawIndicatorGlow(int x, int y)
        {
            float dx = (x + 0.5f - 32f) / 32f;
            float dy = (y + 0.5f - 32f) / 32f;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.3f) * 0.64f;
            return new Color(0.46f, 0.74f, 1f, alpha);
        }

        static Color DrawHushGesture(int x, int y)
        {
            // Raised index finger, palm and blue sleeve, with the skin fill inside a dark pixel outline.
            if (x >= 8 && x <= 11 && y >= 14 && y <= 29)
            {
                if (x == 8 || x == 11 || y == 29) return new Color(0.12f, 0.09f, 0.10f, 1f);
                return new Color(0.77f, 0.56f, 0.41f, 1f);
            }
            if (x >= 6 && x <= 14 && y >= 9 && y <= 16)
            {
                if (x == 6 || x == 14 || y == 9) return new Color(0.12f, 0.09f, 0.10f, 1f);
                return new Color(0.77f, 0.56f, 0.41f, 1f);
            }
            if (x >= 2 && x <= 8 && y >= 4 && y <= 10)
            {
                if (x == 2 || y == 4 || (x == 8 && y < 8)) return new Color(0.12f, 0.09f, 0.10f, 1f);
                return new Color(0.28f, 0.42f, 0.59f, 1f);
            }
            return new Color(0f, 0f, 0f, 0f);
        }

        static Color DrawElevatorDoor(int x, int y)
        {
            float u = x / 128f;
            float highlight = Mathf.Exp(-Mathf.Pow((u - 0.12f) / 0.045f, 2f)) * 0.19f +
                              Mathf.Exp(-Mathf.Pow((u - 0.88f) / 0.055f, 2f)) * 0.12f;
            float brushed = 0.018f * Mathf.Sin(x * 0.62f) + 0.009f * Mathf.Sin(x * 1.47f + y * 0.035f);
            float shade = 0.44f + highlight + brushed;
            if (x < 3 || x > 124) shade *= 0.55f;
            if (y < 5 || y > 210) shade *= 0.60f;
            return new Color(shade * 0.82f, shade * 0.92f, shade * 1.04f, 1f);
        }

        static float ArchiveAmbientSample(float t)
        {
            float hum = 0.008f * Mathf.Sin(2f * Mathf.PI * 52f * t) + 0.004f * Mathf.Sin(2f * Mathf.PI * 104f * t + 0.2f);
            float air = 0.003f * (Mathf.PerlinNoise(t * 7.5f, 0.37f) - 0.5f);
            return hum + air;
        }

        static float QuietPhraseSample(float t)
        {
            float result = 0f;
            float[] starts = { 0.34f, 1.32f, 2.42f, 3.63f, 4.72f, 5.72f, 6.90f, 7.80f };
            float[] notes = { 392f, 523.25f, 440f, 659.25f, 329.63f, 493.88f, 392f, 587.33f };
            for (int i = 0; i < starts.Length; i++)
            {
                float local = t - starts[i];
                if (local < 0f || local > 0.62f) continue;
                float envelope = Mathf.Exp(-local * 4.9f) * Mathf.Clamp01(local * 64f);
                result += 0.044f * envelope * (Mathf.Sin(2f * Mathf.PI * notes[i] * local) +
                    0.08f * Mathf.Sin(2f * Mathf.PI * notes[i] * 2.01f * local));
            }
            float bed = Mathf.Sin(2f * Mathf.PI * 130.81f * t) * Mathf.Exp(-t * 0.08f) * 0.006f;
            return result + bed;
        }

        static float ClothAndBreathSample(float t)
        {
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.72f));
            float softNoise = Mathf.PerlinNoise(8f + t * 185f, 0.61f) - 0.5f;
            float fabric = softNoise * Mathf.Exp(-t * 2.1f) * 0.075f;
            float breath = 0.028f * envelope * Mathf.Sin(2f * Mathf.PI * (88f + 16f * t) * t);
            return fabric + breath;
        }

        static float WoodCreakSample(float t)
        {
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.44f)) * Mathf.Exp(-t * 1.6f);
            float tone = Mathf.Sin(2f * Mathf.PI * (115f + 54f * t) * t);
            float overtone = 0.22f * Mathf.Sin(2f * Mathf.PI * 365f * t);
            return 0.085f * envelope * (tone + overtone);
        }

        static float SoftStepSample(float t)
        {
            float envelope = Mathf.Exp(-t * 32f) * Mathf.Clamp01(t * 115f);
            return envelope * (0.12f * Mathf.Sin(2f * Mathf.PI * 82f * t) + 0.035f * Mathf.Sin(2f * Mathf.PI * 278f * t));
        }

        static float ElevatorMotorSample(float t)
        {
            float swell = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 2.8f));
            float hum = 0.10f * Mathf.Sin(2f * Mathf.PI * 47f * t) + 0.028f * Mathf.Sin(2f * Mathf.PI * 94f * t);
            float whirr = 0.016f * (Mathf.PerlinNoise(t * 76f, 0.88f) - 0.5f);
            return swell * (hum + whirr);
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
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
