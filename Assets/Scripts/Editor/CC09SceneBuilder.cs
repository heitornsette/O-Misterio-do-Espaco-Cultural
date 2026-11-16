#if UNITY_EDITOR
using System;
using System.IO;
using Misterio.Cutscenes;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Misterio.EditorTools
{
    public static class CC09SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/CC09_A_Abertura_Precisa_Esperar.unity";
        static Font uiFont;

        [MenuItem("Misterio/Build CC09 - A abertura precisa esperar")]
        public static void Build()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Scripts/CC09");
            uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Sprite groundFloor = LoadSprite("Assets/Art/CC02/andar1.jpg");
            Sprite gallery = LoadSprite("Assets/Art/CC03/galeria_moonlit.png");
            Sprite archive = LoadSprite("Assets/Art/CC04/archive_quiet.png");
            Sprite[] statues =
            {
                LoadSprite("Assets/Art/CC02/estatua_vaqueiro.png"),
                LoadSprite("Assets/Art/CC02/estatua_pescador.png"),
                LoadSprite("Assets/Art/CC02/estatua_rendeira.png")
            };
            Sprite[] paintings =
            {
                LoadSprite("Assets/Art/CC03/obra_mare_mucuripe.png"),
                LoadSprite("Assets/Art/CC03/obra_nevoa_baturite.png"),
                LoadSprite("Assets/Art/CC03/obra_luz_sertao.png")
            };
            Sprite placementMark = LoadSprite("Assets/Art/CC02/placement_mark.asset");
            Sprite frameMatte = LoadSprite("Assets/Art/CC07/cc07_mare_mucuripe_matte.asset");
            Sprite archivePiece = LoadSprite("Assets/Art/CC04/cc04_archive_piece.asset");
            Sprite archiveObject = LoadSprite("Assets/Art/CC04/cc04_bird_object.asset");
            Sprite shadowSprite = LoadSprite("Assets/Art/CC04A/cc04a_ground_shadow.asset");
            Sprite dawnGlow = LoadSprite("Assets/Art/CC06/cc06_soft_pixel_glow.asset");
            Sprite radioSprite = LoadSprite("Assets/Art/CC08/cc08_security_radio.asset");
            Sprite radioPixel = LoadSprite("Assets/Art/CC08/cc08_radio_screen.asset");
            Sprite vignette = LoadSprite("Assets/Art/CC01/cinematic_vignette.asset");
            Sprite whitePixel = LoadSprite("Assets/Art/CC07/cc07_ui_white_pixel.asset");
            Sprite[] guardIdleBack = LoadGuardFrames("GuardIdleBack", 5);
            Sprite[] guardIdleFront = LoadGuardFrames("GuardIdleFront", 5);
            Sprite[] moemaIdleFront = LoadMoemaFrames("Moema_Idle_Frente", 4);
            Sprite[] moemaWalkBack = LoadMoemaFrames("Moema_Andar_Costas", 8);

            if (groundFloor == null || gallery == null || archive == null || placementMark == null || frameMatte == null ||
                archivePiece == null || archiveObject == null || shadowSprite == null || dawnGlow == null || radioSprite == null ||
                radioPixel == null || whitePixel == null || statues[0] == null || paintings[0] == null || paintings[1] == null || paintings[2] == null ||
                guardIdleBack[0] == null || guardIdleFront[0] == null || moemaIdleFront[0] == null || moemaWalkBack[0] == null)
                throw new InvalidOperationException("CC09 needs the existing floor, statue, painting, archive, character, radio and UI assets.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Câmera - CC09, relógio, andar e rádio");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 1.02f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.020f, 0.044f);
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = Vector3.up;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();

            GameObject groundRoot = new GameObject("Plano 01 - Térreo, estátuas fora das marcas");
            SpriteRenderer groundBackground = MakeWorldSprite(groundRoot.transform, "Fundo - hall do térreo", groundFloor,
                Vector3.zero, -100, Vector3.one);
            FitSprite(groundBackground, 3.5556f, 2f);
            SpriteRenderer groundGlow = MakeWorldSprite(groundRoot.transform, "Amanhecer suave pela entrada", dawnGlow,
                new Vector3(-1.43f, 0.30f, -0.04f), -30, new Vector3(1.10f, 0.44f, 1f));
            groundGlow.color = new Color(1f, 0.70f, 0.42f, 0.075f);
            Vector3[] statueTargets =
            {
                new Vector3(-0.43f, -0.73f, -0.02f),
                new Vector3(0.26f, -0.70f, -0.02f),
                new Vector3(0.88f, -0.67f, -0.02f)
            };
            for (int i = 0; i < statueTargets.Length; i++)
            {
                SpriteRenderer mark = MakeWorldSprite(groundRoot.transform, "Marca de piso " + (i + 1), placementMark,
                    statueTargets[i], 2, new Vector3(0.54f, 0.54f, 1f));
                mark.color = new Color(0.65f, 0.78f, 1f, 0.47f);
            }
            Transform[] displacedStatues = new Transform[3];
            Vector3[] statuePositions =
            {
                new Vector3(-1.03f, -0.70f, -0.04f),
                statueTargets[1] + new Vector3(0.20f, 0.11f, -0.025f),
                statueTargets[2] + new Vector3(0.17f, 0.09f, -0.025f)
            };
            for (int i = 0; i < displacedStatues.Length; i++)
            {
                Transform statue = MakeCharacter(groundRoot.transform, "Estátua preservada " + (i + 1), statues[i],
                    statuePositions[i], 8 + i, Vector3.one * (i == 1 ? 0.55f : 0.57f));
                displacedStatues[i] = statue;
            }
            Transform[] guards = new Transform[3];
            SpriteRenderer[] guardRenderers = new SpriteRenderer[3];
            SpriteRenderer[] guardShadows = new SpriteRenderer[3];
            Transform[] radios = new Transform[3];
            SpriteRenderer[] radioScreens = new SpriteRenderer[3];
            BuildGuardSet(groundRoot.transform, "Segurança do térreo", new Vector3(-1.28f, -0.58f, -0.08f),
                guardIdleBack[0], radioSprite, radioPixel, shadowSprite, out guards[0], out guardRenderers[0],
                out guardShadows[0], out radios[0], out radioScreens[0]);

            GameObject galleryRoot = new GameObject("Plano 02 - Galeria, uma pintura fora da parede");
            SpriteRenderer galleryBackground = MakeWorldSprite(galleryRoot.transform, "Fundo - galeria sob o luar", gallery,
                Vector3.zero, -100, Vector3.one);
            FitSprite(galleryBackground, 3.5556f, 2f);
            SpriteRenderer galleryGlow = MakeWorldSprite(galleryRoot.transform, "Amanhecer atravessa as janelas altas", dawnGlow,
                new Vector3(-1.36f, -0.02f, -0.04f), -30, new Vector3(1.15f, 0.62f, 1f));
            galleryGlow.color = new Color(1f, 0.76f, 0.54f, 0.062f);
            float[] galleryXs = { -0.55f, 0.13f, 0.81f };
            Vector3[] plaqueAnchors = new Vector3[3];
            string[] plaqueNames = { "MARÉ", "NÉVOA", "SERTÃO" };
            for (int i = 0; i < galleryXs.Length; i++)
            {
                plaqueAnchors[i] = new Vector3(galleryXs[i], -0.105f, -0.15f);
                if (i == 2) continue;
                SpriteRenderer wallPainting = MakeWorldSprite(galleryRoot.transform, "Obra intacta na parede " + (i + 1), paintings[i],
                    new Vector3(galleryXs[i], 0.132f, -0.05f), 4, Vector3.one);
                FitSprite(wallPainting, 0.435f, 0.285f);
            }
            Transform displacedPainting = new GameObject("Quadro preservado, aguardando sua parede").transform;
            displacedPainting.SetParent(galleryRoot.transform, false);
            displacedPainting.position = new Vector3(0.78f, -0.545f, -0.10f);
            displacedPainting.rotation = Quaternion.Euler(0f, 0f, -8f);
            SpriteRenderer paintingShadow = MakeLocalSprite(displacedPainting, "Sombra curta da moldura no piso", shadowSprite,
                new Vector3(0f, -0.04f, 0.07f), 5, new Vector3(0.70f, 0.35f, 1f));
            paintingShadow.color = new Color(0.008f, 0.013f, 0.026f, 0.37f);
            SpriteRenderer displacedMatte = MakeLocalSprite(displacedPainting, "Moldura de madeira", frameMatte,
                new Vector3(0f, 0.015f, -0.01f), 7, Vector3.one * 0.83f);
            SpriteRenderer displacedImage = MakeLocalSprite(displacedPainting, "Pintura - Maré do Mucuripe", paintings[2],
                new Vector3(0f, 0.015f, -0.025f), 8, Vector3.one);
            FitSprite(displacedImage, 0.345f, 0.245f);
            BuildGuardSet(galleryRoot.transform, "Segurança da galeria", new Vector3(-1.22f, -0.60f, -0.08f),
                guardIdleBack[0], radioSprite, radioPixel, shadowSprite, out guards[1], out guardRenderers[1],
                out guardShadows[1], out radios[1], out radioScreens[1]);

            GameObject archiveRoot = new GameObject("Plano 03 - Acervo, peças aguardando recolhimento");
            SpriteRenderer archiveBackground = MakeWorldSprite(archiveRoot.transform, "Fundo - reservas do acervo", archive,
                Vector3.zero, -100, Vector3.one);
            FitSprite(archiveBackground, 3.5556f, 2f);
            SpriteRenderer archiveGlow = MakeWorldSprite(archiveRoot.transform, "Luz da manhã refletida no piso", dawnGlow,
                new Vector3(-1.36f, -0.40f, -0.04f), -30, new Vector3(1.20f, 0.57f, 1f));
            archiveGlow.color = new Color(1f, 0.72f, 0.45f, 0.070f);
            Transform[] scatteredArchivePieces = new Transform[3];
            Sprite[] archivePieces = { archivePiece, archiveObject, archivePiece };
            Vector3[] archivePositions =
            {
                new Vector3(-0.28f, -0.665f, -0.13f),
                new Vector3(0.16f, -0.715f, -0.135f),
                new Vector3(0.48f, -0.63f, -0.14f)
            };
            float[] archiveRotations = { -13f, 8f, 17f };
            for (int i = 0; i < scatteredArchivePieces.Length; i++)
            {
                Transform piece = MakeCharacter(archiveRoot.transform, "Peça do acervo aguardando recolhimento " + (i + 1),
                    archivePieces[i], archivePositions[i], 8 + i, Vector3.one * (i == 1 ? 0.67f : 0.75f));
                piece.rotation = Quaternion.Euler(0f, 0f, archiveRotations[i]);
                scatteredArchivePieces[i] = piece;
            }
            Transform moema = MakeCharacter(archiveRoot.transform, "Moema - interrompe a ronda e se afasta", moemaWalkBack[0],
                new Vector3(0.90f, -0.54f, -0.16f), 11, Vector3.one * 0.48f);
            SpriteRenderer moemaRenderer = moema.GetComponent<SpriteRenderer>();
            SpriteRenderer moemaShadow = MakeWorldSprite(archiveRoot.transform, "Sombra de Moema no corredor", shadowSprite,
                new Vector3(0.91f, -0.585f, -0.12f), 10, new Vector3(0.38f, 0.22f, 1f));
            moemaShadow.color = new Color(0.012f, 0.018f, 0.030f, 0.40f);
            moemaShadow.transform.SetSiblingIndex(moema.GetSiblingIndex());
            BuildGuardSet(archiveRoot.transform, "Segurança do acervo", new Vector3(-1.16f, -0.57f, -0.08f),
                guardIdleBack[0], radioSprite, radioPixel, shadowSprite, out guards[2], out guardRenderers[2],
                out guardShadows[2], out radios[2], out radioScreens[2]);

            GameObject[] floorRoots = { groundRoot, galleryRoot, archiveRoot };
            galleryRoot.SetActive(false);
            archiveRoot.SetActive(false);

            Canvas canvas = MakeCanvas("CC09 - Relógio, narração e decisão de adiamento");
            if (vignette != null)
            {
                Image vignetteImage = MakeImage(canvas.transform, "Vinheta cinematográfica compartilhada", Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.16f));
                vignetteImage.sprite = vignette;
                vignetteImage.transform.SetAsFirstSibling();
            }
            Image morningWash = MakeImage(canvas.transform, "Luz quente de amanhecer, quase imperceptível", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(1f, 0.76f, 0.48f, 0f));
            morningWash.sprite = whitePixel;

            Image clockHud = MakeImage(canvas.transform, "Relógio global no canto superior", Vector2.one, Vector2.one,
                new Vector2(-151f, -62f), new Vector2(-12f, -12f), new Color(0.012f, 0.022f, 0.052f, 0f));
            clockHud.sprite = whitePixel;
            Outline hudOutline = clockHud.gameObject.AddComponent<Outline>();
            hudOutline.effectColor = new Color(0.42f, 0.55f, 0.76f, 0.66f);
            hudOutline.effectDistance = new Vector2(1f, -1f);
            Text clockHudLabel = MakeText(canvas.transform, "Rótulo do relógio global", Vector2.one, Vector2.one,
                new Vector2(-140f, -36f), new Vector2(-23f, -16f), 8, TextAnchor.MiddleRight, new Color(0.62f, 0.73f, 0.90f, 0f));
            clockHudLabel.text = "HORA GLOBAL";
            clockHudLabel.fontStyle = FontStyle.Bold;
            Text clockHudValue = MakeText(canvas.transform, "06:00 congelado", Vector2.one, Vector2.one,
                new Vector2(-140f, -59f), new Vector2(-23f, -34f), 17, TextAnchor.MiddleRight, new Color(0.93f, 0.96f, 1f, 0f));
            clockHudValue.text = "06:00";
            clockHudValue.fontStyle = FontStyle.Bold;

            Image subtitleBackground = MakeImage(canvas.transform, "Fundo das legendas sem cobrir as obras", new Vector2(0f, 1f),
                new Vector2(0.72f, 1f), new Vector2(12f, -78f), new Vector2(-8f, -8f), new Color(0.012f, 0.018f, 0.038f, 0f));
            subtitleBackground.sprite = whitePixel;
            Text subtitle = MakeText(canvas.transform, "Narração e resposta pelo rádio", new Vector2(0f, 1f), new Vector2(0.72f, 1f),
                new Vector2(22f, -72f), new Vector2(-16f, -14f), 12, TextAnchor.MiddleCenter, Color.white);
            subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitle.verticalOverflow = VerticalWrapMode.Overflow;
            subtitle.lineSpacing = 0.94f;
            subtitle.fontStyle = FontStyle.Bold;
            subtitle.enabled = false;

            Text[] galleryPlaqueLabels = new Text[3];
            Transform[] galleryPlaqueAnchorObjects = new Transform[3];
            for (int i = 0; i < galleryPlaqueLabels.Length; i++)
            {
                galleryPlaqueAnchorObjects[i] = new GameObject("Ponto de alinhamento da placa " + (i + 1)).transform;
                galleryPlaqueAnchorObjects[i].SetParent(galleryRoot.transform, false);
                galleryPlaqueAnchorObjects[i].position = plaqueAnchors[i];
                Text label = MakeText(canvas.transform, "Título da obra na placa " + (i + 1), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(-42f, -7f), new Vector2(42f, 7f), 6,
                    TextAnchor.MiddleCenter, new Color(0.86f, 0.91f, 1f, 1f));
                label.text = plaqueNames[i];
                label.fontStyle = FontStyle.Bold;
                label.enabled = false;
                galleryPlaqueLabels[i] = label;
            }

            Image resultBackdrop = MakeImage(canvas.transform, "Azul-noturno sobre a silhueta do andar", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0.015f, 0.030f, 0.078f, 0f));
            resultBackdrop.sprite = whitePixel;
            GameObject resultPanel = new GameObject("Painel de resultado e escolhas", typeof(RectTransform), typeof(CanvasGroup));
            resultPanel.transform.SetParent(canvas.transform, false);
            RectTransform resultRect = resultPanel.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0.5f, 0.5f);
            resultRect.anchorMax = new Vector2(0.5f, 0.5f);
            resultRect.offsetMin = new Vector2(-218f, -112f);
            resultRect.offsetMax = new Vector2(218f, 112f);
            Image resultPanelImage = resultPanel.AddComponent<Image>();
            resultPanelImage.sprite = whitePixel;
            resultPanelImage.color = new Color(0.025f, 0.047f, 0.098f, 0.78f);
            resultPanelImage.raycastTarget = false;
            Outline panelOutline = resultPanel.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.34f, 0.47f, 0.68f, 0.68f);
            panelOutline.effectDistance = new Vector2(1f, -1f);
            CanvasGroup resultCanvasGroup = resultPanel.GetComponent<CanvasGroup>();

            Text resultFloorLabel = MakeText(resultPanel.transform, "Andar que ainda precisa de revisão", new Vector2(0f, 0.78f),
                new Vector2(1f, 0.96f), new Vector2(18f, 0f), new Vector2(-18f, 0f), 10, TextAnchor.MiddleCenter,
                new Color(0.67f, 0.79f, 0.95f, 1f));
            resultFloorLabel.fontStyle = FontStyle.Bold;
            Text resultTitle = MakeText(resultPanel.transform, "A inauguração foi adiada", new Vector2(0f, 0.55f),
                new Vector2(1f, 0.82f), new Vector2(16f, 0f), new Vector2(-16f, 0f), 19, TextAnchor.MiddleCenter,
                new Color(0.96f, 0.97f, 1f, 1f));
            resultTitle.text = "A inauguração foi adiada";
            resultTitle.fontStyle = FontStyle.Bold;
            Text resultDescription = MakeText(resultPanel.transform, "Estado do acervo", new Vector2(0f, 0.32f),
                new Vector2(1f, 0.58f), new Vector2(24f, 0f), new Vector2(-24f, 0f), 10, TextAnchor.MiddleCenter,
                new Color(0.78f, 0.84f, 0.94f, 1f));
            resultDescription.text = "A exposição ainda não estava pronta.\nA noite podia ser tentada outra vez.";
            resultDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            resultDescription.lineSpacing = 1.05f;

            Button retryButton = MakeButton(resultPanel.transform, "Tentar novamente", new Vector2(-104f, -67f), "TENTAR NOVAMENTE", whitePixel, true);
            Button menuButton = MakeButton(resultPanel.transform, "Voltar ao menu", new Vector2(104f, -67f), "VOLTAR AO MENU", whitePixel, false);

            GameObject audioObject = new GameObject("CC09 - Ambiências, rádio e tema de adiamento");
            AudioSource voiceSource = MakeAudioSource(audioObject, "Narração e curadoria pelo rádio");
            AudioSource ambienceSource = MakeAudioSource(audioObject, "Ambiência própria do andar");
            AudioSource morningAirSource = MakeAudioSource(audioObject, "Ar externo quando chegam as seis");
            AudioSource effectsSource = MakeAudioSource(audioObject, "Relógio, movimento das obras e rádio");
            AudioSource musicSource = MakeAudioSource(audioObject, "Tema contido de encerramento");

            GameObject controllerObject = new GameObject("CC09 - A abertura precisa esperar (14s)");
            CC09CutsceneController controller = controllerObject.AddComponent<CC09CutsceneController>();
            SetObject(controller, "sceneCamera", camera);
            SetObjectArray(controller, "floorRoots", floorRoots);
            SetVector3Array(controller, "floorFocusPositions", new[]
            {
                new Vector3(-0.10f, 0f, -10f),
                new Vector3(0.10f, 0f, -10f),
                new Vector3(0.08f, 0f, -10f)
            });
            SetVector3Array(controller, "radioCameraPositions", new[]
            {
                new Vector3(0f, 0f, -10f),
                new Vector3(0f, 0f, -10f),
                new Vector3(0f, 0f, -10f)
            });
            SetFloatArray(controller, "radioCameraSizes", new[] { 1.02f, 1.02f, 1.02f });
            SetObjectArray(controller, "guards", guards);
            SetObjectArray(controller, "guardRenderers", guardRenderers);
            SetObjectArray(controller, "guardShadows", guardShadows);
            SetObjectArray(controller, "radios", radios);
            SetObjectArray(controller, "radioScreens", radioScreens);
            SetObjectArray(controller, "guardIdleBack", guardIdleBack);
            SetObjectArray(controller, "guardIdleFront", guardIdleFront);
            SetObjectArray(controller, "displacedStatues", displacedStatues);
            SetObject(controller, "displacedPainting", displacedPainting);
            SetObjectArray(controller, "scatteredArchivePieces", scatteredArchivePieces);
            SetObject(controller, "moema", moema);
            SetObject(controller, "moemaRenderer", moemaRenderer);
            SetObject(controller, "moemaShadow", moemaShadow);
            SetObjectArray(controller, "moemaIdleFront", moemaIdleFront);
            SetObjectArray(controller, "moemaWalkBack", moemaWalkBack);
            SetObject(controller, "sceneCanvas", canvas);
            SetObjectArray(controller, "galleryPlaqueAnchors", galleryPlaqueAnchorObjects);
            SetObjectArray(controller, "galleryPlaqueLabels", galleryPlaqueLabels);
            SetObject(controller, "subtitleBackground", subtitleBackground);
            SetObject(controller, "subtitleText", subtitle);
            SetObject(controller, "clockHud", clockHud);
            SetObject(controller, "clockHudLabel", clockHudLabel);
            SetObject(controller, "clockHudValue", clockHudValue);
            SetObject(controller, "morningWash", morningWash);
            Image fade = MakeImage(canvas.transform, "Transições suaves entre os planos", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            fade.sprite = whitePixel;
            SetObject(controller, "fadeImage", fade);
            SetObject(controller, "resultBackdrop", resultBackdrop);
            SetObject(controller, "resultCanvasGroup", resultCanvasGroup);
            SetObject(controller, "resultFloorLabel", resultFloorLabel);
            SetObject(controller, "resultTitle", resultTitle);
            SetObject(controller, "resultDescription", resultDescription);
            SetObject(controller, "retryButton", retryButton);
            SetObject(controller, "menuButton", menuButton);
            SetObject(controller, "voiceSource", voiceSource);
            SetObject(controller, "ambienceSource", ambienceSource);
            SetObject(controller, "morningAirSource", morningAirSource);
            SetObject(controller, "effectsSource", effectsSource);
            SetObject(controller, "musicSource", musicSource);
            SetObjectArray(controller, "floorAmbiences", new UnityEngine.Object[]
            {
                LoadAudio("Assets/Audio/CC02/cc02_ambiente_noturno.wav"),
                LoadAudio("Assets/Audio/CC03/cc03_ambiente_galeria.wav"),
                LoadAudio("Assets/Audio/CC04A/cc04a_ambiencia_acervo.wav")
            });
            SetObject(controller, "morningAir", LoadAudio("Assets/Audio/CC08/cc08_ambiencia_exterior.wav"));
            SetObject(controller, "clockChime", LoadAudio("Assets/Audio/CC02/cc02_confirmacao.wav"));
            SetObject(controller, "groundFloorSettle", LoadAudio("Assets/Audio/CC02/cc02_estatua_assenta.wav"));
            SetObject(controller, "galleryWoodShift", LoadAudio("Assets/Audio/CC03/cc03_madeira_ao_fundo.wav"));
            SetObject(controller, "moemaTrot", LoadAudio("Assets/Audio/CC04A/cc04a_passos_trote.wav"));
            SetObject(controller, "radioClick", LoadAudio("Assets/Audio/CC02/cc02_confirmacao.wav"));
            SetObject(controller, "calmEndingMusic", LoadAudio("Assets/Audio/CC04A/cc04a_pausa_musical.wav"));
            SetObject(controller, "xylophoneNote", LoadAudio("Assets/Audio/CC04A/cc04a_acento_xilofone.wav"));
            SetObject(controller, "playOnAwake", true);
            UnityEventTools.AddPersistentListener(retryButton.onClick, controller.RetrySameFloor);
            UnityEventTools.AddPersistentListener(menuButton.onClick, controller.ReturnToMenu);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CC09 built with three floor-specific endings, a restrained 06:00 transition, readable radio exchange and retry/menu handoff.");
        }

        static void BuildGuardSet(Transform root, string name, Vector3 position, Sprite guardSprite, Sprite radioSprite,
            Sprite radioPixel, Sprite shadowSprite, out Transform guard, out SpriteRenderer guardRenderer,
            out SpriteRenderer guardShadow, out Transform radio, out SpriteRenderer radioScreen)
        {
            guardShadow = MakeWorldSprite(root, "Sombra - " + name, shadowSprite, position + new Vector3(0.04f, -0.025f, 0.07f), 6,
                new Vector3(0.37f, 0.14f, 1f));
            guardShadow.color = new Color(0.010f, 0.016f, 0.032f, 0.38f);
            guard = MakeCharacter(root, name, guardSprite, position, 9, Vector3.one * 0.48f);
            guardRenderer = guard.GetComponent<SpriteRenderer>();
            radio = MakeLocalCharacter(guard, "Rádio de mão", radioSprite, new Vector3(0.12f, 0.24f, -0.02f), 10,
                new Vector3(0.48f, 0.48f, 1f));
            radioScreen = MakeLocalSprite(radio, "Visor azul do rádio", radioPixel, new Vector3(0f, 0.10f, -0.01f), 11,
                new Vector3(0.15f, 0.13f, 1f));
            radioScreen.color = new Color(0.52f, 0.82f, 1f, 0f);
        }

        static void FitSprite(SpriteRenderer renderer, float width, float height)
        {
            Vector2 original = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(width / Mathf.Max(0.001f, original.x), height / Mathf.Max(0.001f, original.y), 1f);
        }

        static Sprite[] LoadGuardFrames(string animation, int count)
        {
            Sprite[] frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC01/" + animation + "_" + i.ToString("00") + ".asset");
            return frames;
        }

        static Sprite[] LoadMoemaFrames(string animation, int count)
        {
            Sprite[] frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC04A/" + animation + "_Frame_" + i.ToString("00") + ".asset");
            return frames;
        }

        static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        static AudioClip LoadAudio(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);

        static Transform MakeCharacter(Transform parent, string name, Sprite sprite, Vector3 position, int sorting, Vector3 scale)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sorting;
            go.transform.SetParent(parent, true);
            return go.transform;
        }

        static SpriteRenderer MakeWorldSprite(Transform parent, string name, Sprite sprite, Vector3 position, int sorting, Vector3 scale)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sorting;
            go.transform.SetParent(parent, true);
            return renderer;
        }

        static Transform MakeLocalCharacter(Transform parent, string name, Sprite sprite, Vector3 localPosition, int sorting, Vector3 scale)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sorting;
            return go.transform;
        }

        static SpriteRenderer MakeLocalSprite(Transform parent, string name, Sprite sprite, Vector3 localPosition, int sorting, Vector3 scale)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sorting;
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

        static Button MakeButton(Transform parent, string name, Vector2 position, string label, Sprite whitePixel, bool primary)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(182f, 34f);
            Image image = go.GetComponent<Image>();
            image.sprite = whitePixel;
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
            colors.colorMultiplier = 1f;
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

        static void SetVector3Array(UnityEngine.Object target, string name, Vector3[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new MissingFieldException(target.GetType().Name, name);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).vector3Value = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloatArray(UnityEngine.Object target, string name, float[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null) throw new MissingFieldException(target.GetType().Name, name);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).floatValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene item in current) if (item.path == scenePath) return;
            EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[current.Length + 1];
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
