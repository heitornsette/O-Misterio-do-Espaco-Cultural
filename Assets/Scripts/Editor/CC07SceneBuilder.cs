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
    public static class CC07SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/CC07_A_Trilha_de_Moema.unity";
        const string ArtPath = "Assets/Art/CC07";
        const string CC02Path = "Assets/Art/CC02";
        const string CC03Path = "Assets/Art/CC03";
        static Font uiFont;

        [MenuItem("Misterio/Build CC07 - A trilha de Moema")]
        public static void Build()
        {
            EnsureFolder(ArtPath);
            ImportSourceArt();
            uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/RobotoMono-Bold.ttf");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Sprite groundFloor = LoadSprite(CC02Path + "/andar1.jpg");
            Sprite gallery = LoadSprite(CC03Path + "/galeria_moonlit.png");
            Sprite fourthFloor = LoadSprite("Assets/Art/CC05/fundo_andar4.png");
            Sprite panel = LoadSprite("Assets/Art/CC05/painel.png");
            Sprite damagedConduits = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC05/cc05_damaged_conduits.asset");
            Sprite repairedBridge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC06/cc06_closed_circuit_bridge.asset");
            Sprite mothSprite = LoadSprite("Assets/Art/Characters/Mariposa_Base.png");
            Sprite feather = LoadSprite("Assets/Art/CC01/feather.asset");
            Sprite vaqueiro = LoadSprite(CC02Path + "/estatua_vaqueiro.png");
            Sprite pescador = LoadSprite(CC02Path + "/estatua_pescador.png");
            Sprite rendeira = LoadSprite(CC02Path + "/estatua_rendeira.png");
            Sprite[] guardIdleFront = LoadGuardFrames("GuardIdleFront", 5);
            Sprite[] guardIdleBack = LoadGuardFrames("GuardIdleBack", 5);
            Sprite[] moemaIdleFront = LoadMoemaFrames("Moema_Idle_Frente", 4);
            Sprite[] moemaWalkFront = LoadMoemaFrames("Moema_Andar_Frente", 7);
            Sprite[] moemaWalkBack = LoadMoemaFrames("Moema_Andar_Costas", 8);
            Sprite[] moemaAlertFront = LoadMoemaFrames("Moema_Alerta_Frente", 5);
            Texture2D sourcePainting = AssetDatabase.LoadAssetAtPath<Texture2D>(CC03Path + "/obra_mare_mucuripe.png");
            if (groundFloor == null || gallery == null || fourthFloor == null || panel == null || damagedConduits == null || repairedBridge == null ||
                mothSprite == null || feather == null || vaqueiro == null || pescador == null || rendeira == null ||
                sourcePainting == null || guardIdleFront[0] == null || guardIdleBack[0] == null ||
                moemaIdleFront[0] == null || moemaWalkFront[0] == null || moemaWalkBack[0] == null || moemaAlertFront[0] == null)
                throw new InvalidOperationException("CC07 requires the validated floor, gallery, sculpture, character, painting and cable assets.");

            Color[] paintingPixels = sourcePainting.GetPixels();
            int paintingWidth = sourcePainting.width;
            int paintingHeight = sourcePainting.height;
            Sprite whitePixel = CreatePixelSprite("cc07_ui_white_pixel", 1, 1, (x, y) => Color.white, 1f);
            Sprite softGlow = CreatePixelSprite("cc07_soft_light", 96, 96, DrawSoftGlow, 64f);
            Sprite shadow = CreatePixelSprite("cc07_soft_shadow", 48, 14, DrawShadow, 64f);
            Sprite doorMask = CreatePixelSprite("cc07_open_door_shadow", 20, 96, DrawDoorMask, 180f);
            Sprite doorLeaf = CreatePixelSprite("cc07_service_door_leaf", 22, 96, DrawServiceDoor, 180f, new Vector2(0.08f, 0.5f));
            Sprite pedestal = CreatePixelSprite("cc07_gallery_pedestal", 48, 24, DrawPedestal, 128f);
            Sprite paintingMatte = CreatePixelSprite("cc07_mare_mucuripe_matte", 64, 52, DrawPaintingMatte, 128f);
            Sprite paintingImage = CreatePixelSprite("cc07_mare_mucuripe_detail", 56, 40,
                (x, y) => SamplePainting(paintingPixels, paintingWidth, paintingHeight, x, y), 128f);
            Sprite cableSegment = CreatePixelSprite("cc07_frayed_cable", 26, 56, DrawCable, 64f);
            Sprite cableConnector = CreatePixelSprite("cc07_loose_connector", 14, 12, DrawConnector, 64f);
            Sprite memoryBorderSprite = CreatePixelSprite("cc07_memory_border", 128, 72, DrawMemoryBorder, 1f);
            Sprite vignette = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC01/cinematic_vignette.asset");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Câmera - acompanha a trilha pela lembrança");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 1.38f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.014f, 0.022f, 0.050f);
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = Vector3.up;
            camera.transform.position = new Vector3(-3.30f, 0.68f, -10f);
            cameraObject.AddComponent<AudioListener>();

            Transform presentRoot = MakeRoot("Plano 01 - Presente, a pena junto ao painel");
            MakeWorldSprite(presentRoot, "Quarto andar - salão depois da restauração", fourthFloor, Vector3.zero, -100, Vector3.one);
            SpriteRenderer presentConduits = MakeWorldSprite(presentRoot, "Conduítes do salão", damagedConduits,
                new Vector3(0f, 0f, -0.02f), -20, Vector3.one);
            presentConduits.color = new Color(0.76f, 0.79f, 0.88f, 0.78f);
            SpriteRenderer restoredConnection = MakeWorldSprite(presentRoot, "Emenda restaurada na conexão", repairedBridge,
                new Vector3(2.40f, 0.82f, -0.04f), -19, Vector3.one);
            restoredConnection.transform.rotation = Quaternion.Euler(0f, 0f, -42f);
            restoredConnection.color = new Color(0.65f, 0.74f, 0.84f, 0.88f);
            MakeWorldSprite(presentRoot, "Painel já fechado", panel, new Vector3(-4.20f, 0.83f, -0.07f), 5, Vector3.one);
            SpriteRenderer featherAtPanel = MakeWorldSprite(presentRoot, "Pena deixada junto ao painel", feather,
                new Vector3(-3.90f, 0.05f, -0.11f), 7, new Vector3(0.30f, 0.30f, 1f));
            featherAtPanel.transform.rotation = Quaternion.Euler(0f, 0f, 18f);
            SpriteRenderer presentMothShadow = MakeWorldSprite(presentRoot, "Sombra leve da mariposa", shadow,
                new Vector3(-3.58f, 0.55f, -0.04f), 8, new Vector3(0.38f, 0.26f, 1f));
            presentMothShadow.color = new Color(0.012f, 0.016f, 0.026f, 0.16f);
            Transform presentMoth = MakeCharacter(presentRoot, "Mariposa - desperta junto à pena", mothSprite,
                new Vector3(-4.02f, 0.84f, -0.12f), 10, Vector3.one * 0.72f);
            SpriteRenderer presentMothRenderer = presentMoth.GetComponent<SpriteRenderer>();
            SpriteRenderer presentMothGlow = MakeWorldSprite(presentRoot, "Luz ambiente, sem brilho mágico", softGlow,
                new Vector3(-3.84f, 0.84f, -0.08f), 9, new Vector3(0.66f, 0.48f, 1f));
            presentMothGlow.color = new Color(0.78f, 0.83f, 0.91f, 0.08f);
            presentMothGlow.transform.SetSiblingIndex(presentMoth.GetSiblingIndex());

            SpriteRenderer guardShadow = MakeWorldSprite(presentRoot, "Sombra curta do segurança", shadow,
                new Vector3(-2.24f, -1.30f, -0.04f), 7, new Vector3(0.72f, 0.34f, 1f));
            guardShadow.color = new Color(0.012f, 0.016f, 0.028f, 0.46f);
            Transform guard = MakeCharacter(presentRoot, "Segurança - entende o que aconteceu", guardIdleBack[0],
                new Vector3(-2.24f, -1.18f, -0.08f), 9, Vector3.one * 1.05f);
            SpriteRenderer guardRenderer = guard.GetComponent<SpriteRenderer>();

            Transform returningMoema = MakeCharacter(presentRoot, "Moema - sai assustada pela basculante", moemaWalkBack[0],
                new Vector3(1.58f, 0.76f, -0.13f), 10, Vector3.one * 0.44f);
            SpriteRenderer returningMoemaRenderer = returningMoema.GetComponent<SpriteRenderer>();
            returningMoemaRenderer.color = new Color(1f, 1f, 1f, 0f);

            Transform serviceRoot = MakeRoot("Plano 02 - Lembrança, a porta de serviço aberta");
            MakeWorldSprite(serviceRoot, "Térreo - entrada e escadaria de serviço", groundFloor, Vector3.zero, -100, Vector3.one);
            SpriteRenderer doorOcclusion = MakeWorldSprite(serviceRoot, "Vão escuro da porta aberta", doorMask,
                new Vector3(-1.55f, 0.29f, -0.04f), 2, Vector3.one);
            doorOcclusion.color = new Color(0.014f, 0.020f, 0.039f, 0.97f);
            Transform doorLeafTransform = MakeCharacter(serviceRoot, "Folha da porta - já entreaberta", doorLeaf,
                new Vector3(-1.62f, 0.29f, -0.06f), 3, Vector3.one);
            doorLeafTransform.rotation = Quaternion.Euler(0f, 0f, -24f);
            Transform serviceMoth = MakeCharacter(serviceRoot, "Mariposa - deixa o vão e segue pelo salão", mothSprite,
                new Vector3(-1.35f, 0.72f, -0.12f), 10, Vector3.one * 0.66f);
            Transform serviceMoema = MakeCharacter(serviceRoot, "Moema - segue a visitante", moemaIdleFront[0],
                new Vector3(-0.95f, -0.44f, -0.10f), 8, Vector3.one * 0.42f);
            SpriteRenderer serviceMoemaRenderer = serviceMoema.GetComponent<SpriteRenderer>();
            SpriteRenderer serviceShadow = MakeWorldSprite(serviceRoot, "Sombra de Moema no piso", shadow,
                new Vector3(-0.95f, -0.47f, -0.06f), 7, new Vector3(0.28f, 0.14f, 1f));
            serviceShadow.color = new Color(0.02f, 0.025f, 0.04f, 0.30f);
            serviceShadow.transform.SetSiblingIndex(serviceMoema.GetSiblingIndex());

            Transform statueRoot = MakeRoot("Plano 03 - Lembrança, Moema esbarra nas esculturas");
            MakeWorldSprite(statueRoot, "Térreo - mesmo salão durante a montagem", groundFloor, Vector3.zero, -100, Vector3.one);
            MakeWorldSprite(statueRoot, "Pedestal do pescador", pedestal,
                new Vector3(-0.56f, -0.56f, -0.08f), 6, new Vector3(0.78f, 0.78f, 1f));
            MakeWorldSprite(statueRoot, "Escultura do pescador", pescador,
                new Vector3(-0.56f, -0.55f, -0.10f), 7, new Vector3(0.62f, 0.62f, 1f));
            MakeWorldSprite(statueRoot, "Pedestal da escultura atingida", pedestal,
                new Vector3(0.04f, -0.53f, -0.08f), 8, new Vector3(0.94f, 0.94f, 1f));
            Transform bumpedStatue = MakeCharacter(statueRoot, "Escultura do vaqueiro - oscila após o toque", vaqueiro,
                new Vector3(0.04f, -0.52f, -0.11f), 10, new Vector3(0.76f, 0.76f, 1f));
            MakeWorldSprite(statueRoot, "Pedestal da rendeira", pedestal,
                new Vector3(0.66f, -0.42f, -0.08f), 6, new Vector3(0.72f, 0.72f, 1f));
            MakeWorldSprite(statueRoot, "Escultura da rendeira", rendeira,
                new Vector3(0.66f, -0.41f, -0.10f), 7, new Vector3(0.58f, 0.58f, 1f));
            Transform statueMoema = MakeCharacter(statueRoot, "Moema - perde o equilíbrio ao tocar a peça", moemaWalkFront[0],
                new Vector3(-0.70f, -0.62f, -0.14f), 11, Vector3.one * 0.43f);
            SpriteRenderer statueMoemaRenderer = statueMoema.GetComponent<SpriteRenderer>();
            SpriteRenderer statueMoemaShadow = MakeWorldSprite(statueRoot, "Sombra de Moema entre os pedestais", shadow,
                new Vector3(-0.70f, -0.65f, -0.06f), 9, new Vector3(0.28f, 0.14f, 1f));
            statueMoemaShadow.color = new Color(0.02f, 0.025f, 0.04f, 0.32f);
            statueMoemaShadow.transform.SetSiblingIndex(statueMoema.GetSiblingIndex());
            Transform statueMoth = MakeCharacter(statueRoot, "Mariposa - passa entre as peças", mothSprite,
                new Vector3(-0.40f, 0.43f, -0.12f), 12, Vector3.one * 0.62f);

            Transform galleryRoot = MakeRoot("Plano 04 - Lembrança, o quadro escorrega da parede");
            MakeWorldSprite(galleryRoot, "Galeria do acervo - durante os preparativos", gallery, Vector3.zero, -100, Vector3.one);
            Transform displacedPainting = MakeCharacter(galleryRoot, "Moldura da obra Maré do Mucuripe", paintingMatte,
                new Vector3(0.12f, 0.20f, -0.12f), 5, Vector3.one);
            MakeWorldSprite(displacedPainting, "Detalhe original da obra dentro da moldura", paintingImage,
                new Vector3(0.12f, 0.20f, -0.14f), 6, Vector3.one * 0.78f);
            Transform galleryMoema = MakeCharacter(galleryRoot, "Moema - atravessa a galeria atrás da mariposa", moemaWalkFront[0],
                new Vector3(-0.86f, -0.62f, -0.16f), 8, Vector3.one * 0.43f);
            SpriteRenderer galleryMoemaRenderer = galleryMoema.GetComponent<SpriteRenderer>();
            SpriteRenderer galleryShadow = MakeWorldSprite(galleryRoot, "Sombra de Moema sob o quadro", shadow,
                new Vector3(-0.86f, -0.65f, -0.06f), 7, new Vector3(0.29f, 0.14f, 1f));
            galleryShadow.color = new Color(0.02f, 0.025f, 0.04f, 0.30f);
            galleryShadow.transform.SetSiblingIndex(galleryMoema.GetSiblingIndex());
            Transform galleryMoth = MakeCharacter(galleryRoot, "Mariposa - cruza a galeria", mothSprite,
                new Vector3(-0.36f, 0.63f, -0.12f), 9, Vector3.one * 0.62f);

            Transform cableRoot = MakeRoot("Plano 05 - Lembrança, a conexão se solta no escuro");
            MakeWorldSprite(cableRoot, "Salão do quarto andar - instante do incidente", fourthFloor, Vector3.zero, -100, Vector3.one);
            SpriteRenderer cableConduits = MakeWorldSprite(cableRoot, "Conduítes danificados do salão", damagedConduits,
                new Vector3(0f, 0f, -0.02f), -20, Vector3.one);
            cableConduits.color = new Color(0.88f, 0.89f, 0.94f, 0.92f);
            MakeWorldSprite(cableRoot, "Painel elétrico ao longe", panel, new Vector3(-4.20f, 0.83f, -0.07f), 2, Vector3.one);
            SpriteRenderer roomLight = MakeWorldSprite(cableRoot, "Luz mínima antes da queda", softGlow,
                new Vector3(2.40f, 0.76f, -0.13f), 3, new Vector3(1.75f, 1.12f, 1f));
            roomLight.color = new Color(0.66f, 0.75f, 0.96f, 0.12f);
            SpriteRenderer cableLight = MakeWorldSprite(cableRoot, "Reflexo frio sobre o cabo", softGlow,
                new Vector3(2.42f, 0.78f, -0.15f), 4, new Vector3(0.62f, 0.44f, 1f));
            cableLight.color = new Color(0.66f, 0.80f, 1f, 0.14f);
            SpriteRenderer looseCable = MakeWorldSprite(cableRoot, "Condutor desfiado que Moema alcança", cableSegment,
                new Vector3(2.35f, 0.40f, -0.19f), 7, Vector3.one);
            Transform connector = MakeCharacter(cableRoot, "Conector frouxo - se desprende sem arco violento", cableConnector,
                new Vector3(2.48f, -0.02f, -0.21f), 8, Vector3.one);
            Transform cableMoema = MakeCharacter(cableRoot, "Moema - tenta alcançar a mariposa junto ao fio", moemaWalkFront[0],
                new Vector3(2.01f, -0.55f, -0.16f), 10, Vector3.one * 0.43f);
            SpriteRenderer cableMoemaRenderer = cableMoema.GetComponent<SpriteRenderer>();
            SpriteRenderer cableShadow = MakeWorldSprite(cableRoot, "Sombra curta de Moema no piso", shadow,
                new Vector3(2.01f, -0.59f, -0.07f), 9, new Vector3(0.32f, 0.15f, 1f));
            cableShadow.color = new Color(0.015f, 0.020f, 0.03f, 0.34f);
            cableShadow.transform.SetSiblingIndex(cableMoema.GetSiblingIndex());
            Transform cableMoth = MakeCharacter(cableRoot, "Mariposa - paira perto do conduíte", mothSprite,
                new Vector3(2.02f, 0.64f, -0.18f), 11, Vector3.one * 0.64f);

            GameObject[] sceneRoots = { presentRoot.gameObject, serviceRoot.gameObject, statueRoot.gameObject, galleryRoot.gameObject, cableRoot.gameObject };
            for (int i = 1; i < sceneRoots.Length; i++) sceneRoots[i].gameObject.SetActive(false);

            Canvas canvas = MakeCanvas("CC07 - Lembrança, legendas e passagem ao presente");
            if (vignette != null)
            {
                Image vignetteImage = MakeImage(canvas.transform, "Vinheta compartilhada e discreta", Vector2.zero, Vector2.one,
                    Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.18f));
                vignetteImage.sprite = vignette;
                vignetteImage.raycastTarget = false;
                vignetteImage.transform.SetAsFirstSibling();
            }
            Image memoryTint = MakeImage(canvas.transform, "Tom âmbar das lembranças", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Color.clear);
            memoryTint.sprite = whitePixel;
            Image memoryBorder = MakeImage(canvas.transform, "Filete âmbar nas transições de memória", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(1f, 0.70f, 0.38f, 0f));
            memoryBorder.sprite = memoryBorderSprite;
            Text memoryLabel = MakeText(canvas.transform, "Identificador sutil da lembrança", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-76f, -34f), new Vector2(76f, -10f), 10, TextAnchor.MiddleCenter,
                new Color(1f, 0.86f, 0.67f, 1f));
            memoryLabel.text = "LEMBRANÇA";
            memoryLabel.fontStyle = FontStyle.Bold;
            memoryLabel.enabled = false;
            Image subtitleBackground = MakeImage(canvas.transform, "Faixa de legenda com recuo confortável", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 68f), new Color(0.012f, 0.018f, 0.038f, 0.84f));
            subtitleBackground.sprite = whitePixel;
            Text subtitle = MakeText(canvas.transform, "Narração e fala do segurança", new Vector2(0.045f, 0f), new Vector2(0.955f, 0f),
                new Vector2(0f, 5f), new Vector2(0f, 63f), 13, TextAnchor.MiddleCenter, Color.white);
            subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitle.verticalOverflow = VerticalWrapMode.Overflow;
            subtitle.lineSpacing = 0.95f;
            subtitle.fontStyle = FontStyle.Bold;
            subtitle.enabled = false;
            Image blackout = MakeImage(canvas.transform, "Escuro gradual após a queda de energia", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0.012f, 0.020f, 0.052f, 0f));
            blackout.sprite = whitePixel;
            Image fade = MakeImage(canvas.transform, "Cortes em preto e encerramento da lembrança", Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            fade.sprite = whitePixel;

            GameObject audioObject = new GameObject("CC07 - Ambiente e detalhes sonoros da lembrança");
            AudioSource voiceSource = MakeAudioSource(audioObject, "Narração e resposta do segurança");
            AudioSource ambienceSource = MakeAudioSource(audioObject, "Zumbido da sala antes da queda");
            AudioSource effectsSource = MakeAudioSource(audioObject, "Porta, passos, toque e cabo");
            AudioSource musicSource = MakeAudioSource(audioObject, "Frase musical baixa da lembrança");

            GameObject controllerObject = new GameObject("CC07 - A trilha de Moema (15s)");
            CC07CutsceneController controller = controllerObject.AddComponent<CC07CutsceneController>();
            SetObject(controller, "sceneCamera", camera);
            SetObjectArray(controller, "sceneRoots", sceneRoots);
            SetObject(controller, "doorLeaf", doorLeafTransform);
            SetObject(controller, "bumpedStatue", bumpedStatue);
            SetObject(controller, "displacedPainting", displacedPainting);
            SetObject(controller, "looseCable", looseCable);
            SetObject(controller, "connector", connector);
            SetObject(controller, "guard", guard);
            SetObject(controller, "guardRenderer", guardRenderer);
            SetObjectArray(controller, "guardIdleFront", guardIdleFront);
            SetObjectArray(controller, "guardIdleBack", guardIdleBack);
            SetObjectArray(controller, "moemaByScene", new Transform[] { null, serviceMoema, statueMoema, galleryMoema, cableMoema });
            SetObjectArray(controller, "moemaRenderers", new SpriteRenderer[] { null, serviceMoemaRenderer, statueMoemaRenderer, galleryMoemaRenderer, cableMoemaRenderer });
            SetObject(controller, "returningMoema", returningMoema);
            SetObject(controller, "returningMoemaRenderer", returningMoemaRenderer);
            SetObjectArray(controller, "mothByScene", new Transform[] { presentMoth, serviceMoth, statueMoth, galleryMoth, cableMoth });
            SetObjectArray(controller, "mothRenderers", new SpriteRenderer[]
                { presentMothRenderer, serviceMoth.GetComponent<SpriteRenderer>(), statueMoth.GetComponent<SpriteRenderer>(),
                  galleryMoth.GetComponent<SpriteRenderer>(), cableMoth.GetComponent<SpriteRenderer>() });
            SetObjectArray(controller, "moemaIdleFront", moemaIdleFront);
            SetObjectArray(controller, "moemaWalkFront", moemaWalkFront);
            SetObjectArray(controller, "moemaWalkBack", moemaWalkBack);
            SetObjectArray(controller, "moemaAlertFront", moemaAlertFront);
            SetObject(controller, "featherAtPanel", featherAtPanel);
            SetObject(controller, "panelGlow", cableLight);
            SetObject(controller, "memoryRoomLight", roomLight);
            SetObject(controller, "subtitleBackground", subtitleBackground);
            SetObject(controller, "subtitleText", subtitle);
            SetObject(controller, "fadeImage", fade);
            SetObject(controller, "memoryTint", memoryTint);
            SetObject(controller, "memoryBorder", memoryBorder);
            SetObject(controller, "memoryLabel", memoryLabel);
            SetObject(controller, "blackoutOverlay", blackout);
            SetObject(controller, "voiceSource", voiceSource);
            SetObject(controller, "ambienceSource", ambienceSource);
            SetObject(controller, "effectsSource", effectsSource);
            SetObject(controller, "musicSource", musicSource);
            SetObject(controller, "electricalHum", LoadAudio("Assets/Audio/CC05/cc05_zumbido_eletrico.wav"));
            SetObject(controller, "memoryMusic", LoadAudio("Assets/Audio/CC02/cc02_xilofone_escadaria.wav"));
            SetObject(controller, "doorCreak", LoadAudio("Assets/Audio/CC04/cc04_rangido_leve.wav"));
            SetObject(controller, "softSteps", LoadAudio("Assets/Audio/CC03/cc03_passos_no_piso.wav"));
            SetObject(controller, "woodTap", LoadAudio("Assets/Audio/CC02/cc02_estatua_assenta.wav"));
            SetObject(controller, "paintingSlide", LoadAudio("Assets/Audio/CC03/cc03_madeira_ao_fundo.wav"));
            SetObject(controller, "cableClick", LoadAudio("Assets/Audio/CC03/cc03_clique_magnetico.wav"));
            SetObject(controller, "electricalSnap", LoadAudio("Assets/Audio/CC05/cc05_estalo_eletrico.wav"));
            SetObject(controller, "featherRustle", LoadAudio("Assets/Audio/CC04A/cc04a_penas_rocam.wav"));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            ConfigureSourcePaintingReadable(false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CC07 built with the service-door trail, three new Drive sculptures, the gallery artwork, the fourth-floor cable incident and a reviewed present-day return.");
        }

        static void ImportSourceArt()
        {
            ConfigureSpriteTexture(CC02Path + "/andar1.jpg", 180f, false, new Vector2(0.5f, 0.5f), false);
            ConfigureSpriteTexture(CC03Path + "/galeria_moonlit.png", 470.5f, false, new Vector2(0.5f, 0.5f), false);
            ConfigureSpriteTexture(CC03Path + "/obra_mare_mucuripe.png", 3400f, false, new Vector2(0.5f, 0.5f), true);
            ConfigureSpriteTexture("Assets/Art/CC05/fundo_andar4.png", 64f, false, new Vector2(0.5f, 0.5f), true);
            ConfigureSpriteTexture("Assets/Art/CC05/painel.png", 64f, true, new Vector2(0.5f, 0.5f), true);
            ConfigureSpriteTexture("Assets/Art/Characters/Mariposa_Base.png", 64f, true, new Vector2(0.5f, 0.5f), false);
            ConfigureSpriteTexture(CC02Path + "/estatua_vaqueiro.png", 128f, true, new Vector2(0.5f, 0f), false);
            ConfigureSpriteTexture(CC02Path + "/estatua_pescador.png", 128f, true, new Vector2(0.5f, 0f), false);
            ConfigureSpriteTexture(CC02Path + "/estatua_rendeira.png", 128f, true, new Vector2(0.5f, 0f), false);
            ConfigureSourcePaintingReadable(true);
            AssetDatabase.Refresh();
        }

        static void ConfigureSourcePaintingReadable(bool readable)
        {
            TextureImporter importer = AssetImporter.GetAtPath(CC03Path + "/obra_mare_mucuripe.png") as TextureImporter;
            if (importer == null) return;
            if (importer.isReadable == readable) return;
            importer.isReadable = readable;
            importer.SaveAndReimport();
        }

        static void ConfigureSpriteTexture(string path, float ppu, bool transparent, Vector2 pivot, bool readable)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Missing scene sprite source: " + path);
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.spritePivot = pivot;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = transparent;
            importer.isReadable = readable;
            importer.SaveAndReimport();
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
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/CC04A/" + animation + "_Frame_" + i.ToString("00") + ".asset");
            return frames;
        }

        static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        static AudioClip LoadAudio(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);

        static Transform MakeRoot(string name) => new GameObject(name).transform;

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

        static Sprite CreatePixelSprite(string name, int width, int height, Func<int, int, Color> draw, float ppu)
            => CreatePixelSprite(name, width, height, draw, ppu, new Vector2(0.5f, 0.5f));

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
            var pixels = new Color[width * height];
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

        static Color DrawSoftGlow(int x, int y)
        {
            float dx = (x + 0.5f - 48f) / 48f;
            float dy = (y + 0.5f - 48f) / 48f;
            float radius = dx * dx * 0.88f + dy * dy;
            if (radius >= 1f) return Color.clear;
            float alpha = Mathf.Floor(Mathf.Clamp01(1f - radius) * 4f) / 4f;
            return new Color(1f, 1f, 1f, alpha * 0.34f);
        }

        static Color DrawShadow(int x, int y)
        {
            float dx = (x + 0.5f - 24f) / 24f;
            float dy = (y + 0.5f - 7f) / 7f;
            float distance = dx * dx * 0.82f + dy * dy;
            if (distance >= 1f) return Color.clear;
            float alpha = Mathf.Floor(Mathf.Clamp01(1f - distance) * 3f) / 3f * 0.62f;
            return new Color(0.012f, 0.017f, 0.030f, alpha);
        }

        static Color DrawDoorMask(int x, int y)
        {
            if (x == 0 || x == 19 || y == 0 || y == 95) return new Color(0.025f, 0.036f, 0.060f, 1f);
            float edge = Mathf.Min(x, 19 - x);
            float shade = 0.018f + 0.008f * (edge / 9f);
            return new Color(shade, shade * 1.12f, shade * 1.50f, 1f);
        }

        static Color DrawServiceDoor(int x, int y)
        {
            bool outer = x < 2 || x > 19 || y < 2 || y > 93;
            bool upperInset = x >= 5 && x <= 16 && y >= 12 && y <= 40;
            bool lowerInset = x >= 5 && x <= 16 && y >= 51 && y <= 82;
            if (outer) return new Color(0.15f, 0.065f, 0.046f, 1f);
            if (x == 3 || x == 18) return new Color(0.40f, 0.18f, 0.10f, 1f);
            if (upperInset || lowerInset)
            {
                bool bevel = x == 5 || x == 16 || y == (upperInset ? 12 : 51) || y == (upperInset ? 40 : 82);
                if (bevel) return new Color(0.56f, 0.25f, 0.12f, 1f);
                float grain = ((x + y * 3) % 11 == 0) ? 0.025f : 0f;
                return new Color(0.25f + grain, 0.105f + grain * 0.4f, 0.063f, 1f);
            }
            if (x == 15 && y >= 44 && y <= 49) return new Color(0.88f, 0.64f, 0.27f, 1f);
            if (x == 16 && y >= 45 && y <= 48) return new Color(0.60f, 0.38f, 0.16f, 1f);
            if (y == 7 || y == 88) return new Color(0.33f, 0.14f, 0.08f, 1f);
            return new Color(0.34f, 0.14f, 0.08f, 1f);
        }

        static Color DrawPedestal(int x, int y)
        {
            if (y >= 18) return new Color(0.43f, 0.34f, 0.27f, 1f);
            if (y >= 14) return new Color(0.72f, 0.61f, 0.43f, 1f);
            if (y >= 4) return new Color(0.36f, 0.31f, 0.29f, 1f);
            if (y == 3 || y == 2) return new Color(0.49f, 0.40f, 0.31f, 1f);
            return new Color(0.22f, 0.23f, 0.27f, 1f);
        }

        static Color DrawPaintingMatte(int x, int y)
        {
            if (x < 2 || x >= 62 || y < 2 || y >= 50) return new Color(0.22f, 0.12f, 0.072f, 1f);
            if (x < 5 || x >= 59 || y < 5 || y >= 47) return new Color(0.72f, 0.51f, 0.25f, 1f);
            if (x == 5 || x == 58 || y == 5 || y == 46) return new Color(0.93f, 0.75f, 0.42f, 1f);
            if ((x + y) % 17 == 0) return new Color(0.40f, 0.23f, 0.13f, 1f);
            return new Color(0.17f, 0.10f, 0.075f, 1f);
        }

        static Color SamplePainting(Color[] pixels, int width, int height, int x, int y)
        {
            const float cropBottom = 245f;
            const float cropTop = 785f;
            int sourceX = Mathf.Clamp(Mathf.RoundToInt((x + 0.5f) * width / 56f), 0, width - 1);
            int sourceY = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(cropBottom, cropTop, (y + 0.5f) / 40f)), 0, height - 1);
            return pixels[sourceY * width + sourceX];
        }

        static Color DrawCable(int x, int y)
        {
            bool horizontal = y >= 49 && y <= 52 && x >= 8 && x <= 19;
            if (horizontal)
                return y == 49 || y == 52 ? new Color(0.16f, 0.19f, 0.25f, 1f) : new Color(0.08f, 0.10f, 0.15f, 1f);
            if (x >= 12 && x <= 15 && y >= 5 && y < 50)
            {
                if (x == 12 || x == 15) return new Color(0.11f, 0.14f, 0.20f, 1f);
                if (x == 13 && y % 9 < 3) return new Color(0.58f, 0.34f, 0.23f, 1f);
                if (x == 14 && y % 9 >= 4 && y % 9 <= 6) return new Color(0.37f, 0.50f, 0.64f, 1f);
                return new Color(0.08f, 0.10f, 0.15f, 1f);
            }
            if (x >= 9 && x <= 18 && y >= 1 && y <= 6 && (x + y) % 4 != 0)
                return new Color(0.34f, 0.23f, 0.19f, 1f);
            return Color.clear;
        }

        static Color DrawConnector(int x, int y)
        {
            if (x < 2 || x > 11 || y < 2 || y > 9) return Color.clear;
            if (x == 2 || x == 11 || y == 2 || y == 9) return new Color(0.16f, 0.18f, 0.22f, 1f);
            if (x >= 5 && x <= 8 && y >= 4 && y <= 7) return new Color(0.56f, 0.58f, 0.61f, 1f);
            return new Color(0.31f, 0.35f, 0.40f, 1f);
        }

        static Color DrawMemoryBorder(int x, int y)
        {
            bool horizontal = (y <= 1 || y >= 70) && x >= 2 && x <= 125;
            bool vertical = (x <= 1 || x >= 126) && y >= 2 && y <= 69;
            bool corner = (x < 7 || x > 120) && (y < 7 || y > 64) && ((x + y) % 3 == 0);
            if (horizontal || vertical) return Color.white;
            return corner ? new Color(1f, 1f, 1f, 0.42f) : Color.clear;
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
            foreach (EditorBuildSettingsScene item in current)
                if (item.path == scenePath) return;
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
