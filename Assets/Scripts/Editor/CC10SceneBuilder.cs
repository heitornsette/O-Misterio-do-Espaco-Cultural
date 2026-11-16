#if UNITY_EDITOR
using System;
using System.IO;
using Misterio.Cutscenes;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Misterio.EditorTools
{
    public static class CC10SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/CC10_A_Ultima_Sala_Ainda_no_Escuro.unity";
        const string ArtPath = "Assets/Art/CC10";
        static Font uiFont;

        [MenuItem("Misterio/Build CC10 - A última sala ainda no escuro")]
        public static void Build()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(ArtPath);
            uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Sprite hallBackground = LoadSprite("Assets/Art/CC05/fundo_andar4.png");
            Sprite nightSky = LoadSprite("Assets/Art/CC05/cc05_window_sky_night.asset");
            Sprite dawnSky = LoadSprite("Assets/Art/CC05/cc05_window_sky_dawn.asset");
            Sprite conduitRoute = LoadSprite("Assets/Art/CC05/cc05_damaged_conduits.asset");
            Sprite wireSpark = LoadSprite("Assets/Art/CC05/cc05_wire_spark.asset");
            Sprite artwork = LoadSprite("Assets/Art/CC05/obra_apagada.png");
            Sprite openPanel = LoadSprite("Assets/Art/CC05/painel.png");
            Sprite closedPanelBase = CreatePixelSprite("cc10_panel_closed_base", 64, 64, DrawClosedPanelBase, 64f);
            Sprite panelDoor = CreatePixelSprite("cc10_panel_door", 56, 58, DrawPanelDoor, 64f);
            Sprite shadow = LoadSprite("Assets/Art/CC05/cc05_character_shadow.asset");
            Sprite radioSprite = LoadSprite("Assets/Art/CC08/cc08_security_radio.asset");
            Sprite radioPixel = LoadSprite("Assets/Art/CC08/cc08_radio_screen.asset");
            Sprite guardFrontZero = LoadSprite("Assets/Art/CC01/GuardIdleFront_00.asset");
            Sprite guardBackZero = LoadSprite("Assets/Art/CC01/GuardIdleBack_00.asset");
            Sprite[] guardIdleFront = LoadGuardFrames("GuardIdleFront", 5);
            Sprite[] guardIdleBack = LoadGuardFrames("GuardIdleBack", 5);

            Sprite exteriorArchitecture = LoadSprite("Assets/Art/CC08/jardim_amanhecer_arquitetura.png");
            Sprite exteriorFloor = LoadSprite("Assets/Art/CC08/jardim_amanhecer_piso.png");
            Sprite rector = LoadSprite("Assets/Art/CC08/reitor_base_frente.png");
            Sprite curatorOne = LoadSprite("Assets/Art/CC08/curadora1_base_frente.png");
            Sprite curatorTwo = LoadSprite("Assets/Art/CC08/curadora2_base_frente.png");
            Sprite[] rectorIdle = LoadRectorFrames();
            Sprite whitePixel = LoadSprite("Assets/Art/CC08/cc08_ui_white_pixel.asset");
            if (hallBackground == null || nightSky == null || dawnSky == null || conduitRoute == null || wireSpark == null ||
                artwork == null || openPanel == null || closedPanelBase == null || panelDoor == null || shadow == null ||
                radioSprite == null || radioPixel == null || guardFrontZero == null || guardBackZero == null ||
                exteriorArchitecture == null || exteriorFloor == null || rector == null || curatorOne == null || curatorTwo == null ||
                rectorIdle[0] == null || whitePixel == null || guardIdleFront[0] == null || guardIdleBack[0] == null)
                throw new InvalidOperationException("CC10 needs the validated CC05 room and CC08 entrance, radio and cast assets.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Câmera - relógio, painel, obra e entrada");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 1.72f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.024f, 0.061f, 1f);
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = Vector3.up;
            camera.transform.position = new Vector3(0.54f, 0.30f, -10f);
            cameraObject.AddComponent<AudioListener>();

            Transform hallRoot = new GameObject("Plano A - Salão Principal ainda sem energia").transform;
            MakeWorldSprite(hallRoot, "Salão Principal - arte original do quarto andar", hallBackground, Vector3.zero, -100, Vector3.one);
            SpriteRenderer nightSkyRenderer = MakeWorldSprite(hallRoot, "Vidraças - céu da madrugada", nightSky,
                new Vector3(0f, 0f, -0.02f), -90, Vector3.one);
            SpriteRenderer dawnSkyRenderer = MakeWorldSprite(hallRoot, "Vidraças - amanhecer natural", dawnSky,
                new Vector3(0f, 0f, -0.03f), -89, Vector3.one);
            SpriteRenderer conduitRenderer = MakeWorldSprite(hallRoot, "Conduítes - última conexão ainda aberta", conduitRoute,
                new Vector3(0f, 0f, -0.04f), -20, Vector3.one);
            SpriteRenderer sculptureRenderer = MakeWorldSprite(hallRoot, "Obra principal - intacta, sem focos elétricos",
                artwork, new Vector3(0f, 0.76f, -0.13f), 7, new Vector3(0.92f, 0.92f, 1f));
            sculptureRenderer.color = new Color(0.71f, 0.77f, 0.91f, 1f);

            SpriteRenderer panelOpenRenderer = MakeWorldSprite(hallRoot, "Painel aberto - sem faíscas após as seis",
                openPanel, new Vector3(-4.20f, 0.83f, -0.17f), 8, Vector3.one);
            SpriteRenderer panelClosedRenderer = MakeWorldSprite(hallRoot, "Estrutura do painel fechado com cuidado",
                closedPanelBase, new Vector3(-4.20f, 0.83f, -0.19f), 9, Vector3.one);
            SpriteRenderer panelDoorRenderer = MakeWorldSprite(hallRoot, "Tampa fecha sem tocar nos fios",
                panelDoor, new Vector3(-4.20f, 0.83f, -0.24f), 10, Vector3.one);
            SpriteRenderer sparkRenderer = MakeWorldSprite(hallRoot, "Último brilho elétrico se apaga às seis",
                wireSpark, new Vector3(2.42f, 0.72f, -0.20f), 6, new Vector3(0.78f, 0.78f, 1f));
            SpriteRenderer guardShadow = MakeWorldSprite(hallRoot, "Sombra baixa do segurança", shadow,
                new Vector3(-3.38f, -0.67f, -0.045f), 6, new Vector3(0.72f, 0.50f, 1f));
            guardShadow.color = new Color(0.012f, 0.018f, 0.032f, 0.49f);
            Transform guard = MakeCharacter(hallRoot, "Segurança - afasta as mãos do painel e chama a curadoria",
                guardBackZero, new Vector3(-3.42f, -0.50f, -0.08f), 11, Vector3.one * 1.08f);
            SpriteRenderer guardRenderer = guard.GetComponent<SpriteRenderer>();
            Transform radio = MakeLocalCharacter(guard, "Rádio - mão junto ao uniforme", radioSprite,
                new Vector3(0.13f, 0.24f, -0.04f), 12, Vector3.one);
            SpriteRenderer radioScreen = MakeLocalSprite(radio, "Visor do rádio", radioPixel,
                new Vector3(0f, 0.155f, -0.02f), 13, Vector3.one);
            radioScreen.color = new Color(0.53f, 0.84f, 1f, 0.08f);

            Transform entranceRoot = new GameObject("Plano B - equipe aguarda na entrada do Espaço Cultural").transform;
            SpriteRenderer entranceArchitectureRenderer = MakeWorldSprite(entranceRoot,
                "Fachada do Espaço Cultural ao amanhecer", exteriorArchitecture, Vector3.zero, -100, Vector3.one);
            SpriteRenderer entranceFloorRenderer = MakeWorldSprite(entranceRoot, "Caminho da entrada", exteriorFloor,
                new Vector3(0f, 0f, 0.02f), -90, Vector3.one);
            Transform rectorTransform = MakePerson(entranceRoot, "Reitor - aguarda do lado de fora", rector,
                new Vector3(-0.97f, -0.82f, -0.12f), 0.37f, 12, shadow);
            Transform curatorOneTransform = MakePerson(entranceRoot, "Curadora - confirma o adiamento da sala", curatorOne,
                new Vector3(-0.53f, -0.82f, -0.12f), 0.36f, 11, shadow);
            Transform curatorTwoTransform = MakePerson(entranceRoot, "Equipe de curadoria - permanece na entrada", curatorTwo,
                new Vector3(-1.38f, -0.82f, -0.12f), 0.36f, 10, shadow);
            Transform[] entranceTeam = { rectorTransform, curatorOneTransform, curatorTwoTransform };
            SpriteRenderer[] entranceTeamRenderers =
            {
                rectorTransform.GetComponent<SpriteRenderer>(), curatorOneTransform.GetComponent<SpriteRenderer>(),
                curatorTwoTransform.GetComponent<SpriteRenderer>()
            };
            SpriteRenderer[] entranceTeamShadows =
            {
                entranceRoot.Find("Sombra - Reitor - aguarda do lado de fora").GetComponent<SpriteRenderer>(),
                entranceRoot.Find("Sombra - Curadora - confirma o adiamento da sala").GetComponent<SpriteRenderer>(),
                entranceRoot.Find("Sombra - Equipe de curadoria - permanece na entrada").GetComponent<SpriteRenderer>()
            };
            hallRoot.gameObject.SetActive(true);
            entranceRoot.gameObject.SetActive(false);

            Canvas canvas = MakeCanvas("CC10 - Relógio, diálogo e tela de resultado");
            Image clockHud = MakeImage(canvas.transform, "Relógio global parado em 06:00", Vector2.one, Vector2.one,
                new Vector2(-151f, -62f), new Vector2(-12f, -12f), new Color(0.012f, 0.022f, 0.052f, 0f));
            clockHud.sprite = whitePixel;
            Outline clockOutline = clockHud.gameObject.AddComponent<Outline>();
            clockOutline.effectColor = new Color(0.42f, 0.55f, 0.76f, 0.66f);
            clockOutline.effectDistance = new Vector2(1f, -1f);
            Text clockLabel = MakeText(canvas.transform, "Rótulo da hora global", Vector2.one, Vector2.one,
                new Vector2(-140f, -36f), new Vector2(-23f, -16f), 8, TextAnchor.MiddleRight,
                new Color(0.62f, 0.73f, 0.90f, 0f));
            clockLabel.text = "HORA GLOBAL";
            clockLabel.fontStyle = FontStyle.Bold;
            Text clockValue = MakeText(canvas.transform, "06:00 congelado", Vector2.one, Vector2.one,
                new Vector2(-140f, -59f), new Vector2(-23f, -34f), 17, TextAnchor.MiddleRight,
                new Color(0.93f, 0.96f, 1f, 0f));
            clockValue.text = "06:00";
            clockValue.fontStyle = FontStyle.Bold;

            Image subtitleBackground = MakeImage(canvas.transform, "Faixa discreta de narração e diálogo",
                new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 62f),
                new Color(0.012f, 0.018f, 0.038f, 0f));
            subtitleBackground.sprite = whitePixel;
            Text subtitle = MakeText(canvas.transform, "Legendas da equipe e do narrador", new Vector2(0.035f, 0f),
                new Vector2(0.965f, 0f), new Vector2(0f, 2f), new Vector2(0f, 59f), 12,
                TextAnchor.MiddleCenter, Color.white);
            subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitle.verticalOverflow = VerticalWrapMode.Overflow;
            subtitle.lineSpacing = 0.92f;
            subtitle.fontStyle = FontStyle.Bold;
            subtitle.enabled = false;

            Image resultBackdrop = MakeImage(canvas.transform, "Sombra azul sobre o salão em revisão",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.012f, 0.023f, 0.052f, 0f));
            resultBackdrop.sprite = whitePixel;
            resultBackdrop.transform.SetAsFirstSibling();
            GameObject resultPanel = new GameObject("Painel de encerramento e escolhas",
                typeof(RectTransform), typeof(CanvasGroup));
            resultPanel.transform.SetParent(canvas.transform, false);
            RectTransform resultRect = resultPanel.GetComponent<RectTransform>();
            resultRect.anchorMin = resultRect.anchorMax = new Vector2(0.5f, 0.5f);
            resultRect.offsetMin = new Vector2(-218f, -113f);
            resultRect.offsetMax = new Vector2(218f, 113f);
            Image panelImage = resultPanel.AddComponent<Image>();
            panelImage.sprite = whitePixel;
            panelImage.color = new Color(0.025f, 0.047f, 0.098f, 0.82f);
            panelImage.raycastTarget = false;
            Outline panelOutline = resultPanel.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.34f, 0.47f, 0.68f, 0.68f);
            panelOutline.effectDistance = new Vector2(1f, -1f);
            CanvasGroup resultGroup = resultPanel.GetComponent<CanvasGroup>();
            Text resultKicker = MakeText(resultPanel.transform, "Área adiada", new Vector2(0f, 0.78f),
                new Vector2(1f, 0.96f), new Vector2(18f, 0f), new Vector2(-18f, 0f), 10,
                TextAnchor.MiddleCenter, new Color(0.67f, 0.79f, 0.95f, 1f));
            resultKicker.text = "FINAL ALTERNATIVO — QUARTO ANDAR";
            resultKicker.fontStyle = FontStyle.Bold;
            Text resultTitle = MakeText(resultPanel.transform, "O Salão Principal ficou sem energia",
                new Vector2(0f, 0.53f), new Vector2(1f, 0.81f), new Vector2(12f, 0f), new Vector2(-12f, 0f),
                16, TextAnchor.MiddleCenter, new Color(0.96f, 0.97f, 1f, 1f));
            resultTitle.text = "O Salão Principal ficou sem energia";
            resultTitle.fontStyle = FontStyle.Bold;
            Text resultDescription = MakeText(resultPanel.transform, "Salão principal aguardará revisão elétrica",
                new Vector2(0f, 0.29f), new Vector2(1f, 0.58f), new Vector2(22f, 0f), new Vector2(-22f, 0f),
                10, TextAnchor.MiddleCenter, new Color(0.78f, 0.84f, 0.94f, 1f));
            resultDescription.text = "O restante da exposição permanece seguro.\nEsta sala aguarda revisão elétrica.";
            resultDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            resultDescription.lineSpacing = 1.05f;
            Button retryButton = MakeButton(resultPanel.transform, "Tentar novamente", new Vector2(-104f, -69f),
                "TENTAR NOVAMENTE", whitePixel, true);
            Button menuButton = MakeButton(resultPanel.transform, "Voltar ao menu", new Vector2(104f, -69f),
                "VOLTAR AO MENU", whitePixel, false);
            resultGroup.alpha = 0f;
            resultGroup.interactable = false;
            resultGroup.blocksRaycasts = false;
            retryButton.interactable = false;
            menuButton.interactable = false;

            Image fade = MakeImage(canvas.transform, "Corte e dissolução para o resultado", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            fade.sprite = whitePixel;

            GameObject audioObject = new GameObject("CC10 - Ambiências, rádio e fecho do painel");
            AudioSource voiceSource = MakeAudioSource(audioObject, "Vozes - campos de gravação CC10");
            AudioSource ambienceSource = MakeAudioSource(audioObject, "Ambiência da sala e pássaros da manhã");
            AudioSource effectsSource = MakeAudioSource(audioObject, "Relógio, rádio, tampa e xilofone");
            AudioSource musicSource = MakeAudioSource(audioObject, "Tema sereno de adiamento");

            GameObject controllerObject = new GameObject("CC10 - A última sala ainda no escuro (15s)");
            CC10CutsceneController controller = controllerObject.AddComponent<CC10CutsceneController>();
            SetObject(controller, "sceneCamera", camera);
            SetObject(controller, "hallRoot", hallRoot.gameObject);
            SetObject(controller, "entranceRoot", entranceRoot.gameObject);
            SetObject(controller, "nightSky", nightSkyRenderer);
            SetObject(controller, "dawnSky", dawnSkyRenderer);
            SetObject(controller, "conduitRoute", conduitRenderer);
            SetObject(controller, "wireSpark", sparkRenderer);
            SetObject(controller, "artwork", sculptureRenderer);
            SetObject(controller, "panelOpen", panelOpenRenderer);
            SetObject(controller, "panelClosedBase", panelClosedRenderer);
            SetObject(controller, "panelDoor", panelDoorRenderer);
            SetObject(controller, "guard", guard);
            SetObject(controller, "guardRenderer", guardRenderer);
            SetObject(controller, "guardShadow", guardShadow);
            SetObjectArray(controller, "guardIdleFront", guardIdleFront);
            SetObjectArray(controller, "guardIdleBack", guardIdleBack);
            SetObject(controller, "radio", radio);
            SetObject(controller, "radioScreen", radioScreen);
            SetObject(controller, "exteriorArchitecture", entranceArchitectureRenderer);
            SetObject(controller, "exteriorFloor", entranceFloorRenderer);
            SetObjectArray(controller, "entranceTeam", entranceTeam);
            SetObjectArray(controller, "entranceTeamRenderers", entranceTeamRenderers);
            SetObjectArray(controller, "entranceTeamShadows", entranceTeamShadows);
            SetObjectArray(controller, "rectorIdleFrames", rectorIdle);
            SetObject(controller, "clockHud", clockHud);
            SetObject(controller, "clockHudLabel", clockLabel);
            SetObject(controller, "clockHudValue", clockValue);
            SetObject(controller, "subtitleBackground", subtitleBackground);
            SetObject(controller, "subtitleText", subtitle);
            SetObject(controller, "fadeImage", fade);
            SetObject(controller, "resultBackdrop", resultBackdrop);
            SetObject(controller, "resultCanvasGroup", resultGroup);
            SetObject(controller, "resultKicker", resultKicker);
            SetObject(controller, "resultTitle", resultTitle);
            SetObject(controller, "resultDescription", resultDescription);
            SetObject(controller, "retryButton", retryButton);
            SetObject(controller, "menuButton", menuButton);
            SetObject(controller, "voiceSource", voiceSource);
            SetObject(controller, "ambienceSource", ambienceSource);
            SetObject(controller, "effectsSource", effectsSource);
            SetObject(controller, "musicSource", musicSource);
            SetObject(controller, "indoorAmbience", LoadAudio("Assets/Audio/CC05/cc05_zumbido_eletrico.wav"));
            SetObject(controller, "exteriorAmbience", LoadAudio("Assets/Audio/CC08/cc08_ambiencia_exterior.wav"));
            SetObject(controller, "clockChime", LoadAudio("Assets/Audio/CC02/cc02_confirmacao.wav"));
            SetObject(controller, "radioClick", LoadAudio("Assets/Audio/CC02/cc02_confirmacao.wav"));
            SetObject(controller, "panelLatch", LoadAudio("Assets/Audio/CC02/cc02_trava_escada.wav"));
            SetObject(controller, "gentleEndingNote", LoadAudio("Assets/Audio/CC04A/cc04a_acento_xilofone.wav"));
            SetObject(controller, "gentleMusic", LoadAudio("Assets/Audio/CC04A/cc04a_pausa_musical.wav"));
            SetObject(controller, "playOnAwake", true);
            UnityEventTools.AddPersistentListener(retryButton.onClick, controller.RetrySameAttempt);
            UnityEventTools.AddPersistentListener(menuButton.onClick, controller.ReturnToMenu);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CC10 built with the disconnected circuit, dawn-lit but unpowered artwork, radio exchange, careful panel closure and alternate ending.");
        }

        static Color DrawClosedPanelBase(int x, int y)
        {
            if (x < 5 || x > 58 || y < 4 || y > 60) return Color.clear;
            if (x < 9 || x > 54 || y < 8 || y > 56) return new Color32(25, 34, 45, 255);
            if (x == 9 || x == 54 || y == 8 || y == 56) return new Color32(117, 130, 143, 255);
            return new Color32(43, 53, 67, 255);
        }

        static Color DrawPanelDoor(int x, int y)
        {
            if (x < 3 || x > 52 || y < 3 || y > 54) return Color.clear;
            bool edge = x == 3 || x == 52 || y == 3 || y == 54;
            if (edge) return new Color32(28, 39, 52, 255);
            if (x == 5 || y == 5) return new Color32(174, 184, 189, 255);
            if (x == 50 || y == 52) return new Color32(75, 87, 101, 255);
            if ((x == 8 || x == 47) && (y == 9 || y == 48)) return new Color32(207, 211, 204, 255);
            if (x >= 43 && x <= 46 && y >= 27 && y <= 33) return new Color32(31, 42, 54, 255);
            if (x == 42 && y >= 28 && y <= 32) return new Color32(191, 196, 194, 255);
            if (x % 9 == 0 && y >= 14 && y <= 42) return new Color32(126, 138, 149, 255);
            return new Color32(148, 159, 165, 255);
        }

        static Sprite[] LoadGuardFrames(string animation, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = LoadSprite("Assets/Art/CC01/" + animation + "_" + i.ToString("00") + ".asset");
            return frames;
        }

        static Sprite[] LoadRectorFrames()
        {
            var frames = new Sprite[4];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = LoadSprite("Assets/Art/CC08/CC08_ReitorIdle_" + i.ToString("00") + ".asset");
            return frames;
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
                sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), ppu, 0,
                    SpriteMeshType.FullRect);
                sprite.name = name;
                AssetDatabase.CreateAsset(sprite, spritePath);
            }
            EditorUtility.SetDirty(sprite);
            return sprite;
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

        static Transform MakeCharacter(Transform parent, string name, Sprite sprite, Vector3 position, int sorting, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sorting;
            go.transform.SetParent(parent, true);
            return go.transform;
        }

        static Transform MakeLocalCharacter(Transform parent, string name, Sprite sprite, Vector3 localPosition, int sorting, Vector3 scale)
        {
            Transform child = MakeCharacter(parent, name, sprite, localPosition, sorting, scale);
            child.localPosition = localPosition;
            child.localScale = scale;
            return child;
        }

        static SpriteRenderer MakeLocalSprite(Transform parent, string name, Sprite sprite, Vector3 localPosition, int sorting, Vector3 scale)
        {
            Transform child = MakeLocalCharacter(parent, name, sprite, localPosition, sorting, scale);
            return child.GetComponent<SpriteRenderer>();
        }

        static Transform MakePerson(Transform root, string name, Sprite sprite, Vector3 position, float scale, int order, Sprite shadow)
        {
            SpriteRenderer shadowRenderer = MakeWorldSprite(root, "Sombra - " + name, shadow,
                position + new Vector3(0f, -0.02f, 0.035f), order - 1, new Vector3(0.36f, 0.14f, 1f));
            shadowRenderer.color = new Color(0.020f, 0.027f, 0.038f, 0.28f);
            return MakeCharacter(root, name, sprite, position, order, Vector3.one * scale);
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

        static Button MakeButton(Transform parent, string name, Vector2 position, string label, Sprite pixel, bool primary)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(182f, 34f);
            Image image = go.GetComponent<Image>();
            image.sprite = pixel;
            image.color = primary ? new Color(0.21f, 0.34f, 0.53f, 1f) : new Color(0.07f, 0.11f, 0.19f, 1f);
            image.raycastTarget = true;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = primary ? new Color(0.30f, 0.45f, 0.67f, 1f) : new Color(0.13f, 0.20f, 0.32f, 1f);
            colors.pressedColor = new Color(0.12f, 0.21f, 0.34f, 1f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(image.color.r, image.color.g, image.color.b, 0.45f);
            button.colors = colors;
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.54f, 0.67f, 0.86f, 0.62f);
            outline.effectDistance = new Vector2(1f, -1f);
            Text text = MakeText(go.transform, "Rótulo", Vector2.zero, Vector2.one,
                new Vector2(5f, 2f), new Vector2(-5f, -2f), 9, TextAnchor.MiddleCenter, Color.white);
            text.text = label;
            text.fontStyle = FontStyle.Bold;
            return button;
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

        static void SetObject(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new MissingFieldException(target.GetType().Name, name);
            property.objectReferenceValue = value;
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

        static void AddSceneToBuildSettings(string path)
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene item in current) if (item.path == path) return;
            var updated = new EditorBuildSettingsScene[current.Length + 1];
            Array.Copy(current, updated, current.Length);
            updated[current.Length] = new EditorBuildSettingsScene(path, true);
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
