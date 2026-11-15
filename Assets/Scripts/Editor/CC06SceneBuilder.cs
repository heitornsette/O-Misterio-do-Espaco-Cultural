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
    public static class CC06SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/CC06_A_Energia_Retorna.unity";
        const string ArtPath = "Assets/Art/CC06";
        static Font uiFont;

        [MenuItem("Misterio/Build CC06 - A energia retorna")]
        public static void Build()
        {
            EnsureFolder(ArtPath);
            ImportSourceArt();
            uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Sprite background = LoadSprite("Assets/Art/CC05/fundo_andar4.png");
            Sprite panel = LoadSprite("Assets/Art/CC05/painel.png");
            Sprite sculpture = LoadSprite("Assets/Art/CC05/obra_apagada.png");
            Sprite mothSprite = LoadSprite("Assets/Art/Characters/Mariposa_Base.png");
            Sprite conduitSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC05/cc05_damaged_conduits.asset");
            Sprite nightSkySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC05/cc05_window_sky_night.asset");
            Sprite dawnSkySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC05/cc05_window_sky_dawn.asset");
            Sprite[] guardIdleFront = LoadGuardFrames("GuardIdleFront", 5);
            Sprite[] guardIdleBack = LoadGuardFrames("GuardIdleBack", 5);
            if (background == null || panel == null || sculpture == null || mothSprite == null || conduitSprite == null ||
                nightSkySprite == null || dawnSkySprite == null || guardIdleFront[0] == null || guardIdleBack[0] == null)
                throw new InvalidOperationException("CC06 needs the fourth-floor background, panel, artwork, moth, sky layers and guard idle cycles.");

            Sprite glow = CreatePixelSprite("cc06_soft_pixel_glow", 128, 128, DrawSoftGlow, 64f);
            Sprite floorGlow = CreatePixelSprite("cc06_floor_reflection", 160, 48, DrawFloorGlow, 64f);
            Sprite wireLeads = CreatePixelSprite("cc06_conduit_terminal_leads", 18, 8, DrawWireLeads, 64f);
            Sprite bridge = CreatePixelSprite("cc06_closed_circuit_bridge", 6, 4, DrawBridge, 64f);
            Sprite pulse = CreatePixelSprite("cc06_current_pulse", 16, 16, DrawPulse, 64f);
            Sprite whitePixel = CreatePixelSprite("cc06_ui_white_pixel", 1, 1, (x, y) => Color.white, 1f);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Câmera - clímax do quarto andar");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 0.76f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.024f, 0.061f);
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = Vector3.up;
            camera.transform.position = new Vector3(2.39f, 0.77f, -10f);
            cameraObject.AddComponent<AudioListener>();

            Transform roomRoot = new GameObject("CC06 - Salão Principal, circuito restaurado").transform;
            MakeWorldSprite(roomRoot, "Salão original à noite", background, Vector3.zero, -100, Vector3.one);
            SpriteRenderer nightSky = MakeWorldSprite(roomRoot, "Céu noturno nas vidraças", nightSkySprite,
                new Vector3(0f, 0f, -0.02f), -90, Vector3.one);
            SpriteRenderer dawnSky = MakeWorldSprite(roomRoot, "Amanhecer entrando pela basculante", dawnSkySprite,
                new Vector3(0f, 0f, -0.03f), -89, Vector3.one);
            SpriteRenderer conduits = MakeWorldSprite(roomRoot, "Conduítes originais do salão", conduitSprite,
                new Vector3(0f, 0f, -0.04f), -20, Vector3.one);

            SpriteRenderer[] pools = new SpriteRenderer[3];
            SpriteRenderer[] fixtures = new SpriteRenderer[3];
            float[] lightX = { -2.80f, 0f, 2.72f };
            for (int i = 0; i < 3; i++)
            {
                pools[i] = MakeWorldSprite(roomRoot, $"Foco de luz no piso {i + 1:00}", glow,
                    new Vector3(lightX[i], -0.20f, -0.32f), 3, new Vector3(1.55f, 2.35f, 1f));
                pools[i].color = new Color(1f, 0.78f, 0.52f, 0f);
                fixtures[i] = MakeWorldSprite(roomRoot, $"Luminária restaurada {i + 1:00}", glow,
                    new Vector3(lightX[i], 1.69f, -0.26f), 8, new Vector3(0.18f, 0.18f, 1f));
                fixtures[i].color = new Color(1f, 0.90f, 0.69f, 0f);
            }

            SpriteRenderer reflection = MakeWorldSprite(roomRoot, "Reflexo quente e discreto no piso", floorGlow,
                new Vector3(0f, -0.84f, -0.24f), 4, new Vector3(2.65f, 0.92f, 1f));
            reflection.color = new Color(1f, 0.72f, 0.42f, 0f);
            SpriteRenderer artworkGlow = MakeWorldSprite(roomRoot, "Luz de amanhecer na borda da obra", glow,
                new Vector3(0f, 0.60f, -0.18f), 5, new Vector3(1.72f, 1.76f, 1f));
            artworkGlow.color = new Color(1f, 0.67f, 0.37f, 0f);
            SpriteRenderer sculptureRenderer = MakeWorldSprite(roomRoot, "Obra principal - intacta sob os focos", sculpture,
                new Vector3(0f, 0.76f, -0.13f), 7, new Vector3(0.92f, 0.92f, 1f));
            sculptureRenderer.color = new Color(0.71f, 0.77f, 0.91f, 1f);

            SpriteRenderer leadsRenderer = MakeWorldSprite(roomRoot, "Terminais interrompidos do último conduíte", wireLeads,
                new Vector3(2.40f, 0.82f, -0.185f), 8, Vector3.one);
            leadsRenderer.transform.rotation = Quaternion.Euler(0f, 0f, -42f);

            SpriteRenderer panelRenderer = MakeWorldSprite(roomRoot, "Painel elétrico já fechado", panel,
                new Vector3(-4.20f, 0.83f, -0.17f), 7, Vector3.one);
            panelRenderer.color = new Color(0.89f, 0.91f, 0.94f, 1f);

            SpriteRenderer bridgeRenderer = MakeWorldSprite(roomRoot, "Último conduíte se encaixa sem faísca", bridge,
                new Vector3(2.40f, 0.82f, -0.19f), 9, Vector3.one);
            bridgeRenderer.transform.rotation = Quaternion.Euler(0f, 0f, -42f);
            bridgeRenderer.color = new Color(0.65f, 0.91f, 1f, 0f);
            SpriteRenderer[] pulseNodes = new SpriteRenderer[2];
            for (int i = 0; i < pulseNodes.Length; i++)
            {
                pulseNodes[i] = MakeWorldSprite(roomRoot, $"Pulso azul e branco {i + 1:00}", pulse,
                    new Vector3(2.40f, 0.82f, -0.22f), 10, Vector3.one);
                pulseNodes[i].enabled = false;
            }

            SpriteRenderer shadow = MakeWorldSprite(roomRoot, "Sombra baixa do segurança", floorGlow,
                new Vector3(-2.24f, -1.30f, -0.045f), 5, new Vector3(0.68f, 0.35f, 1f));
            shadow.color = new Color(0.01f, 0.016f, 0.03f, 0.55f);
            GameObject guard = new GameObject("Segurança - acompanha a luz e relaxa os ombros");
            guard.transform.position = new Vector3(-2.24f, -1.18f, -0.08f);
            guard.transform.localScale = Vector3.one * 1.12f;
            guard.transform.SetParent(roomRoot, true);
            SpriteRenderer guardRenderer = guard.AddComponent<SpriteRenderer>();
            guardRenderer.sprite = guardIdleFront[0];
            guardRenderer.sortingOrder = 9;

            SpriteRenderer mothShadow = MakeWorldSprite(roomRoot, "Sombra quase imperceptível da mariposa", floorGlow,
                new Vector3(-4.02f, 0.68f, -0.045f), 6, new Vector3(0.30f, 0.20f, 1f));
            mothShadow.color = new Color(0.016f, 0.019f, 0.030f, 0f);
            GameObject moth = new GameObject("Mariposa - sai do painel e alcança a janela");
            moth.transform.position = new Vector3(-4.02f, 0.92f, -0.10f);
            moth.transform.localScale = Vector3.one * 0.68f;
            moth.transform.SetParent(roomRoot, true);
            SpriteRenderer mothRenderer = moth.AddComponent<SpriteRenderer>();
            mothRenderer.sprite = mothSprite;
            mothRenderer.sortingOrder = 11;
            mothRenderer.color = new Color(1f, 1f, 1f, 0f);

            Canvas canvas = MakeCanvas("CC06 - Legendas e relógio da partida");
            Image clockPlate = MakeImage(canvas.transform, "Visor do relógio global", Vector2.one, Vector2.one,
                new Vector2(-160f, -64f), new Vector2(-12f, -12f), new Color(0.012f, 0.022f, 0.052f, 0.84f));
            clockPlate.sprite = whitePixel;
            Outline clockOutline = clockPlate.gameObject.AddComponent<Outline>();
            clockOutline.effectColor = new Color(0.42f, 0.55f, 0.76f, 0.72f);
            clockOutline.effectDistance = new Vector2(1f, -1f);
            Text clockLabel = MakeText(canvas.transform, "Rótulo da hora global", Vector2.one, Vector2.one,
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
                Vector2.zero, new Vector2(0f, 70f), new Color(0.010f, 0.018f, 0.040f, 0.80f));
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
            Image fade = MakeImage(canvas.transform, "Entrada e saída em preto", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            fade.sprite = whitePixel;

            GameObject audioObject = new GameObject("CC06 - Ambiência e resolução musical");
            AudioSource voiceSource = MakeAudioSource(audioObject, "Narração e fala do segurança");
            AudioSource ambienceSource = MakeAudioSource(audioObject, "Zumbido elétrico se estabiliza");
            AudioSource effectsSource = MakeAudioSource(audioObject, "Encaixe e acentos de luz");
            AudioSource musicSource = MakeAudioSource(audioObject, "Tema matinal resolvido");
            AudioClip electricalHum = LoadAudio("Assets/Audio/CC05/cc05_zumbido_eletrico.wav");
            AudioClip circuitClick = LoadAudio("Assets/Audio/CC02/cc02_confirmacao.wav");
            AudioClip lightChime = LoadAudio("Assets/Audio/CC04A/cc04a_acento_xilofone.wav");
            AudioClip music = LoadAudio("Assets/Audio/CC02/cc02_xilofone_escadaria.wav");

            GameObject controllerObject = new GameObject("CC06 - A energia retorna (12s)");
            CC06CutsceneController controller = controllerObject.AddComponent<CC06CutsceneController>();
            SetObject(controller, "sceneCamera", camera);
            SetObject(controller, "nightSky", nightSky);
            SetObject(controller, "dawnSky", dawnSky);
            SetObject(controller, "conduitRoute", conduits);
            SetObject(controller, "connectionLeads", leadsRenderer);
            SetObject(controller, "connectionBridge", bridgeRenderer);
            SetObjectArray(controller, "pulseNodes", pulseNodes);
            SetObjectArray(controller, "lightPools", pools);
            SetObjectArray(controller, "fixtures", fixtures);
            SetObject(controller, "sculptureRenderer", sculptureRenderer);
            SetObject(controller, "artworkGlow", artworkGlow);
            SetObject(controller, "floorReflection", reflection);
            SetObject(controller, "guard", guard.transform);
            SetObject(controller, "guardRenderer", guardRenderer);
            SetObjectArray(controller, "guardIdleFront", guardIdleFront);
            SetObjectArray(controller, "guardIdleBack", guardIdleBack);
            SetObject(controller, "guardShadow", shadow);
            SetObject(controller, "moth", moth.transform);
            SetObject(controller, "mothRenderer", mothRenderer);
            SetObject(controller, "mothShadow", mothShadow);
            SetObject(controller, "subtitleText", subtitle);
            SetObject(controller, "subtitleBackground", subtitleBackground);
            SetObject(controller, "clockPlate", clockPlate);
            SetObject(controller, "clockLabel", clockLabel);
            SetObject(controller, "clockValueText", clockValue);
            SetObject(controller, "fadeImage", fade);
            SetObject(controller, "voiceSource", voiceSource);
            SetObject(controller, "ambienceSource", ambienceSource);
            SetObject(controller, "effectsSource", effectsSource);
            SetObject(controller, "musicSource", musicSource);
            SetObject(controller, "electricalHum", electricalHum);
            SetObject(controller, "circuitClick", circuitClick);
            SetObject(controller, "lightChime", lightChime);
            SetObject(controller, "resolvedMusic", music);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CC06 built: the last conduit closes, a restrained pulse travels through the circuit, the hall lights rise in sequence, and the moth crosses to the window.");
        }

        static void ImportSourceArt()
        {
            ConfigureSpriteTexture("Assets/Art/CC05/fundo_andar4.png", 64f, false);
            ConfigureSpriteTexture("Assets/Art/CC05/painel.png", 64f, true);
            ConfigureSpriteTexture("Assets/Art/CC05/obra_apagada.png", 64f, true);
            ConfigureSpriteTexture("Assets/Art/Characters/Mariposa_Base.png", 64f, true);
            AssetDatabase.Refresh();
        }

        static void ConfigureSpriteTexture(string path, float ppu, bool transparent)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing source scene art: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = transparent;
            importer.SaveAndReimport();
        }

        static Sprite[] LoadGuardFrames(string animation, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC01/" + animation + "_" + i.ToString("00") + ".asset");
            return frames;
        }

        static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        static AudioClip LoadAudio(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);

        static SpriteRenderer MakeWorldSprite(Transform parent, string name, Sprite sprite, Vector3 position, int sorting, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sorting;
            go.transform.SetParent(parent, true);
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
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        static Sprite CreatePixelSprite(string name, int width, int height, Func<int, int, Color> draw, float ppu)
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
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) pixels[y * width + x] = draw(x, y);
            texture.SetPixels(pixels);
            texture.Apply();
            EditorUtility.SetDirty(texture);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
                sprite.name = name;
                AssetDatabase.CreateAsset(sprite, spritePath);
            }
            return sprite;
        }

        static Color DrawSoftGlow(int x, int y)
        {
            float dx = (x + 0.5f - 64f) / 64f;
            float dy = (y + 0.5f - 64f) / 64f;
            float r = dx * dx * 0.82f + dy * dy;
            if (r > 1f) return Color.clear;
            float a = Mathf.Floor(Mathf.Clamp01((1f - r) * 1.28f) * 5f) / 5f;
            return new Color(1f, 1f, 1f, a * 0.34f);
        }

        static Color DrawFloorGlow(int x, int y)
        {
            float dx = (x + 0.5f - 80f) / 80f;
            float dy = (y + 0.5f - 24f) / 24f;
            float r = dx * dx + dy * dy;
            if (r > 1f) return Color.clear;
            float a = Mathf.Floor(Mathf.Clamp01((1f - r) * 1.15f) * 4f) / 4f;
            return new Color(1f, 1f, 1f, a * 0.38f);
        }

        static Color DrawBridge(int x, int y)
        {
            bool line = x >= 1 && x <= 4 && (y == 1 || y == 2);
            return line ? Color.white : Color.clear;
        }

        static Color DrawWireLeads(int x, int y)
        {
            bool left = x >= 0 && x <= 5;
            bool right = x >= 12 && x <= 17;
            if (!(left || right)) return Color.clear;
            if ((x == 5 || x == 12) && y >= 2 && y <= 5)
                return new Color(0.58f, 0.48f, 0.34f, 1f);
            if (y == 3 || y == 4) return new Color(0.54f, 0.63f, 0.77f, 1f);
            if ((y == 2 || y == 5) && (x == 1 || x == 2 || x == 15 || x == 16))
                return new Color(0.34f, 0.42f, 0.56f, 1f);
            return Color.clear;
        }

        static Color DrawPulse(int x, int y)
        {
            float dx = x + 0.5f - 8f;
            float dy = y + 0.5f - 8f;
            float d = dx * dx + dy * dy;
            if (d <= 4f) return new Color(1f, 1f, 1f, 0.92f);
            if (d <= 18f) return new Color(1f, 1f, 1f, 0.55f);
            if (d <= 42f) return new Color(1f, 1f, 1f, 0.18f);
            return Color.clear;
        }

        static void SetObject(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new MissingFieldException(target.GetType().Name, name);
            property.objectReferenceValue = value;
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
            foreach (EditorBuildSettingsScene scene in current)
                if (scene.path == scenePath) return;
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
