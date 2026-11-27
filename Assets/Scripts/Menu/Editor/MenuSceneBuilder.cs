using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EspacoCultural.Menu.EditorTools
{
    /// <summary>
    /// Menu "Espaço Cultural > Menu Inicial > Criar cena do menu".
    /// Monta a cena Assets/Scenes/MenuInicial.unity inteira (fundo, personagens animados, título,
    /// menu, painéis de Configurações e Créditos, fade) e coloca a cena como a primeira do build.
    /// Pode rodar de novo: recria a cena do zero.
    /// </summary>
    public static class MenuSceneBuilder
    {
        // ---------- Caminhos ----------
        private const string ScenePath = "Assets/Scenes/MenuInicial.unity";
        private const string GameScenePath = "Assets/Scenes/SampleScene.unity";

        private const string ArtMenu = "Assets/Art/Menu/";
        private const string BgPath = "Assets/Art/Cenarios/andar2/fundo_andar2.png";
        private const string VitrinePath = "Assets/Art/Cenarios/andar3/objetos/vitrine.png";
        private const string EstatuaPath = "Assets/Art/Cenarios/andar1/objetos/estatua_vaqueiro.png";
        private const string GuardaPath = "Assets/Art/Personagens/guarda/animacoes/guarda_idle_frente_sheet.png";
        private const string MoemaPath = "Assets/Art/Personagens/moema/animacoes/moema_idle_frente_sheet.png";
        private const string MariposaPath = "Assets/Art/Personagens/npcs/mariposa/animacoes/mariposa_voo_sheet_16.png";

        private const string FontTitlePath = "Assets/Fonts/PressStart2P-Regular.ttf";
        private const string FontMenuPath = "Assets/Fonts/Silkscreen-Regular.ttf";
        private const string FontMenuBoldPath = "Assets/Fonts/Silkscreen-Bold.ttf";

        // ---------- Paleta (paleta_misterio) ----------
        private static readonly Color Navy = Hex("141830");
        private static readonly Color LightBlue = Hex("8EC1DA");
        private static readonly Color Ice = Hex("ECE8DE");
        private static readonly Color Muted = Hex("AFB5CB");

        private static Font fontTitle, fontMenu, fontMenuBold;

        [UnityEditor.MenuItem("Espaço Cultural/Menu Inicial/Criar cena do menu")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ConfigureImports();
            fontTitle = AssetDatabase.LoadAssetAtPath<Font>(FontTitlePath);
            fontMenu = AssetDatabase.LoadAssetAtPath<Font>(FontMenuPath);
            fontMenuBold = AssetDatabase.LoadAssetAtPath<Font>(FontMenuBoldPath) ?? fontMenu;
            if (fontTitle == null || fontMenu == null)
            {
                Debug.LogError("[Menu] Fontes não encontradas em Assets/Fonts.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- Câmera e EventSystem ----------
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.transform.position = new Vector3(0, 0, -10);

            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            esGo.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            // ---------- Canvas 640×360 (×3 exato em 1080p) ----------
            var canvasGo = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var tela = NewRect("Tela", canvasGo.transform);
            tela.anchorMin = tela.anchorMax = new Vector2(0.5f, 0.5f);
            tela.pivot = new Vector2(0.5f, 0.5f);
            tela.sizeDelta = new Vector2(640, 360);
            tela.anchoredPosition = Vector2.zero;
            tela.gameObject.AddComponent<RectMask2D>();

            // ---------- Cenário ----------
            NewImage("Fundo_Galeria", tela, Spr(BgPath), 0, 0, 640, 360, raycast: false);
            var dark = NewImage("Escurecer", tela, Spr(ArtMenu + "pixel_branco.png"), 0, 0, 640, 360, raycast: false);
            dark.color = new Color(Navy.r, Navy.g, Navy.b, 0.42f);

            NewImage("Pegadas", tela, Spr(ArtMenu + "pegadas_trilha.png"), 330, 276, 98, 44, raycast: false);

            var cone = NewImage("Lanterna_Cone", tela, Spr(ArtMenu + "lanterna_cone.png"), 288, 262, 170, 100, raycast: false);
            cone.gameObject.AddComponent<LightFlicker>();

            // Moema espiando atrás da vitrine (máscara corta o corpo quando ela se abaixa)
            var mask = NewRect("Moema_Mascara", tela);
            Place(mask, 430, 100, 128, 192);
            mask.gameObject.AddComponent<RectMask2D>();
            var moema = NewRaw("Moema", mask, Tex(MoemaPath), 0, 48, 128, 128);
            var moemaAnim = moema.gameObject.AddComponent<UISpriteSheetAnimator>();
            moemaAnim.frames = 4; moemaAnim.fps = 4f; moemaAnim.flipX = true;
            moema.gameObject.AddComponent<PeekBehaviour>().hideDistance = 64f;
            moema.uvRect = new Rect(0.25f, 0, -0.25f, 1);   // 1º quadro já no Editor

            NewImage("Vitrine", tela, Spr(VitrinePath), 420, 190, 128, 128, raycast: false);

            var estatua = NewImage("Estatua_Derrubada", tela, Spr(EstatuaPath), 0, 0, 128, 128, raycast: false);
            estatua.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            estatua.rectTransform.anchoredPosition = new Vector2(584, -300);
            estatua.rectTransform.localEulerAngles = new Vector3(0, 0, 90);

            var guarda = NewRaw("Guarda", tela, Tex(GuardaPath), 200, 210, 128, 128);
            var gAnim = guarda.gameObject.AddComponent<UISpriteSheetAnimator>();
            gAnim.frames = 5; gAnim.fps = 6f;
            guarda.uvRect = new Rect(0, 0, 0.2f, 1);

            var mariposa = NewRaw("Mariposa", tela, Tex(MariposaPath), 0, 0, 16, 16);
            mariposa.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            mariposa.rectTransform.anchoredPosition = new Vector2(338, -98);
            var mAnim = mariposa.gameObject.AddComponent<UISpriteSheetAnimator>();
            mAnim.frames = 4; mAnim.fps = 12f;
            mariposa.uvRect = new Rect(0, 0, 0.25f, 1);
            var wander = mariposa.gameObject.AddComponent<PixelWander>();
            wander.amplitude = new Vector2(56, 16);

            // ---------- Título ----------
            var t1 = NewText("Titulo_Linha1", tela, "O MISTÉRIO DO", fontMenuBold, 16, Color.white, TextAnchor.UpperCenter, 0, 20, 640, 24);
            TitleEffects(t1);
            var t2 = NewText("Titulo_Linha2", tela, "ESPAÇO CULTURAL", fontTitle, 24, Color.white, TextAnchor.UpperCenter, 0, 44, 640, 32);
            TitleEffects(t2);

            NewText("Rodape", tela, "UNIFOR · COMPUTAÇÃO GRÁFICA · 2026", fontMenu, 8, Muted, TextAnchor.UpperRight, 0, 347, 632, 10);
            NewText("Dica_Controles", tela, "SETAS: ESCOLHER · ENTER: CONFIRMAR · ESC: VOLTAR", fontMenu, 8, Muted, TextAnchor.UpperLeft, 8, 347, 400, 10);

            // ---------- Menu principal ----------
            var mainPanel = NewPanel("Painel_Menu", tela, 22, 150, 190, 128);
            var bIniciar = NewMenuItem(mainPanel, "INICIAR", 0, 10, 190);
            var bConfig = NewMenuItem(mainPanel, "CONFIGURAÇÕES", 0, 38, 190);
            var bCreditos = NewMenuItem(mainPanel, "CRÉDITOS", 0, 66, 190);
            var bSair = NewMenuItem(mainPanel, "SAIR", 0, 94, 190);
            Chain(bIniciar, bConfig, bCreditos, bSair);

            // ---------- Painel de Configurações ----------
            var cfgPanel = NewPanel("Painel_Configuracoes", tela, 120, 96, 400, 212);
            NewText("Titulo", cfgPanel, "CONFIGURAÇÕES", fontMenuBold, 16, LightBlue, TextAnchor.UpperCenter, 0, 10, 400, 20);
            var rMusica = NewOptionRow(cfgPanel, "MÚSICA", OptionRow.Kind.Nivel, 40);
            var rEfeitos = NewOptionRow(cfgPanel, "EFEITOS", OptionRow.Kind.Nivel, 70);
            var rTela = NewOptionRow(cfgPanel, "TELA CHEIA", OptionRow.Kind.Alternar, 100);
            var rRes = NewOptionRow(cfgPanel, "RESOLUÇÃO", OptionRow.Kind.Lista, 130);
            var bVoltarCfg = NewMenuItem(cfgPanel, "VOLTAR", 0, 172, 400);
            Chain(rMusica, rEfeitos, rTela, rRes, bVoltarCfg);

            // ---------- Painel de Créditos ----------
            var credPanel = NewPanel("Painel_Creditos", tela, 100, 90, 440, 240);
            NewText("Titulo", credPanel, "CRÉDITOS", fontMenuBold, 16, LightBlue, TextAnchor.UpperCenter, 0, 10, 440, 20);
            var colA = NewText("Coluna_Esquerda", credPanel, CreditsLeft(), fontMenu, 8, Color.white, TextAnchor.UpperLeft, 22, 40, 190, 150);
            var colB = NewText("Coluna_Direita", credPanel, CreditsRight(), fontMenu, 8, Color.white, TextAnchor.UpperLeft, 232, 40, 190, 150);
            foreach (var c in new[] { colA, colB })
            {
                c.supportRichText = true;
                c.lineSpacing = 1.3f;
                c.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            var bVoltarCred = NewMenuItem(credPanel, "VOLTAR", 0, 204, 440);
            Chain(bVoltarCred);

            // ---------- Fade (por cima de tudo, inclusive das barras pretas) ----------
            var fadeRt = NewRect("Fade", canvasGo.transform);
            fadeRt.anchorMin = Vector2.zero; fadeRt.anchorMax = Vector2.one;
            fadeRt.offsetMin = fadeRt.offsetMax = Vector2.zero;
            var fade = fadeRt.gameObject.AddComponent<Image>();
            fade.color = new Color(0, 0, 0, 0);   // transparente no Editor; o controlador faz o fade de entrada
            fade.raycastTarget = false;

            // ---------- Controlador ----------
            var ctrl = canvasGo.AddComponent<MenuController>();
            ctrl.mainPanel = mainPanel.gameObject;
            ctrl.configPanel = cfgPanel.gameObject;
            ctrl.creditsPanel = credPanel.gameObject;
            ctrl.firstMain = bIniciar;
            ctrl.firstConfig = rMusica;
            ctrl.firstCredits = bVoltarCred;
            ctrl.configButton = bConfig;
            ctrl.creditsButton = bCreditos;
            ctrl.musicRow = rMusica;
            ctrl.sfxRow = rEfeitos;
            ctrl.fullscreenRow = rTela;
            ctrl.resolutionRow = rRes;
            ctrl.fadeImage = fade;
            ctrl.gameSceneName = System.IO.Path.GetFileNameWithoutExtension(GameScenePath);

            UnityEventTools.AddPersistentListener(bIniciar.onClick, ctrl.OnIniciar);
            UnityEventTools.AddPersistentListener(bConfig.onClick, ctrl.OnConfiguracoes);
            UnityEventTools.AddPersistentListener(bCreditos.onClick, ctrl.OnCreditos);
            UnityEventTools.AddPersistentListener(bSair.onClick, ctrl.OnSair);
            UnityEventTools.AddPersistentListener(bVoltarCfg.onClick, ctrl.OnVoltar);
            UnityEventTools.AddPersistentListener(bVoltarCred.onClick, ctrl.OnVoltar);

            cfgPanel.gameObject.SetActive(false);
            credPanel.gameObject.SetActive(false);

            // ---------- Salvar e colocar no build ----------
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes();
            Selection.activeGameObject = canvasGo;
            Debug.Log("[Menu] Cena MenuInicial criada e colocada como primeira do build. Dê Play para testar.");
        }

        // ================= Créditos (GDD 9.3) =================

        private static string Sec(string s) => $"<color=#8EC1DA>{s}</color>";

        private static string CreditsLeft() => string.Join("\n", new[]
        {
            Sec("DESENVOLVIMENTO E PROGRAMAÇÃO"), "Levi de Pontes", "",
            Sec("DIREÇÃO DE ARTE"), "Heitor Sette", "",
            Sec("ROTEIRO E GAME DESIGN"), "Natália Pacheco", "Savas Neto", "",
            Sec("MÚSICA E EFEITOS SONOROS"), "Levi de Pontes",
        });

        private static string CreditsRight() => string.Join("\n", new[]
        {
            Sec("FERRAMENTAS"), "Unity · Aseprite", "Fontes Press Start 2P e Silkscreen (SIL OFL)", "",
            Sec("PROJETO ACADÊMICO"), "Computação Gráfica – Universidade de Fortaleza (Unifor)", "Prof. [Nome]", "",
            Sec("AGRADECIMENTOS"), "Espaço Cultural Unifor e à Moema, por toda a bagunça",
        });

        // ================= Construção de UI =================

        private static RectTransform NewPanel(string name, Transform parent, float x, float y, float w, float h)
        {
            var img = NewImage(name, parent, Spr(ArtMenu + "painel_placa.png"), x, y, w, h, raycast: true);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            return img.rectTransform;
        }

        private static Button NewMenuItem(Transform panel, string text, float x, float y, float w)
        {
            var rt = NewRect("Item_" + Clean(text), panel);
            Place(rt, x, y, w, 24);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;

            var cursor = NewImage("Cursor", rt, Spr(ArtMenu + "cursor_triangulo.png"), 14, 7, 6, 10, raycast: false);
            var label = NewText("Texto", rt, text, fontMenu, 16, Ice, TextAnchor.MiddleLeft, 28, 2, w - 32, 20);

            var item = rt.gameObject.AddComponent<MenuItem>();
            item.label = label;
            item.cursor = cursor;
            cursor.color = LightBlue;
            cursor.enabled = false;
            return btn;
        }

        private static OptionRow NewOptionRow(Transform panel, string text, OptionRow.Kind kind, float y)
        {
            var rt = NewRect("Opcao_" + Clean(text), panel);
            Place(rt, 0, y, 400, 24);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);

            var cursor = NewImage("Cursor", rt, Spr(ArtMenu + "cursor_triangulo.png"), 14, 7, 6, 10, raycast: false);
            cursor.color = LightBlue;
            cursor.enabled = false;
            var label = NewText("Texto", rt, text, fontMenu, 16, Ice, TextAnchor.MiddleLeft, 28, 2, 160, 20);

            var row = rt.gameObject.AddComponent<OptionRow>();
            row.kind = kind;
            row.targetGraphic = hit;
            row.label = label;
            row.cursor = cursor;
            row.leftArrow = NewArrow(rt, "<", 196);
            row.rightArrow = NewArrow(rt, ">", 368);

            if (kind == OptionRow.Kind.Nivel)
            {
                var segs = new List<Image>();
                for (int i = 0; i < 10; i++)
                    segs.Add(NewImage("Seg" + i, rt, Spr(ArtMenu + "pixel_branco.png"), 214 + i * 15, 7, 12, 10, raycast: true));
                row.segments = segs.ToArray();
                row.maxLevel = 10;
            }
            else
            {
                row.valueText = NewText("Valor", rt, "", fontMenu, 16, Ice, TextAnchor.MiddleCenter, 212, 2, 154, 20);
            }
            return row;
        }

        private static Button NewArrow(Transform parent, string glyph, float x)
        {
            var rt = NewRect("Seta_" + (glyph == "<" ? "Esq" : "Dir"), parent);
            Place(rt, x, 2, 14, 20);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = hit;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            NewText("Texto", rt, glyph, fontMenu, 16, Ice, TextAnchor.MiddleCenter, 0, 0, 14, 20);
            return b;
        }

        /// <summary>Navegação vertical explícita, dando a volta no fim da lista.</summary>
        private static void Chain(params Selectable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var nav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = items[(i - 1 + items.Length) % items.Length],
                    selectOnDown = items[(i + 1) % items.Length],
                };
                items[i].navigation = nav;
            }
        }

        private static void TitleEffects(Text t)
        {
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = LightBlue;
            sh.effectDistance = new Vector2(2, -2);
            var o1 = t.gameObject.AddComponent<Outline>();
            o1.effectColor = Navy;
            o1.effectDistance = new Vector2(2, 2);
            var o2 = t.gameObject.AddComponent<Outline>();
            o2.effectColor = Navy;
            o2.effectDistance = new Vector2(2, 0);
        }

        // ================= Primitivas =================

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Posiciona em pixels a partir do canto superior esquerdo do pai.</summary>
        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static Image NewImage(string name, Transform parent, Sprite sprite, float x, float y, float w, float h, bool raycast)
        {
            var rt = NewRect(name, parent);
            Place(rt, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = raycast;
            return img;
        }

        private static RawImage NewRaw(string name, Transform parent, Texture tex, float x, float y, float w, float h)
        {
            var rt = NewRect(name, parent);
            Place(rt, x, y, w, h);
            var img = rt.gameObject.AddComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
            return img;
        }

        private static Text NewText(string name, Transform parent, string text, Font font, int size, Color color, TextAnchor align, float x, float y, float w, float h)
        {
            var rt = NewRect(name, parent);
            Place(rt, x, y, w, h);
            var t = rt.gameObject.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        // ================= Importação =================

        private static void ConfigureImports()
        {
            AssetDatabase.Refresh();
            var textures = new[]
            {
                BgPath, VitrinePath, EstatuaPath, GuardaPath, MoemaPath, MariposaPath,
                ArtMenu + "pegadas_trilha.png", ArtMenu + "lanterna_cone.png", ArtMenu + "painel_placa.png",
                ArtMenu + "cursor_triangulo.png", ArtMenu + "pixel_branco.png",
            };
            foreach (var p in textures)
            {
                var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                if (imp == null) { Debug.LogWarning("[Menu] Textura não encontrada: " + p); continue; }
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.spritePixelsPerUnit = 100;
                if (p.EndsWith("painel_placa.png")) imp.spriteBorder = new Vector4(5, 5, 5, 5);
                imp.SaveAndReimport();
            }
            foreach (var p in new[] { FontTitlePath, FontMenuPath, FontMenuBoldPath })
            {
                var fi = AssetImporter.GetAtPath(p) as TrueTypeFontImporter;
                if (fi == null) continue;
                fi.fontRenderingMode = FontRenderingMode.HintedRaster;
                fi.fontTextureCase = FontTextureCase.Dynamic;
                fi.SaveAndReimport();
            }
        }

        private static void SetBuildScenes()
        {
            var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath) list.Add(s);
            if (!list.Any(s => s.path == GameScenePath) && System.IO.File.Exists(GameScenePath))
                list.Add(new EditorBuildSettingsScene(GameScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static Sprite Spr(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        private static Texture2D Tex(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        private static string Clean(string s)
        {
            var map = new Dictionary<char, char> { { 'Ç', 'C' }, { 'Õ', 'O' }, { 'É', 'E' }, { 'Ú', 'U' }, { 'Ã', 'A' }, { ' ', '_' } };
            return new string(s.Select(ch => map.TryGetValue(ch, out var r) ? r : ch).ToArray());
        }
    }
}
