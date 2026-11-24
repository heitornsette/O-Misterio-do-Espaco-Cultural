#if UNITY_EDITOR
using System;
using System.IO;
using Misterio.Cutscenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Misterio.EditorTools
{
    public static class CC08SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/CC08_A_Abertura_do_Dia.unity";
        const string ArtPath = "Assets/Art/CC08";
        static Font uiFont;

        [MenuItem("Misterio/Build CC08 - A abertura do dia")]
        public static void Build()
        {
            EnsureFolder(ArtPath);
            EnsureFolder("Assets/Audio/CC08");
            ConfigureAssets();

            Sprite exteriorArchitectureSprite = LoadSprite(ArtPath + "/jardim_amanhecer_arquitetura.png");
            Sprite exteriorFloorSprite = LoadSprite(ArtPath + "/jardim_amanhecer_piso.png");
            Sprite lobbyBackground = LoadSprite(ArtPath + "/foyer_amanhecer_base.png");
            Sprite rector = LoadSprite(ArtPath + "/reitor_base_frente.png");
            Sprite rectorWave = LoadSprite(ArtPath + "/reitor_base_frente_acenando.png");
            Sprite curatorOne = LoadSprite(ArtPath + "/curadora1_base_frente.png");
            Sprite curatorTwo = LoadSprite(ArtPath + "/curadora2_base_frente.png");
            Sprite guardFirst = LoadSprite("Assets/Art/CC01/GuardIdleFront_00.asset");
            Sprite moemaFirst = LoadSprite("Assets/Art/Characters/Moema_Idle_Frente_Frame_00.png");
            Sprite mothSheet = LoadSprite(ArtPath + "/mariposa_voo_sheet.png");
            if (exteriorArchitectureSprite == null || exteriorFloorSprite == null || lobbyBackground == null ||
                rector == null || rectorWave == null || curatorOne == null || curatorTwo == null ||
                guardFirst == null || moemaFirst == null || mothSheet == null)
                throw new InvalidOperationException("CC08 needs the validated dawn garden, foyer, cast and moth assets.");

            Sprite[] rectorIdle = SliceSheet(ArtPath + "/reitor_idle_sheet.png", "CC08_ReitorIdle", 4, 128, 128,
                64f, new Vector2(0.5f, 0.039f));
            Sprite[] mothFlight = SliceSheet(ArtPath + "/mariposa_voo_sheet.png", "CC08_MariposaVoo", 4, 32, 32,
                64f, new Vector2(0.5f, 0.5f));
            Sprite[] guardFrames = LoadFrames("Assets/Art/CC01", "GuardIdleFront", 5);
            Sprite[] moemaIdle = LoadMoemaIdleFrames();

            Sprite whitePixel = CreatePixelSprite("cc08_ui_white_pixel", 1, 1,
                (x, y) => Color.white, 64f, new Vector2(0.5f, 0.5f));
            Sprite shadow = CreatePixelSprite("cc08_soft_shadow", 40, 12, DrawShadow, 64f, new Vector2(0.5f, 0.5f));
            Sprite softLight = CreatePixelSprite("cc08_soft_light", 64, 24, DrawSoftLight, 64f, new Vector2(0.5f, 0.5f));
            AssetDatabase.DeleteAsset(ArtPath + "/cc08_glass_door_left.asset");
            AssetDatabase.DeleteAsset(ArtPath + "/cc08_glass_door_right.asset");
            Sprite leftGlassDoor = CreatePixelSprite("cc08_glass_door_left", 22, 52, DrawGlassDoor, 64f, new Vector2(0.5f, 0.5f));
            Sprite rightGlassDoor = CreatePixelSprite("cc08_glass_door_right", 22, 52, DrawGlassDoor, 64f, new Vector2(0.5f, 0.5f));
            Sprite glassGlint = CreatePixelSprite("cc08_glass_glint", 18, 48, DrawGlassGlint, 64f, new Vector2(0.5f, 0.5f));
            Sprite radioSprite = CreatePixelSprite("cc08_security_radio", 18, 32, DrawRadio, 128f, new Vector2(0.5f, 0.08f));
            Sprite radioScreenPixel = CreatePixelSprite("cc08_radio_screen", 4, 4,
                (x, y) => new Color(0.63f, 0.86f, 1f, 0.85f), 128f, new Vector2(0.5f, 0.5f));
            Sprite guardWaveArmSprite = CreatePixelSprite("cc08_guard_wave_arm", 18, 30, DrawGuardWaveArm, 128f,
                new Vector2(0.5f, 0f));
            Sprite leafSprite = CreatePixelSprite("cc08_landing_leaf", 24, 10, DrawLeaf, 64f, new Vector2(0.52f, 0.45f));
            Sprite blinkSprite = CreatePixelSprite("cc08_moema_blink", 7, 3,
                (x, y) => y == 1 ? new Color(0.10f, 0.09f, 0.08f, 1f) : new Color(0.18f, 0.17f, 0.14f, 0.88f),
                64f, new Vector2(0.5f, 0.5f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Câmera - acompanha a abertura e encontra Moema");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 1.6875f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.050f, 0.080f, 1f);
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = Vector3.up;
            camera.transform.position = new Vector3(-0.10f, 0.02f, -10f);
            cameraObject.AddComponent<AudioListener>();

            Transform exteriorRoot = MakeRoot("Plano 01 - A equipe chega ao amanhecer");
            SpriteRenderer exteriorArchitecture = MakeWorldSprite(exteriorRoot, "Fachada do Espaço Cultural - arquitetura", exteriorArchitectureSprite,
                Vector3.zero, -100, Vector3.one);
            SpriteRenderer exteriorFloor = MakeWorldSprite(exteriorRoot, "Caminho de entrada - reflexos da manhã", exteriorFloorSprite,
                new Vector3(0f, 0f, 0.02f), -90, Vector3.one);
            Transform exteriorLeftDoor = MakeCharacter(exteriorRoot, "Porta externa esquerda - vidro", leftGlassDoor,
                new Vector3(-0.165f, 0.05f, -0.16f), 2, Vector3.one);
            Transform exteriorRightDoor = MakeCharacter(exteriorRoot, "Porta externa direita - vidro", rightGlassDoor,
                new Vector3(0.165f, 0.05f, -0.17f), 3, Vector3.one);
            Transform exteriorRector = MakePerson(exteriorRoot, "Reitor - aproxima-se da entrada", rector,
                new Vector3(-0.97f, -0.82f, -0.12f), 0.37f, 12, shadow);
            Transform exteriorCuratorOne = MakePerson(exteriorRoot, "Curadora - chega com o reitor", curatorOne,
                new Vector3(-0.53f, -0.82f, -0.12f), 0.36f, 11, shadow);
            Transform exteriorCuratorTwo = MakePerson(exteriorRoot, "Equipe de curadoria - acompanha a chegada", curatorTwo,
                new Vector3(-1.38f, -0.82f, -0.12f), 0.36f, 10, shadow);
            SpriteRenderer exteriorRectorRenderer = exteriorRector.GetComponent<SpriteRenderer>();
            Transform[] exteriorTeam = { exteriorRector, exteriorCuratorOne, exteriorCuratorTwo };
            SpriteRenderer[] exteriorTeamRenderers =
            {
                exteriorRectorRenderer, exteriorCuratorOne.GetComponent<SpriteRenderer>(), exteriorCuratorTwo.GetComponent<SpriteRenderer>()
            };

            Transform lobbyRoot = MakeRoot("Plano 02 - A exposição aguarda no saguão");
            MakeWorldSprite(lobbyRoot, "Saguão do térreo - exposição organizada", lobbyBackground, Vector3.zero, -100, Vector3.one);
            Transform lobbyLeftDoor = MakeCharacter(lobbyRoot, "Porta interna esquerda - vidro", leftGlassDoor,
                new Vector3(-0.165f, 0.16f, -0.18f), 2, Vector3.one);
            Transform lobbyRightDoor = MakeCharacter(lobbyRoot, "Porta interna direita - vidro", rightGlassDoor,
                new Vector3(0.165f, 0.16f, -0.19f), 3, Vector3.one);
            SpriteRenderer lobbyGlassReflection = MakeWorldSprite(lobbyRoot, "Reflexo claro nas folhas de vidro", glassGlint,
                new Vector3(0.04f, 0.17f, -0.20f), 4, new Vector3(0.92f, 0.92f, 1f));
            SpriteRenderer lobbySunPatch = MakeWorldSprite(lobbyRoot, "Manhã refletida no piso", softLight,
                new Vector3(0f, -0.83f, -0.10f), 5, new Vector3(2.2f, 0.65f, 1f));
            lobbySunPatch.color = new Color(1f, 0.82f, 0.60f, 0.42f);
            Transform lobbyRector = MakePerson(lobbyRoot, "Reitor - entra no saguão", rector,
                new Vector3(-0.81f, -1.12f, -0.12f), 0.36f, 12, shadow);
            Transform lobbyCuratorOne = MakePerson(lobbyRoot, "Curadora - observa a exposição", curatorOne,
                new Vector3(-0.02f, -1.12f, -0.12f), 0.36f, 11, shadow);
            Transform lobbyCuratorTwo = MakePerson(lobbyRoot, "Equipe de curadoria", curatorTwo,
                new Vector3(-1.38f, -1.12f, -0.12f), 0.36f, 10, shadow);
            Transform[] lobbyTeam = { lobbyRector, lobbyCuratorOne, lobbyCuratorTwo };
            SpriteRenderer[] lobbyTeamRenderers =
            {
                lobbyRector.GetComponent<SpriteRenderer>(), lobbyCuratorOne.GetComponent<SpriteRenderer>(), lobbyCuratorTwo.GetComponent<SpriteRenderer>()
            };

            Sprite[] guardInitialFrames = guardFrames;
            Transform guard = MakeCharacter(lobbyRoot, "Segurança - espera impecável com o rádio", guardInitialFrames[0],
                new Vector3(0.86f, -1.06f, -0.15f), 18, new Vector3(0.82f, 0.82f, 1f));
            SpriteRenderer guardRenderer = guard.GetComponent<SpriteRenderer>();
            SpriteRenderer guardWaveArm = MakeLocalCharacter(guard, "Aceno cordial do segurança", guardWaveArmSprite,
                new Vector3(0.13f, 0.22f, -0.035f), 19, Vector3.one).GetComponent<SpriteRenderer>();
            guardWaveArm.enabled = false;
            Transform radio = MakeLocalCharacter(guard, "Rádio de mão", radioSprite,
                new Vector3(0.135f, 0.27f, -0.04f), 20, Vector3.one);
            SpriteRenderer radioScreen = MakeLocalSprite(radio, "Visor azul do rádio", radioScreenPixel,
                new Vector3(0f, 0.155f, -0.02f), 21, Vector3.one);

            Transform gardenRoot = MakeRoot("Plano 03 - Moema descansa sob a árvore");
            MakeWorldSprite(gardenRoot, "Jardim do campus - arquitetura e copa", exteriorArchitectureSprite,
                Vector3.zero, -100, Vector3.one);
            MakeWorldSprite(gardenRoot, "Gramado e caminho - camada de primeiro plano", exteriorFloorSprite,
                new Vector3(0f, 0f, 0.02f), -90, Vector3.one);
            AddFireflyLight(gardenRoot, whitePixel, new Vector3(-1.86f, 0.39f, -0.10f), 1.1f);
            AddFireflyLight(gardenRoot, whitePixel, new Vector3(-1.39f, 0.59f, -0.11f), 0.9f);
            AddFireflyLight(gardenRoot, whitePixel, new Vector3(-0.97f, 0.31f, -0.12f), 1.2f);
            SpriteRenderer moemaShadow = MakeWorldSprite(gardenRoot, "Sombra suave de Moema na grama", shadow,
                new Vector3(-1.58f, -1.31f, -0.07f), 8, new Vector3(0.60f, 0.34f, 1f));
            moemaShadow.color = new Color(0.025f, 0.035f, 0.043f, 0.34f);
            Transform moema = MakeCharacter(gardenRoot, "Moema - finalmente para de seguir a mariposa", moemaFirst,
                new Vector3(-1.58f, -1.27f, -0.11f), 10, new Vector3(0.48f, 0.48f, 1f));
            SpriteRenderer moemaRenderer = moema.GetComponent<SpriteRenderer>();
            SpriteRenderer moemaBlink = MakeLocalSprite(moema, "Pálpebra - piscar breve para a câmera", blinkSprite,
                new Vector3(0.142f, 0.685f, -0.03f), 13, new Vector3(0.80f, 0.80f, 1f));
            moemaBlink.enabled = false;
            Transform landingLeaf = MakeCharacter(gardenRoot, "Folha que recebe a mariposa", leafSprite,
                new Vector3(-1.08f, -0.22f, -0.09f), 11, Vector3.one);
            Transform moth = MakeCharacter(gardenRoot, "Mariposa - pousa sobre a folha", mothFlight[0],
                new Vector3(-0.08f, -0.16f, -0.12f), 12, Vector3.one * 0.36f);
            SpriteRenderer mothRenderer = moth.GetComponent<SpriteRenderer>();

            exteriorRoot.gameObject.SetActive(true);
            lobbyRoot.gameObject.SetActive(false);
            gardenRoot.gameObject.SetActive(false);

            Canvas canvas = MakeCanvas("CC08 - Legendas, luz ambiente e encerramento");
            Image subtitleBackground = MakeImage(canvas.transform, "Tarja discreta de narração e diálogo",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 58f),
                new Color(0.012f, 0.018f, 0.038f, 0f));
            subtitleBackground.sprite = whitePixel;
            Text subtitle = MakeText(canvas.transform, "Narração e falas da abertura", new Vector2(0.045f, 0f), new Vector2(0.955f, 0f),
                new Vector2(0f, 2f), new Vector2(0f, 56f), 12, TextAnchor.MiddleCenter, Color.white);
            subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitle.verticalOverflow = VerticalWrapMode.Overflow;
            subtitle.lineSpacing = 0.94f;
            subtitle.fontStyle = FontStyle.Bold;
            subtitle.enabled = false;
            Image lobbyDaylight = MakeImage(canvas.transform, "Luz suave do amanhecer no saguão", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(1f, 0.92f, 0.78f, 0.025f));
            lobbyDaylight.sprite = whitePixel;
            lobbyDaylight.raycastTarget = false;
            lobbyDaylight.transform.SetAsFirstSibling();
            Image finalFade = MakeImage(canvas.transform, "Fade final para os créditos", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            finalFade.sprite = whitePixel;
            finalFade.raycastTarget = false;

            GameObject audioObject = new GameObject("CC08 - Ambiências e pontuações sonoras");
            AudioSource voiceSource = MakeAudioSource(audioObject, "Narração e falas - campos para gravação");
            AudioSource ambienceSource = MakeAudioSource(audioObject, "Ambiência externa e interior");
            AudioSource effectsSource = MakeAudioSource(audioObject, "Portas, passos, rádio e mariposa");
            AudioSource musicSource = MakeAudioSource(audioObject, "Tema matinal de xilofone");

            GameObject controllerObject = new GameObject("CC08 - A abertura do dia (25s)");
            CC08CutsceneController controller = controllerObject.AddComponent<CC08CutsceneController>();
            SetObject(controller, "sceneCamera", camera);
            SetObjectArray(controller, "sceneRoots", new UnityEngine.Object[] { exteriorRoot.gameObject, lobbyRoot.gameObject, gardenRoot.gameObject });
            SetObject(controller, "exteriorArchitecture", exteriorArchitecture);
            SetObject(controller, "exteriorFloor", exteriorFloor);
            SetObject(controller, "lobbyGlassReflection", lobbyGlassReflection);
            SetObject(controller, "exteriorLeftDoor", exteriorLeftDoor);
            SetObject(controller, "exteriorRightDoor", exteriorRightDoor);
            SetObject(controller, "lobbyLeftDoor", lobbyLeftDoor);
            SetObject(controller, "lobbyRightDoor", lobbyRightDoor);
            SetObjectArray(controller, "exteriorTeam", exteriorTeam);
            SetObjectArray(controller, "exteriorTeamRenderers", exteriorTeamRenderers);
            SetObjectArray(controller, "lobbyTeam", lobbyTeam);
            SetObjectArray(controller, "lobbyTeamRenderers", lobbyTeamRenderers);
            SetObjectArray(controller, "rectorIdleFrames", rectorIdle);
            SetObject(controller, "rectorWaveSprite", rectorWave);
            SetObject(controller, "guard", guard);
            SetObject(controller, "guardRenderer", guardRenderer);
            SetObjectArray(controller, "guardIdleFrames", guardFrames);
            SetObject(controller, "guardWaveArm", guardWaveArm);
            SetObject(controller, "radio", radio);
            SetObject(controller, "radioScreen", radioScreen);
            SetObject(controller, "moema", moema);
            SetObject(controller, "moemaRenderer", moemaRenderer);
            SetObjectArray(controller, "moemaIdleFrames", moemaIdle);
            SetObject(controller, "moemaBlink", moemaBlink);
            SetObject(controller, "landingLeaf", landingLeaf);
            SetObject(controller, "moth", moth);
            SetObject(controller, "mothRenderer", mothRenderer);
            SetObjectArray(controller, "mothFlightFrames", mothFlight);
            SetObject(controller, "subtitleBackground", subtitleBackground);
            SetObject(controller, "subtitleText", subtitle);
            SetObject(controller, "lobbyDaylight", lobbyDaylight);
            SetObject(controller, "finalFade", finalFade);
            SetObject(controller, "voiceSource", voiceSource);
            SetObject(controller, "ambienceSource", ambienceSource);
            SetObject(controller, "effectsSource", effectsSource);
            SetObject(controller, "musicSource", musicSource);
            SetObject(controller, "exteriorAmbience", LoadAudio("Assets/Audio/CC08/cc08_ambiencia_exterior.wav"));
            SetObject(controller, "lobbyAmbience", LoadAudio("Assets/Audio/CC08/cc08_ambiencia_saguao.wav"));
            SetObject(controller, "morningTheme", LoadAudio("Assets/Audio/CC02/cc02_xilofone_escadaria.wav"));
            SetObject(controller, "footsteps", LoadAudio("Assets/Audio/CC03/cc03_passos_no_piso.wav"));
            SetObject(controller, "doorOpening", LoadAudio("Assets/Audio/CC04/cc04_rangido_leve.wav"));
            SetObject(controller, "radioClick", LoadAudio("Assets/Audio/CC02/cc02_confirmacao.wav"));
            SetObject(controller, "mothWings", LoadAudio("Assets/Audio/CC04A/cc04a_penas_rocam.wav"));
            SetObject(controller, "finalXylophone", LoadAudio("Assets/Audio/CC04A/cc04a_acento_xilofone.wav"));
            SetObject(controller, "playOnAwake", true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CC08 built with the dawn arrival, continuous glass-door entrance, guard's radio beat and Moema's final rest under the campus tree.");
        }

        static void ConfigureAssets()
        {
            ConfigureSpriteTexture(ArtPath + "/jardim_amanhecer_referencia.png", 64f, true, new Vector2(0.5f, 0.5f));
            ConfigureSpriteTexture(ArtPath + "/jardim_amanhecer_arquitetura.png", 64f, true, new Vector2(0.5f, 0.5f));
            ConfigureSpriteTexture(ArtPath + "/jardim_amanhecer_piso.png", 64f, true, new Vector2(0.5f, 0.5f));
            ConfigureSpriteTexture(ArtPath + "/foyer_amanhecer_base.png", 64f, true, new Vector2(0.5f, 0.5f));
            ConfigureSpriteTexture(ArtPath + "/reitor_base_frente.png", 64f, true, new Vector2(0.5f, 0.039f));
            ConfigureSpriteTexture(ArtPath + "/reitor_base_frente_acenando.png", 64f, true, new Vector2(0.5f, 0.039f));
            ConfigureSpriteTexture(ArtPath + "/curadora1_base_frente.png", 64f, true, new Vector2(0.5f, 0.039f));
            ConfigureSpriteTexture(ArtPath + "/curadora2_base_frente.png", 64f, true, new Vector2(0.5f, 0.039f));
            ConfigureSpriteTexture(ArtPath + "/reitor_idle_sheet.png", 64f, true, new Vector2(0.5f, 0.039f));
            ConfigureSpriteTexture(ArtPath + "/mariposa_voo_sheet.png", 64f, true, new Vector2(0.5f, 0.5f));
            ConfigureAudio("Assets/Audio/CC08/cc08_ambiencia_exterior.wav");
            ConfigureAudio("Assets/Audio/CC08/cc08_ambiencia_saguao.wav");
        }

        static void ConfigureSpriteTexture(string path, float ppu, bool readable, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.spritePivot = pivot;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = readable;
            importer.SaveAndReimport();
        }

        static void ConfigureAudio(string path)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return;
            importer.forceToMono = false;
            importer.loadInBackground = false;
            AudioImporterSampleSettings sampleSettings = importer.defaultSampleSettings;
            sampleSettings.preloadAudioData = true;
            importer.defaultSampleSettings = sampleSettings;
            importer.SaveAndReimport();
        }

        static Sprite[] LoadMoemaIdleFrames()
        {
            Sprite[] frames = new Sprite[4];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = LoadSprite($"Assets/Art/Characters/Moema_Idle_Frente_Frame_{i:00}.png");
            return frames;
        }

        static Sprite[] LoadFrames(string folder, string prefix, int count)
        {
            Sprite[] frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{prefix}_{i:00}.asset");
            if (frames[0] == null) throw new InvalidOperationException($"Missing animation frames for {prefix}.");
            return frames;
        }

        static Sprite[] SliceSheet(string texturePath, string prefix, int count, int frameWidth, int frameHeight,
            float ppu, Vector2 pivot)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null || texture.width < frameWidth * count || texture.height != frameHeight)
                throw new InvalidOperationException($"Unexpected sprite sheet size: {texturePath}.");
            Sprite[] result = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                string path = ArtPath + "/" + prefix + "_" + i.ToString("00") + ".asset";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    sprite = Sprite.Create(texture, new Rect(i * frameWidth, 0, frameWidth, frameHeight), pivot, ppu,
                        0, SpriteMeshType.FullRect);
                    sprite.name = prefix + "_" + i.ToString("00");
                    AssetDatabase.CreateAsset(sprite, path);
                }
                result[i] = sprite;
            }
            return result;
        }

        static Sprite CreatePixelSprite(string name, int width, int height, Func<int, int, Color> draw, float ppu, Vector2 pivot)
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
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) pixels[y * width + x] = draw(x, y);
            texture.SetPixels(pixels);
            texture.Apply();
            EditorUtility.SetDirty(texture);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, ppu, 0, SpriteMeshType.FullRect);
                sprite.name = name;
                AssetDatabase.CreateAsset(sprite, spritePath);
            }
            return sprite;
        }

        static Color DrawGlassDoor(int x, int y)
        {
            const int width = 22;
            const int height = 52;
            bool frame = x == 0 || x == width - 1 || y == 0 || y == height - 1;
            if (frame) return new Color(0.18f, 0.27f, 0.39f, 0.86f);
            if (x == 2 || x == width - 3) return new Color(0.75f, 0.90f, 1f, 0.56f);
            if (x == 4 && y > 8 && y < height - 8) return new Color(0.84f, 0.94f, 1f, 0.24f);
            if (x == width - 5 && y > height / 2 && y < height - 7) return new Color(0.98f, 0.82f, 0.55f, 0.42f);
            return new Color(0.36f, 0.57f, 0.76f, 0.18f);
        }

        static Color DrawGlassGlint(int x, int y)
        {
            if (x == 4 && y > 8 && y < 40) return new Color(0.78f, 0.92f, 1f, 0.75f);
            if (x == 5 && y > 12 && y < 36) return new Color(0.78f, 0.92f, 1f, 0.34f);
            if (x == 13 && y > 5 && y < 24) return new Color(1f, 0.88f, 0.70f, 0.28f);
            return Color.clear;
        }

        static Color DrawRadio(int x, int y)
        {
            if (x >= 8 && y >= 28 && y <= 31) return new Color(0.12f, 0.18f, 0.25f, 1f);
            if (x >= 7 && x <= 10 && y >= 25 && y <= 30) return new Color(0.17f, 0.23f, 0.30f, 1f);
            if (x < 2 || x > 15 || y < 2 || y > 27) return Color.clear;
            bool edge = x == 2 || x == 15 || y == 2 || y == 27;
            if (edge) return new Color(0.07f, 0.10f, 0.14f, 1f);
            if (y >= 20 && y <= 24 && x >= 4 && x <= 13)
                return new Color(0.25f, 0.48f, 0.62f, 1f);
            if (y == 19 && x >= 4 && x <= 13) return new Color(0.48f, 0.76f, 0.88f, 1f);
            if (y >= 8 && y <= 16 && x >= 4 && x <= 13)
                return ((x + y) % 3 == 0) ? new Color(0.30f, 0.38f, 0.45f, 1f) : new Color(0.16f, 0.22f, 0.29f, 1f);
            if (y == 5 && x >= 5 && x <= 12) return new Color(0.72f, 0.48f, 0.27f, 1f);
            return new Color(0.20f, 0.27f, 0.34f, 1f);
        }

        static Color DrawGuardWaveArm(int x, int y)
        {
            bool sleeve = (x >= 2 && x <= 8 && y >= 2 && y <= 11) ||
                          (x >= 6 && x <= 10 && y >= 8 && y <= 15);
            bool forearm = (x >= 8 && x <= 12 && y >= 13 && y <= 21) ||
                           (x >= 7 && x <= 14 && y >= 19 && y <= 25);
            bool fingers = (x >= 8 && x <= 10 && y >= 24 && y <= 29) ||
                           (x >= 11 && x <= 13 && y >= 25 && y <= 29) ||
                           (x == 14 && y >= 23 && y <= 27);
            if (!sleeve && !forearm && !fingers) return Color.clear;
            if (sleeve)
            {
                bool edge = x == 2 || x == 8 || y == 2 || (x == 10 && y >= 9 && y <= 13);
                if (edge) return new Color(0.10f, 0.17f, 0.24f, 1f);
                if (x == 4 && y > 4 && y < 10) return new Color(0.53f, 0.70f, 0.82f, 1f);
                return new Color(0.32f, 0.50f, 0.64f, 1f);
            }
            bool skinEdge = x == 7 || x == 14 || y == 19 || (y == 29 && (x == 8 || x == 11));
            if (skinEdge) return new Color(0.27f, 0.21f, 0.18f, 1f);
            if (y == 22 && x >= 9 && x <= 12) return new Color(0.90f, 0.67f, 0.48f, 1f);
            return new Color(0.73f, 0.48f, 0.33f, 1f);
        }

        static Color DrawLeaf(int x, int y)
        {
            float center = 4.5f + 0.16f * x;
            if (Mathf.Abs(y - center) > 3.2f - Mathf.Abs(x - 11.5f) * 0.20f) return Color.clear;
            if (Mathf.Abs(y - center) < 0.7f) return new Color(0.79f, 0.76f, 0.48f, 1f);
            return y > center ? new Color(0.29f, 0.48f, 0.27f, 1f) : new Color(0.40f, 0.60f, 0.33f, 1f);
        }

        static Color DrawShadow(int x, int y)
        {
            float nx = (x - 19.5f) / 19.5f;
            float ny = (y - 5.5f) / 5.5f;
            float d = nx * nx + ny * ny;
            float a = Mathf.Clamp01((1f - d) * 0.38f);
            return new Color(0.018f, 0.026f, 0.040f, a);
        }

        static Color DrawSoftLight(int x, int y)
        {
            float nx = (x - 31.5f) / 31.5f;
            float ny = (y - 11.5f) / 11.5f;
            float distance = nx * nx + ny * ny;
            float alpha = Mathf.Clamp01((1f - distance) * 0.28f);
            return new Color(1f, 1f, 1f, alpha);
        }

        static SpriteRenderer AddFireflyLight(Transform parent, Sprite pixel, Vector3 position, float size)
        {
            SpriteRenderer light = MakeWorldSprite(parent, "Ponto de luz filtrado pelas folhas", pixel, position, 7,
                new Vector3(size, size, 1f));
            light.color = new Color(1f, 0.88f, 0.58f, 0.58f);
            return light;
        }

        static Transform MakePerson(Transform parent, string name, Sprite sprite, Vector3 position, float scale, int order, Sprite shadow)
        {
            SpriteRenderer shadowRenderer = MakeWorldSprite(parent, "Sombra sob " + name, shadow,
                new Vector3(position.x, position.y + 0.01f, position.z + 0.01f), order - 1,
                new Vector3(scale * 0.72f, scale * 0.32f, 1f));
            shadowRenderer.color = new Color(0.018f, 0.026f, 0.038f, 0.32f);
            return MakeCharacter(parent, name, sprite, position, order, Vector3.one * scale);
        }

        static SpriteRenderer MakeWorldSprite(Transform parent, string name, Sprite sprite, Vector3 position, int order, Vector3 scale)
        {
            Transform transform = MakeCharacter(parent, name, sprite, position, order, scale);
            return transform.GetComponent<SpriteRenderer>();
        }

        static Transform MakeLocalCharacter(Transform parent, string name, Sprite sprite, Vector3 localPosition, int order, Vector3 scale)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.color = Color.white;
            return go.transform;
        }

        static SpriteRenderer MakeLocalSprite(Transform parent, string name, Sprite sprite, Vector3 localPosition, int order, Vector3 scale)
            => MakeLocalCharacter(parent, name, sprite, localPosition, order, scale).GetComponent<SpriteRenderer>();

        static Transform MakeCharacter(Transform parent, string name, Sprite sprite, Vector3 position, int order, Vector3 scale)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.color = Color.white;
            return go.transform;
        }

        static Transform MakeRoot(string name)
        {
            GameObject go = new GameObject(name);
            return go.transform;
        }

        static Canvas MakeCanvas(string name)
        {
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(384, 216);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        static Image MakeImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Text text = go.GetComponent<Text>();
            if (uiFont == null)
            {
                uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
                if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
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
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        static AudioClip LoadAudio(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);

        static void SetObject(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new MissingFieldException(target.GetType().Name, name);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetObject(UnityEngine.Object target, string name, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new MissingFieldException(target.GetType().Name, name);
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetObject(UnityEngine.Object target, string name, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new MissingFieldException(target.GetType().Name, name);
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetObjectArray(UnityEngine.Object target, string name, UnityEngine.Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new MissingFieldException(target.GetType().Name, name);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene item in current) if (item.path == scenePath) return;
            var updated = new EditorBuildSettingsScene[current.Length + 1];
            Array.Copy(current, updated, current.Length);
            updated[current.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = updated;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
