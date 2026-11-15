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
    public static class CC05SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/CC05_A_Obra_no_Escuro.unity";
        const string ArtPath = "Assets/Art/CC05";
        const string AudioPath = "Assets/Audio/CC05";
        const string CharacterPath = "Assets/Art/Characters";
        const string CharacterSpritesPath = "Assets/Art/CC04A";
        const int SampleRate = 22050;
        static Font uiFont;
        static Color[] skySourcePixels;
        static int skySourceWidth;
        static int skySourceHeight;

        [MenuItem("Misterio/Build CC05 - A obra no escuro")]
        public static void Build()
        {
            EnsureFolders();
            ImportSceneArt();
            uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Sprite background = LoadSprite(ArtPath + "/fundo_andar4.png");
            Sprite panelBase = LoadSprite(ArtPath + "/painel.png");
            Sprite sculpture = LoadSprite(ArtPath + "/obra_apagada.png");
            Sprite[] panelSparks = LoadPanelSparkFrames();
            Sprite[] guardIdleFront = LoadGuardFrames("GuardIdleFront", 5);
            Sprite[] guardIdleBack = LoadGuardFrames("GuardIdleBack", 5);
            Sprite[] guardWalkFront = LoadGuardFrames("GuardWalkFront", 8);
            Sprite[] moemaIdleFront = LoadMoemaFrames("Moema_Idle_Frente", 4);
            Sprite[] moemaWalkBack = LoadMoemaFrames("Moema_Andar_Costas", 8);
            if (background == null || panelBase == null || sculpture == null || panelSparks[0] == null ||
                guardIdleFront[0] == null || guardIdleBack[0] == null || guardWalkFront[0] == null ||
                moemaIdleFront[0] == null || moemaWalkBack[0] == null)
                throw new InvalidOperationException("CC05 needs the validated fourth-floor art and the existing guard/Moema frame sets.");

            Sprite skyNightSprite = CreateWindowSkySprite("cc05_window_sky_night", ArtPath + "/ceu_noite.png");
            Sprite skyDawnSprite = CreateWindowSkySprite("cc05_window_sky_dawn", ArtPath + "/ceu_amanhecer.png");
            Sprite conduitSprite = CreatePixelSprite("cc05_damaged_conduits", 640, 360, DrawConduitPixel, 64f);
            Sprite beamSprite = CreatePixelSprite("cc05_flashlight_beam", 192, 72, DrawBeamPixel, 64f);
            Sprite glowSprite = CreatePixelSprite("cc05_panel_glow", 96, 96, DrawPanelGlowPixel, 64f);
            Sprite shadowSprite = CreatePixelSprite("cc05_character_shadow", 48, 12, DrawShadowPixel, 64f);
            Sprite sparkSprite = CreatePixelSprite("cc05_wire_spark", 20, 18, DrawWireSparkPixel, 64f);
            Sprite moteSprite = CreatePixelSprite("cc05_dust_mote", 4, 4, DrawDustPixel, 64f);
            Sprite cabSprite = CreatePixelSprite("cc05_elevator_cab", 68, 100, DrawElevatorCabPixel, 64f);
            Sprite doorSprite = CreatePixelSprite("cc05_elevator_door", 34, 96, DrawElevatorDoorPixel, 64f);
            Sprite whitePixel = CreatePixelSprite("cc05_white_pixel", 1, 1, (x, y) => Color.white, 1f);

            AudioClip roomHum = CreateWaveAsset("cc05_zumbido_eletrico.wav", 10f, RoomHumSample);
            AudioClip electricalSnap = CreateWaveAsset("cc05_estalo_eletrico.wav", 0.24f, ElectricalSnapSample);
            AudioClip music = CreateWaveAsset("cc05_trilha_tensao.wav", 10f, TensionMusicSample);
            AudioClip elevatorMotor = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CC04/cc04_motor_elevador.wav");
            AudioClip step = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CC04/cc04_passo_abafado.wav");
            AudioClip windowCreak = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CC04/cc04_rangido_leve.wav");
            AudioClip feathers = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CC04A/cc04a_penas_rocam.wav");
            AudioClip trotStep = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CC04A/cc04a_passos_trote.wav");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Câmera - quarto andar 640x360");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 1.92f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.030f, 0.075f);
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = Vector3.up;
            camera.transform.position = new Vector3(-2.48f, 0.10f, -10f);
            cameraObject.AddComponent<AudioListener>();

            Transform roomRoot = new GameObject("CC05 - Salão Principal, quarto andar").transform;
            MakeWorldSprite(roomRoot, "Fundo original - salão à noite", background, Vector3.zero, -100, Vector3.one);
            SpriteRenderer nightSky = MakeWorldSprite(roomRoot, "Vidros da basculante - céu noturno", skyNightSprite,
                new Vector3(0f, 0f, -0.02f), -90, Vector3.one);
            SpriteRenderer dawnSky = MakeWorldSprite(roomRoot, "Vidros da basculante - amanhecer gradual", skyDawnSprite,
                new Vector3(0f, 0f, -0.03f), -89, Vector3.one);
            MakeWorldSprite(roomRoot, "Conduítes - trechos interrompidos e isolamento danificado", conduitSprite,
                new Vector3(0f, 0f, -0.04f), -20, Vector3.one);

            SpriteRenderer elevatorCab = MakeWorldSprite(roomRoot, "Interior escuro do elevador", cabSprite,
                new Vector3(-2.40f, 0.40f, -0.08f), 0, Vector3.one);
            SpriteRenderer leftDoor = MakeWorldSprite(roomRoot, "Porta esquerda do elevador", doorSprite,
                new Vector3(-2.665f, 0.40f, -0.09f), 1, Vector3.one);
            SpriteRenderer rightDoor = MakeWorldSprite(roomRoot, "Porta direita do elevador", doorSprite,
                new Vector3(-2.135f, 0.40f, -0.09f), 1, Vector3.one);
            rightDoor.flipX = true;

            SpriteRenderer panelGlow = MakeWorldSprite(roomRoot, "Luz contida no quadro elétrico", glowSprite,
                new Vector3(-4.20f, 0.83f, -0.15f), 3, new Vector3(1.38f, 1.20f, 1f));
            panelGlow.color = new Color(0.78f, 0.83f, 1f, 0f);
            SpriteRenderer panel = MakeWorldSprite(roomRoot, "Painel aberto - fios expostos", panelBase,
                new Vector3(-4.20f, 0.83f, -0.17f), 7, Vector3.one);
            panel.color = new Color(0.89f, 0.91f, 0.94f, 1f);
            SpriteRenderer wireSpark = MakeWorldSprite(roomRoot, "Arco breve no conduíte rompido", sparkSprite,
                new Vector3(2.42f, 0.72f, -0.20f), 6, new Vector3(0.78f, 0.78f, 1f));
            wireSpark.enabled = false;

            SpriteRenderer sculptureRenderer = MakeWorldSprite(roomRoot, "Obra principal - ainda sem energia", sculpture,
                new Vector3(0f, 0.76f, -0.13f), 5, new Vector3(0.92f, 0.92f, 1f));
            sculptureRenderer.color = new Color(0.72f, 0.77f, 0.90f, 1f);

            SpriteRenderer guardShadow = MakeWorldSprite(roomRoot, "Sombra baixa do segurança", shadowSprite,
                new Vector3(-2.40f, -1.34f, -0.04f), 6, new Vector3(1.32f, 1f, 1f));
            guardShadow.color = new Color(0.015f, 0.020f, 0.035f, 0.54f);
            GameObject guardObject = new GameObject("Segurança - sai do elevador e volta ao painel");
            guardObject.transform.position = new Vector3(-2.42f, -1.28f, -0.08f);
            guardObject.transform.localScale = Vector3.one * 1.08f;
            guardObject.transform.SetParent(roomRoot, true);
            SpriteRenderer guardRenderer = guardObject.AddComponent<SpriteRenderer>();
            guardRenderer.sprite = guardIdleFront[0];
            guardRenderer.sortingOrder = 9;

            SpriteRenderer moemaShadow = MakeWorldSprite(roomRoot, "Sombra de Moema no trajeto ao fundo", shadowSprite,
                new Vector3(-1.30f, -0.67f, -0.035f), 3, new Vector3(0.82f, 0.70f, 1f));
            moemaShadow.color = new Color(0.012f, 0.016f, 0.028f, 0.40f);
            GameObject moemaObject = new GameObject("Moema - atravessa o fundo e sobe pela basculante");
            moemaObject.transform.position = new Vector3(-1.30f, -0.58f, -0.06f);
            moemaObject.transform.localScale = Vector3.one * 0.34f;
            moemaObject.transform.SetParent(roomRoot, true);
            SpriteRenderer moemaRenderer = moemaObject.AddComponent<SpriteRenderer>();
            moemaRenderer.sprite = moemaIdleFront[0];
            moemaRenderer.sortingOrder = 4;

            Transform beam = new GameObject("Lanterna - facho curto no painel e nos fios").transform;
            beam.SetParent(roomRoot, true);
            beam.position = new Vector3(-3.2f, 0f, -0.22f);
            SpriteRenderer beamRenderer = beam.gameObject.AddComponent<SpriteRenderer>();
            beamRenderer.sprite = beamSprite;
            beamRenderer.sortingOrder = 6;
            beamRenderer.color = new Color(0.91f, 0.91f, 0.79f, 0f);

            SpriteRenderer[] dust = new SpriteRenderer[8];
            for (int i = 0; i < dust.Length; i++)
            {
                dust[i] = MakeWorldSprite(roomRoot, $"Poeira suspensa no facho {i + 1:00}", moteSprite,
                    new Vector3(-4f, -3f, -0.24f), 7, Vector3.one * (0.60f + 0.07f * (i % 3)));
                dust[i].enabled = false;
            }

            Canvas canvas = MakeCanvas("CC05 - Legendas e relógio da partida");
            Image vignette = MakeImage(canvas.transform, "Vinheta discreta de cinema", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.20f));
            vignette.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC01/cinematic_vignette.asset");
            vignette.raycastTarget = false;
            vignette.transform.SetAsFirstSibling();

            Image clockPlate = MakeImage(canvas.transform, "Visor do relógio global", Vector2.one, Vector2.one,
                new Vector2(-160f, -64f), new Vector2(-12f, -12f), new Color(0.012f, 0.022f, 0.052f, 0.84f));
            clockPlate.sprite = whitePixel;
            Outline clockOutline = clockPlate.gameObject.AddComponent<Outline>();
            clockOutline.effectColor = new Color(0.42f, 0.55f, 0.76f, 0.72f);
            clockOutline.effectDistance = new Vector2(1f, -1f);
            Text clockLabel = MakeText(canvas.transform, "Rótulo - hora global", Vector2.one, Vector2.one,
                new Vector2(-148f, -37f), new Vector2(-24f, -17f), 9, TextAnchor.MiddleRight,
                new Color(0.58f, 0.70f, 0.88f, 1f));
            clockLabel.text = "HORA GLOBAL";
            clockLabel.fontStyle = FontStyle.Bold;
            Text clockValue = MakeText(canvas.transform, "Hora atual da partida", Vector2.one, Vector2.one,
                new Vector2(-148f, -60f), new Vector2(-24f, -35f), 17, TextAnchor.MiddleRight,
                new Color(0.90f, 0.94f, 1f, 1f));
            clockValue.text = "--:--";
            clockValue.fontStyle = FontStyle.Bold;

            Image subtitleBackground = MakeImage(canvas.transform, "Faixa baixa de legenda", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 70f), new Color(0.010f, 0.018f, 0.040f, 0.80f));
            subtitleBackground.sprite = whitePixel;
            Text subtitle = MakeText(canvas.transform, "Falas da narrativa", new Vector2(0.035f, 0f), new Vector2(0.965f, 0f),
                new Vector2(0f, 6f), new Vector2(0f, 65f), 14, TextAnchor.MiddleCenter, Color.white);
            subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitle.verticalOverflow = VerticalWrapMode.Overflow;
            subtitle.lineSpacing = 0.94f;
            subtitle.fontStyle = FontStyle.Bold;
            subtitle.raycastTarget = false;
            subtitle.enabled = false;
            subtitleBackground.enabled = false;

            GameObject audioObject = new GameObject("CC05 - Som do salão e da saída");
            AudioSource voiceSource = MakeAudioSource(audioObject, "Vozes - pronto para gravação");
            AudioSource ambienceSource = MakeAudioSource(audioObject, "Zumbido elétrico irregular");
            AudioSource effectsSource = MakeAudioSource(audioObject, "Elevador, faísca, passos e janela");
            AudioSource musicSource = MakeAudioSource(audioObject, "Trilha baixa com pulso ao amanhecer");

            GameObject controllerObject = new GameObject("CC05 - A obra no escuro (10s)");
            CC05CutsceneController controller = controllerObject.AddComponent<CC05CutsceneController>();
            SetObject(controller, "sceneCamera", camera);
            SetObject(controller, "nightSky", nightSky);
            SetObject(controller, "dawnSky", dawnSky);
            SetObject(controller, "flashlightBeam", beamRenderer);
            SetObject(controller, "flashlightBeamTransform", beam);
            SetObject(controller, "panelGlow", panelGlow);
            SetObject(controller, "damagedWireSpark", wireSpark);
            SetObject(controller, "elevatorCab", elevatorCab);
            SetObject(controller, "elevatorDoorLeft", leftDoor);
            SetObject(controller, "elevatorDoorRight", rightDoor);
            SetObjectArray(controller, "elevatorDoorDetails", new UnityEngine.Object[] { doorSprite });
            SetObject(controller, "guard", guardObject.transform);
            SetObject(controller, "guardRenderer", guardRenderer);
            SetObjectArray(controller, "guardIdleFront", guardIdleFront);
            SetObjectArray(controller, "guardIdleBack", guardIdleBack);
            SetObjectArray(controller, "guardWalkFront", guardWalkFront);
            SetObject(controller, "guardShadow", guardShadow);
            SetObject(controller, "moema", moemaObject.transform);
            SetObject(controller, "moemaRenderer", moemaRenderer);
            SetObjectArray(controller, "moemaIdleFront", moemaIdleFront);
            SetObjectArray(controller, "moemaWalkBack", moemaWalkBack);
            SetObject(controller, "moemaShadow", moemaShadow);
            SetObject(controller, "panelRenderer", panel);
            SetObject(controller, "panelBase", panelBase);
            SetObjectArray(controller, "panelSparkFrames", panelSparks);
            SetObject(controller, "sculptureRenderer", sculptureRenderer);
            SetObjectArray(controller, "beamDust", dust);
            SetObject(controller, "subtitleText", subtitle);
            SetObject(controller, "subtitleBackground", subtitleBackground);
            SetObject(controller, "clockPlate", clockPlate);
            SetObject(controller, "clockLabel", clockLabel);
            SetObject(controller, "clockValueText", clockValue);
            SetObject(controller, "voiceSource", voiceSource);
            SetObject(controller, "ambienceSource", ambienceSource);
            SetObject(controller, "effectsSource", effectsSource);
            SetObject(controller, "musicSource", musicSource);
            SetObject(controller, "elevatorMotor", elevatorMotor);
            SetObject(controller, "electricalHum", roomHum);
            SetObject(controller, "electricalSnap", electricalSnap);
            SetObject(controller, "windowCreak", windowCreak);
            SetObject(controller, "featherRustle", feathers);
            SetObject(controller, "guardStep", step);
            SetObject(controller, "moemaStep", trotStep);
            SetObject(controller, "restrainedMusic", music);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CC05 built: the elevator opens onto the damaged fourth-floor circuit, Moema climbs out through the awning window, the global-clock display accepts the live run time, and the guard faces the puzzle.");
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder(ArtPath);
            EnsureFolder("Assets/Audio");
            EnsureFolder(AudioPath);
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Scripts/CC05");
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
            AssetDatabase.Refresh();
            ConfigureSpriteTexture(ArtPath + "/fundo_andar4.png", 64f, false, true);
            ConfigureSpriteTexture(ArtPath + "/painel.png", 64f, true, true);
            ConfigureSpriteTexture(ArtPath + "/obra_apagada.png", 64f, true, true);
            ConfigureSpriteTexture(ArtPath + "/painel_faiscas_sheet.png", 64f, true, true);
            ConfigureSpriteTexture(ArtPath + "/ceu_amanhecer.png", 64f, false, true);
            ConfigureSpriteTexture(ArtPath + "/ceu_noite.png", 64f, false, true);
            string[] moemaFrames = Directory.GetFiles(CharacterPath, "Moema_*_Frame_*.png");
            foreach (string absolute in moemaFrames)
                ConfigureCharacterFrameTexture(absolute.Replace('\\', '/'));
            AssetDatabase.Refresh();
        }

        static void ConfigureSpriteTexture(string path, float pixelsPerUnit, bool transparent, bool readable)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing CC05 art: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = transparent;
            importer.isReadable = readable;
            importer.SaveAndReimport();
        }

        static void ConfigureCharacterFrameTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing Moema frame: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        static Sprite[] LoadPanelSparkFrames()
        {
            const string texturePath = ArtPath + "/painel_faiscas_sheet.png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) throw new FileNotFoundException("Missing three-frame panel spark sheet.");
            var frames = new Sprite[3];
            for (int i = 0; i < frames.Length; i++)
            {
                string path = ArtPath + "/painel_faiscas_" + i.ToString("00") + ".asset";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    sprite = Sprite.Create(texture, new Rect(i * 62f, 0f, 62f, 71f),
                        new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
                    sprite.name = "PainelFaísca_" + i.ToString("00");
                    AssetDatabase.CreateAsset(sprite, path);
                }
                frames[i] = sprite;
            }
            return frames;
        }

        static Sprite[] LoadGuardFrames(string animation, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC01/" + animation + "_" + i.ToString("00") + ".asset");
            return frames;
        }

        static Sprite[] LoadMoemaFrames(string animation, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(CharacterSpritesPath + "/" + animation + "_Frame_" + i.ToString("00") + ".asset");
            return frames;
        }

        static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        static SpriteRenderer MakeWorldSprite(Transform parent, string name, Sprite sprite,
            Vector3 position, int sortingOrder, Vector3 scale)
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
            GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
                sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                    new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = name;
                AssetDatabase.CreateAsset(sprite, spritePath);
            }
            return sprite;
        }

        static Sprite CreateWindowSkySprite(string name, string sourcePath)
        {
            Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            if (source == null || !source.isReadable) throw new FileNotFoundException("Missing readable sky strip: " + sourcePath);
            skySourcePixels = source.GetPixels();
            skySourceWidth = source.width;
            skySourceHeight = source.height;
            Sprite sprite = CreatePixelSprite(name, 640, 360, DrawSkyPixel, 64f);
            skySourcePixels = null;
            return sprite;
        }

        static Color DrawSkyPixel(int x, int y)
        {
            int imageY = 359 - y;
            if (!InsideWindowGlass(x, imageY)) return Color.clear;
            if (skySourcePixels == null || skySourceWidth <= 0 || skySourceHeight <= 0) return Color.clear;
            float vertical = Mathf.Clamp01((imageY - 70f) / 48f);
            int sampleY = Mathf.Clamp(Mathf.RoundToInt((1f - vertical) * (skySourceHeight - 1)), 0, skySourceHeight - 1);
            Color color = skySourcePixels[sampleY * skySourceWidth + skySourceWidth / 2];
            color.a = 0.94f;
            return color;
        }

        static bool InsideWindowGlass(int x, int imageY)
        {
            if (imageY < 70 || imageY > 118) return false;
            float v = Mathf.InverseLerp(70f, 118f, imageY);
            float left = 348f - 5f * v;
            float right = 535f - 7f * v;
            if (x <= left + 2f || x >= right - 2f) return false;
            float[] bars = { 373f, 404f, 435f, 466f, 497f, 523f };
            for (int i = 0; i < bars.Length; i++)
                if (Mathf.Abs(x - (bars[i] - 5f * v)) <= 2.2f) return false;
            return true;
        }

        static Color DrawConduitPixel(int x, int y)
        {
            int imageY = 359 - y;
            float distance = 1000f;
            distance = Mathf.Min(distance, SegmentDistance(x, imageY, 58f, 103f, 64f, 97f));
            distance = Mathf.Min(distance, SegmentDistance(x, imageY, 92f, 95f, 218f, 95f));
            distance = Mathf.Min(distance, SegmentDistance(x, imageY, 250f, 97f, 383f, 97f));
            distance = Mathf.Min(distance, SegmentDistance(x, imageY, 416f, 132f, 467f, 132f));
            distance = Mathf.Min(distance, SegmentDistance(x, imageY, 485f, 132f, 520f, 129f));
            distance = Mathf.Min(distance, SegmentDistance(x, imageY, 520f, 129f, 526f, 117f));

            if (x >= 467 && x <= 485 && imageY >= 121 && imageY <= 136)
            {
                float brokenA = SegmentDistance(x, imageY, 468f, 131f, 473f, 125f);
                float brokenB = SegmentDistance(x, imageY, 478f, 130f, 483f, 123f);
                float brokenC = SegmentDistance(x, imageY, 473f, 126f, 477f, 129f);
                float broken = Mathf.Min(brokenA, Mathf.Min(brokenB, brokenC));
                if (broken < 1.35f) return new Color(0.30f, 0.18f, 0.17f, 0.90f);
                if (broken < 2.05f && ((x + imageY) % 3 == 0)) return new Color(0.47f, 0.29f, 0.20f, 0.64f);
                return Color.clear;
            }

            if (distance < 0.78f) return new Color(0.17f, 0.21f, 0.31f, 0.95f);
            if (distance < 1.55f) return new Color(0.055f, 0.075f, 0.12f, 0.95f);
            if (distance < 2.5f) return new Color(0.35f, 0.40f, 0.52f, 0.60f);
            return Color.clear;
        }

        static float SegmentDistance(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float denominator = dx * dx + dy * dy;
            float t = denominator < 0.001f ? 0f : Mathf.Clamp01(((px - ax) * dx + (py - ay) * dy) / denominator);
            float x = ax + t * dx;
            float y = ay + t * dy;
            float ex = px - x;
            float ey = py - y;
            return Mathf.Sqrt(ex * ex + ey * ey);
        }

        static Color DrawBeamPixel(int x, int y)
        {
            float u = (x + 0.5f) / 192f;
            float v = (y + 0.5f - 36f) / 36f;
            float halfWidth = Mathf.Lerp(0.035f, 0.46f, u);
            float edge = Mathf.Clamp01(1f - Mathf.Abs(v) / halfWidth);
            float shaped = Mathf.Floor(edge * 5f) / 5f;
            float alpha = shaped * (0.52f + 0.48f * Mathf.Sin(Mathf.PI * u)) * 0.43f;
            return new Color(0.88f, 0.90f, 0.72f, alpha);
        }

        static Color DrawPanelGlowPixel(int x, int y)
        {
            float nx = (x + 0.5f - 48f) / 48f;
            float ny = (y + 0.5f - 48f) / 48f;
            float radius = nx * nx * 0.85f + ny * ny;
            if (radius >= 1f) return Color.clear;
            float stepped = Mathf.Floor(Mathf.Clamp01(1f - radius) * 5f) / 5f;
            return new Color(0.42f, 0.56f, 0.80f, stepped * 0.34f);
        }

        static Color DrawShadowPixel(int x, int y)
        {
            float nx = (x + 0.5f - 24f) / 24f;
            float ny = (y + 0.5f - 6f) / 6f;
            float d = nx * nx * 0.78f + ny * ny;
            if (d >= 1f) return Color.clear;
            float alpha = Mathf.Floor(Mathf.Clamp01((1f - d) * 3f) * 3f) / 3f * 0.66f;
            return new Color(0.012f, 0.017f, 0.030f, alpha);
        }

        static Color DrawWireSparkPixel(int x, int y)
        {
            bool core = (x >= 9 && x <= 10 && y >= 7 && y <= 10) ||
                        (x == 8 && y == 10) || (x == 11 && y == 7);
            bool branch = (x == 6 && y == 14) || (x == 7 && y == 12) || (x == 13 && y == 13) ||
                          (x == 14 && y == 11) || (x == 4 && y == 6) || (x == 6 && y == 7) ||
                          (x == 14 && y == 5) || (x == 12 && y == 6) || (x == 10 && y == 15) ||
                          (x == 9 && y == 13) || (x == 5 && y == 11) || (x == 15 && y == 8);
            if (core) return new Color(1f, 0.94f, 0.70f, 1f);
            if (branch) return new Color(0.98f, 0.63f, 0.30f, 0.92f);
            if ((x == 7 && y == 13) || (x == 13 && y == 12) || (x == 5 && y == 7))
                return new Color(0.75f, 0.42f, 0.28f, 0.72f);
            return Color.clear;
        }

        static Color DrawDustPixel(int x, int y)
        {
            bool pixel = (x == 1 && y == 1) || (x == 2 && y == 1) || (x == 2 && y == 2);
            return pixel ? new Color(0.92f, 0.90f, 0.72f, 0.82f) : Color.clear;
        }

        static Color DrawElevatorCabPixel(int x, int y)
        {
            float side = Mathf.Min(x, 67 - x) / 34f;
            float top = Mathf.Min(y, 99 - y) / 50f;
            float interior = 0.06f + 0.04f * Mathf.Clamp01(side) + 0.025f * Mathf.Clamp01(top);
            bool seam = (x == 2 || x == 65 || y == 2 || y == 97);
            if (seam) return new Color(0.10f, 0.13f, 0.20f, 1f);
            return new Color(interior * 0.65f, interior * 0.76f, interior * 1.08f, 1f);
        }

        static Color DrawElevatorDoorPixel(int x, int y)
        {
            float side = Mathf.Exp(-Mathf.Pow((x - 5f) / 4f, 2f)) * 0.12f +
                         Mathf.Exp(-Mathf.Pow((x - 30f) / 3.8f, 2f)) * 0.08f;
            float grain = 0.012f * Mathf.Sin(x * 0.72f + y * 0.035f) + 0.007f * Mathf.Sin(x * 1.56f);
            float level = 0.34f + side + grain;
            if (x < 2 || x > 31 || y < 2 || y > 93) level *= 0.62f;
            if (x == 16 || x == 17) level *= 0.73f;
            return new Color(level * 0.78f, level * 0.88f, level * 1.04f, 1f);
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

        static float RoomHumSample(float t)
        {
            float low = 0.007f * Mathf.Sin(2f * Mathf.PI * 49.8f * t) +
                        0.0034f * Mathf.Sin(2f * Mathf.PI * 99.6f * t + 0.16f);
            float irregular = 0.0015f * (Mathf.PerlinNoise(t * 9.7f, 0.41f) - 0.5f);
            float pulse = 0.0010f * Mathf.Sin(2f * Mathf.PI * 0.31f * t);
            return low + irregular + pulse;
        }

        static float ElectricalSnapSample(float t)
        {
            float envelope = Mathf.Exp(-t * 28f) * Mathf.Clamp01(t * 110f);
            float crackle = Mathf.PerlinNoise(4.1f + t * 410f, 0.27f) - 0.5f;
            float tone = Mathf.Sin(2f * Mathf.PI * (1280f - 410f * t) * t);
            return envelope * (0.14f * crackle + 0.045f * tone);
        }

        static float TensionMusicSample(float t)
        {
            float fadeIn = Mathf.Clamp01(t * 1.8f);
            float fadeOut = Mathf.Clamp01((10f - t) * 3f);
            float pulseStart = Mathf.Clamp01((t - 5.7f) / 2.2f);
            float pulse = 0.58f + 0.42f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 0.54f * t)), 5f) * pulseStart;
            float bed = 0.010f * Mathf.Sin(2f * Mathf.PI * 130.81f * t) +
                        0.005f * Mathf.Sin(2f * Mathf.PI * 196f * t + 0.24f) +
                        0.0025f * Mathf.Sin(2f * Mathf.PI * 261.63f * t + 0.61f);
            float high = 0.0013f * Mathf.Sin(2f * Mathf.PI * 392f * t) * Mathf.Clamp01((t - 6f) / 2f);
            return (bed + high) * pulse * fadeIn * fadeOut;
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
            SerializedObject serialized = new SerializedObject(target);
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
