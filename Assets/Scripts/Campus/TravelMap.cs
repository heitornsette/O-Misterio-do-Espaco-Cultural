using System;
using System.Collections;
using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EspacoCultural.Campus
{
    /// <summary>
    /// Mapa de viagem no estilo Punch Club. Em vez de atravessar o campus andando tela por tela, o guarda
    /// abre o mapa (tecla M ou indo até a borda de um local), escolhe o destino e um bonequinho percorre
    /// as ruas do mapa deixando um rastro pontilhado. Ao chegar, a tela escurece e ele aparece no local.
    ///
    /// - Modo "arte" (27/11/2026): fundo isométrico do campus feito no PixelLab a partir do mapa real da Unifor
    ///   (Resources/Campus/mapa_campus.png) e um ícone por prédio (Resources/Campus/mapa_icone_&lt;id&gt;.png),
    ///   como os prédios do mapa do Punch Club: o escolhido ganha contorno branco, "respira" e mostra um balão
    ///   com o nome; o clique vale pelo pixel do ícone. Sem esses arquivos, usa o mapa gerado por código.
    /// - As ruas são um grafo (nós em pixels do mapa); a rota é o menor caminho por Dijkstra,
    ///   então o atalho pelo jardim entre o CC e a biblioteca é usado sozinho.
    /// - Andar pelo campus não gasta o relógio (decisão de 25/11): a viagem só tem duração visual.
    /// Controles: SETAS/A-D escolhem, E/ENTER viajam (segurar acelera), ESC/M fecham; mouse também funciona.
    /// </summary>
    public class TravelMap : MonoBehaviour
    {
        public class Place
        {
            public string id, name, info, shortName;
            public int node;        // nó no grafo do mapa gerado por código
            public Vector2 label;   // centro do rótulo (px do mapa, y para baixo)
            public int artNode;     // nó no grafo do mapa do PixelLab
            public Vector2 artFeet; // base (centro de baixo) do ícone do prédio no mapa do PixelLab
        }

        // ---- Grafo das ruas (px do mapa, origem no canto superior esquerdo) ----
        private static readonly Vector2[] ProcNodes =
        {
            new Vector2(104, 174), // 0 Espaço Cultural (porta)
            new Vector2(104, 200), // 1 avenida
            new Vector2(220, 138), // 2 Centro de Convivência (porta)
            new Vector2(220, 200), // 3 avenida
            new Vector2(330, 214), // 4 Praça da Fonte
            new Vector2(330, 200), // 5 avenida
            new Vector2(450, 140), // 6 Biblioteca (porta)
            new Vector2(450, 200), // 7 avenida
            new Vector2(556, 162), // 8 Ginásio (porta)
            new Vector2(556, 200), // 9 avenida
            new Vector2(334, 152), // 10 caminho do jardim
        };

        private static readonly int[,] ProcEdges =
        {
            { 0, 1 }, { 1, 3 }, { 3, 2 }, { 3, 5 }, { 5, 4 }, { 5, 7 }, { 7, 6 }, { 7, 9 }, { 9, 8 },
            { 2, 10 }, { 10, 6 },
        };

        // ---- Grafo do mapa do PixelLab (Resources/Campus/mapa_campus.png), seguindo os caminhos internos do campus ----
        private static readonly Vector2[] ArtNodes =
        {
            new Vector2(318, 298), // 0 Espaço Cultural (perto da Av. Washington Soares)
            new Vector2(354, 192), // 1 Centro de Convivência
            new Vector2(226, 224), // 2 Praça da Fonte
            new Vector2(474, 210), // 3 Biblioteca Central
            new Vector2(318, 116), // 4 Ginásio (junto às quadras e à pista)
            new Vector2(340, 140), // 5 cruzamento norte
            new Vector2(270, 196), // 6 cruzamento oeste
            new Vector2(330, 232), // 7 cruzamento sul
            new Vector2(420, 190), // 8 cruzamento leste
            new Vector2(300, 262), // 9 acesso ao Espaço Cultural
        };

        private static readonly int[,] ArtEdges =
        {
            { 4, 5 }, { 5, 1 }, { 5, 8 }, { 1, 6 }, { 6, 2 }, { 6, 7 }, { 1, 7 }, { 7, 9 }, { 9, 0 }, { 7, 8 }, { 8, 3 },
        };

        public static readonly Place[] Places =
        {
            new Place { id = "ec", name = "Espaço Cultural",       info = "Porta de serviço do museu.",            node = 0, label = new Vector2(104, 92), artNode = 0, artFeet = new Vector2(318, 296) },
            new Place { id = "cc", name = "Centro de Convivência", info = "A zeladora e a máquina de café.",       node = 2, label = new Vector2(220, 50), artNode = 1, artFeet = new Vector2(354, 188) },
            new Place { id = "pr", name = "Praça da Fonte",        info = "A fonte com os jatos d'água.",          node = 4, label = new Vector2(330, 290), artNode = 2, artFeet = new Vector2(226, 220) },
            new Place { id = "bi", name = "Biblioteca Central",    info = "O Seu Raimundo vigia a entrada.",       node = 6, label = new Vector2(450, 64), artNode = 3, artFeet = new Vector2(474, 206) },
            new Place { id = "gi", name = "Ginásio Poliesportivo", info = "Quadro de luz e achados e perdidos.",   node = 8, label = new Vector2(556, 84), shortName = "Ginásio", artNode = 4, artFeet = new Vector2(318, 112) },
        };

        public static int IndexOf(string id)
        {
            for (int i = 0; i < Places.Length; i++) if (Places[i].id == id) return i;
            return -1;
        }

        private const int W = Px.W, H = Px.H;
        private const float WalkSpeed = 140f;     // px do mapa por segundo
        private const float MetersPerPx = 0.9f;

        public bool Open => root != null && root.gameObject.activeSelf;
        public bool Traveling { get; private set; }
        public int ClosedFrame { get; private set; } = -1;
        public RectTransform Root => root;

        private RectTransform root;
        private RawImage routeImg, guardImg, cursorImg;
        private Text infoTitle, infoBody, infoDist, controls;
        private Image progressBack, progressFill;
        private readonly List<(RectTransform rt, Text text, Image back)> labels = new List<(RectTransform, Text, Image)>();
        private Texture2D routeTex;
        private Color32[] routeBuf;
        private bool routeDirty;

        private int here = -1, sel;
        private bool inside;
        private Action<string> arrived;
        private float openedAt;
        private string status;

        private static Texture2D baseTex, guardTex, pinTex, cursorTex;

        // Grafo em uso (o do mapa gerado ou o do mapa do PixelLab) e modo "arte" (ícones dos prédios, estilo Punch Club)
        private Vector2[] Nodes = ProcNodes;
        private int[,] Edges = ProcEdges;
        private bool art;
        private RawImage[] icons;
        private Texture2D[] iconTex, iconHot;
        private int NodeOf(int place) => art ? Places[place].artNode : Places[place].node;

        // ------------------------------------------------------------
        // Montagem

        public void Build(RectTransform screen, Font titleFont, Font body)
        {
            root = Px.Rect("MapaCampus", screen);
            Px.Place(root, 0, 0, W, H);

            // Mapa do PixelLab + ícones dos prédios (Resources/Campus/mapa_icone_<id>.png). Sem eles, o mapa gerado por código.
            var custom = Resources.Load<Texture2D>("Campus/mapa_campus");
            iconTex = new Texture2D[Places.Length];
            iconHot = new Texture2D[Places.Length];
            bool allIcons = true;
            for (int i = 0; i < Places.Length; i++)
            {
                iconTex[i] = Resources.Load<Texture2D>("Campus/mapa_icone_" + Places[i].id);
                if (iconTex[i] == null) { allIcons = false; continue; }
                iconTex[i].filterMode = FilterMode.Point;
                iconHot[i] = MakeHighlight(iconTex[i]);
            }
            art = custom != null && allIcons;
            if (art)
            {
                custom.filterMode = FilterMode.Point;
                Nodes = ArtNodes;
                Edges = ArtEdges;
                Px.Raw("Mapa", root, custom, 0, 0, W, H);
            }
            else
            {
                if (baseTex == null) baseTex = GenerateMap();
                Px.Raw("Mapa", root, baseTex, 0, 0, W, H);
            }

            routeTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            routeBuf = new Color32[W * H];
            routeTex.SetPixels32(routeBuf);
            routeTex.Apply();
            routeImg = Px.Raw("Rota", root, routeTex, 0, 0, W, H);

            if (art)
            {
                // Ícones dos prédios, de trás para a frente (quem está mais embaixo no mapa fica na frente)
                icons = new RawImage[Places.Length];
                var order = new List<int>();
                for (int i = 0; i < Places.Length; i++) order.Add(i);
                order.Sort((a, b) => Places[a].artFeet.y.CompareTo(Places[b].artFeet.y));
                foreach (int i in order)
                {
                    var t = iconTex[i];
                    var f = Places[i].artFeet;
                    icons[i] = Px.Raw("Predio_" + Places[i].id, root, t, f.x - t.width * 0.5f, f.y - t.height, t.width, t.height);
                }
                routeImg.rectTransform.SetAsLastSibling();   // o pontilhado passa por cima dos prédios
            }
            else
            {
                if (pinTex == null) pinTex = MakePin();
                foreach (var p in Places)
                {
                    var n = Nodes[p.node];
                    Px.Raw("Pino_" + p.id, root, pinTex, n.x - 4, n.y - 4, 9, 9);
                }
            }

            foreach (var p in Places)
            {
                var back = Px.Box("Rotulo_" + p.id, root, new Color(0.04f, 0.07f, 0.16f, 0.85f), 0, 0, 10, 13);
                var t = Px.Label("Texto", back.rectTransform, body, 8, Color.white, 4, 2, 200, 12);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.text = art ? p.name : (p.shortName ?? p.name);
                float w = Mathf.Ceil(Mathf.Max(40f, t.preferredWidth)) + 8;
                if (art)
                {
                    // Balão com o nome acima do prédio, como no Punch Club (só aparece no selecionado)
                    var it = iconTex[IndexOf(p.id)];
                    float x = Mathf.Clamp(Mathf.Round(p.artFeet.x - w * 0.5f), 4, W - w - 4);
                    Px.Place(back.rectTransform, x, Mathf.Max(32, p.artFeet.y - it.height - 12), w, 13);
                }
                else Px.Place(back.rectTransform, Mathf.Round(p.label.x - w * 0.5f), p.label.y - 6, w, 13);
                labels.Add((back.rectTransform, t, back));
            }

            if (cursorTex == null) cursorTex = MakeCursor();
            cursorImg = Px.Raw("Cursor", root, cursorTex, 0, 0, 7, 4);

            if (guardTex == null) guardTex = MakeGuard();
            guardImg = Px.Raw("Guarda", root, guardTex, 0, 0, guardTex.width * 2, guardTex.height * 2);

            // Título
            var title = Px.Panel("Titulo", root, 8, 8, 160, 22);
            Px.Label("Texto", title, titleFont, 8, Px.Warm, 0, 7, 160, 12, TextAnchor.UpperCenter).text = "MAPA DO CAMPUS";

            // Painel de informações
            var info = Px.Panel("Info", root, 120, 298, 400, 46);
            infoTitle = Px.Label("Destino", info, body, 8, Px.Warm, 10, 6, 380, 12);
            infoBody = Px.Label("Descricao", info, body, 8, Color.white, 10, 18, 380, 12);
            infoDist = Px.Label("Distancia", info, body, 8, Px.LightBlue, 10, 30, 380, 12);
            progressBack = Px.Box("Barra", info, new Color(0.1f, 0.13f, 0.25f), 10, 32, 380, 6);
            progressFill = Px.Box("Preenchimento", progressBack.rectTransform, Px.Warm, 0, 0, 0, 6);
            progressBack.gameObject.SetActive(false);

            var cb = Px.Box("Controles", root, new Color(0.04f, 0.07f, 0.16f, 0.85f), 0, 346, 640, 14);
            controls = Px.Label("Texto", cb.rectTransform, body, 8, Px.LightBlue, 4, 2, 632, 12, TextAnchor.UpperCenter);
            controls.horizontalOverflow = HorizontalWrapMode.Overflow;

            root.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------
        // Abrir / fechar

        /// <param name="hereId">Local atual (id de Places) ou null.</param>
        /// <param name="isInside">true dentro de um prédio: escolher o próprio local = sair pela frente.</param>
        /// <param name="onArrived">Chamado ao fim da caminhada, com o id do destino (o mapa ainda está aberto).</param>
        /// <param name="dir">-1/+1 pré-seleciona o vizinho à esquerda/direita (quando o guarda saiu andando por uma borda).</param>
        public void Show(string hereId, bool isInside, Action<string> onArrived, int dir = 0)
        {
            here = IndexOf(hereId);
            inside = isInside;
            arrived = onArrived;
            if (here < 0) sel = 0;
            else if (inside && dir == 0) sel = here;
            else sel = Mathf.Clamp(here + (dir < 0 ? -1 : 1), 0, Places.Length - 1);
            if (sel == here && !inside) sel = here == 0 ? 1 : here - 1;
            status = null;
            Traveling = false;
            openedAt = Time.unscaledTime;
            progressBack.gameObject.SetActive(false);
            infoDist.gameObject.SetActive(true);
            controls.text = "SETAS escolher    E viajar    ESC voltar";
            ClearRoute();
            PlaceGuard(here >= 0 ? Nodes[NodeOf(here)] : Nodes[1], 0f);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            var fade = root.parent.Find("Fade");
            if (fade != null) root.SetSiblingIndex(fade.GetSiblingIndex());
            Refresh();
        }

        public void Hide()
        {
            if (!Open) return;
            root.gameObject.SetActive(false);
            ClosedFrame = Time.frameCount;
            Traveling = false;
            arrived = null;
        }

        // ------------------------------------------------------------
        // Entrada

        private void Update()
        {
            if (!Open) return;
            AnimateCursor();
            if (routeDirty) { routeTex.SetPixels32(routeBuf); routeTex.Apply(); routeDirty = false; }
            if (Traveling || Time.unscaledTime - openedAt < 0.15f) return;

            var k = Keyboard.current;
            if (k != null)
            {
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame ||
                    k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) Move(+1);
                else if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame ||
                         k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) Move(-1);
                else if (k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Confirm();
                else if (k.escapeKey.wasPressedThisFrame || k.mKey.wasPressedThisFrame) Hide();
            }

            var m = Mouse.current;
            if (m != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(root, m.position.ReadValue(), null, out var local))
            {
                var mp = new Vector2(local.x, -local.y);   // px do mapa, y para baixo
                int hit = HitTest(mp);
                if (hit >= 0 && hit != sel && m.delta.ReadValue().sqrMagnitude > 0.01f) { sel = hit; status = null; GameAudio.Sfx("sfx_menu_mover", 0.5f); Refresh(); }
                if (m.leftButton.wasPressedThisFrame && hit >= 0) { sel = hit; Confirm(); }
                else if (m.rightButton.wasPressedThisFrame) Hide();
            }
        }

        private int HitTest(Vector2 p)
        {
            if (art)
            {
                // De frente para trás: o prédio desenhado por cima ganha o clique.
                int best = -1; float bestY = float.MinValue;
                for (int i = 0; i < Places.Length; i++)
                {
                    var t = iconTex[i]; var f = Places[i].artFeet;
                    if (p.x >= f.x - t.width * 0.5f && p.x <= f.x + t.width * 0.5f && p.y >= f.y - t.height && p.y <= f.y &&
                        IconOpaqueAt(i, p) && f.y > bestY) { best = i; bestY = f.y; }
                }
                if (best >= 0) return best;
            }
            for (int i = 0; i < Places.Length; i++)
            {
                if (Vector2.Distance(p, Nodes[NodeOf(i)]) < 12f) return i;
                var r = labels[i].rt;
                var pos = r.anchoredPosition; var size = r.sizeDelta;
                if (p.x >= pos.x && p.x <= pos.x + size.x && p.y >= -pos.y && p.y <= -pos.y + size.y) return i;
            }
            return -1;
        }

        private void Move(int d)
        {
            sel = (sel + d + Places.Length) % Places.Length;
            status = null;
            GameAudio.Sfx("sfx_menu_mover", 0.5f);
            Refresh();
        }

        private void Confirm()
        {
            if (sel == here && !inside)
            {
                status = "Você já está aqui.";
                GameAudio.Sfx("sfx_ajuste", 0.5f);
                Refresh();
                return;
            }
            GameAudio.Sfx("sfx_menu_confirmar", 0.7f);
            StartCoroutine(Travel(sel));
        }

        // ------------------------------------------------------------
        // Desenho

        private void Refresh()
        {
            for (int i = 0; i < labels.Count; i++)
            {
                bool s = i == sel;
                if (art)
                {
                    labels[i].rt.gameObject.SetActive(s && !Traveling);
                    var t = s ? iconHot[i] : iconTex[i];
                    var f = Places[i].artFeet;
                    int pad = s ? 1 : 0;
                    icons[i].texture = t;
                    Px.Place(icons[i].rectTransform, f.x - iconTex[i].width * 0.5f - pad, f.y - iconTex[i].height - pad, t.width, t.height);
                }
                labels[i].text.color = s ? Px.Warm : (i == here ? Px.LightBlue : Color.white);
                labels[i].back.color = s ? new Color(0.12f, 0.14f, 0.3f, 0.95f) : new Color(0.04f, 0.07f, 0.16f, 0.85f);
            }

            var p = Places[sel];
            infoTitle.text = "▶ " + p.name + (sel == here ? (inside ? "  (sair pela frente)" : "  (você está aqui)") : "");
            infoBody.text = p.info;

            ClearRoute();
            if (here >= 0 && sel != here)
            {
                var path = ShortestPath(NodeOf(here), NodeOf(sel));
                float len = PathLength(path);
                DrawDots(path, 0f, len, new Color32(0x8E, 0xC1, 0xDA, 0xFF), 5f);
                infoDist.text = status ?? $"cerca de {Mathf.RoundToInt(len * MetersPerPx / 10f) * 10} m a pé  ·  o relógio não corre no campus";
            }
            else infoDist.text = status ?? (inside ? "Sair do prédio por aqui." : "Escolha outro destino.");
        }

        private void AnimateCursor()
        {
            if (art)
            {
                // O prédio escolhido "respira" 1 px para cima e para baixo; sem a setinha.
                cursorImg.enabled = false;
                if (Traveling) return;
                float lift = Mathf.Floor(Time.unscaledTime * 2.5f) % 2 == 0 ? 1 : 0;
                var f = Places[sel].artFeet; var t = iconTex[sel];
                icons[sel].rectTransform.anchoredPosition = new Vector2(Mathf.Round(f.x - t.width * 0.5f - 1), -(f.y - t.height - 1 - lift));
                return;
            }
            var n = Nodes[NodeOf(sel)];
            float bob = Mathf.Floor(Time.unscaledTime * 3f) % 2 == 0 ? 0 : 1;
            Px.Place(cursorImg.rectTransform, n.x - 3, n.y - 11 + bob, 7, 4);
            cursorImg.enabled = !Traveling;
        }

        private void PlaceGuard(Vector2 feet, float bob)
        {
            float w = guardTex.width * 2, h = guardTex.height * 2;
            Px.Place(guardImg.rectTransform, Mathf.Round(feet.x - w * 0.5f), Mathf.Round(feet.y - h + 2 - bob), w, h);
        }

        // ------------------------------------------------------------
        // Viagem

        private IEnumerator Travel(int dest)
        {
            Traveling = true;
            if (art) foreach (var l in labels) l.rt.gameObject.SetActive(false);
            var p = Places[dest];
            infoTitle.text = "Caminhando até " + p.name + "...";
            infoBody.text = "(segure E para apressar o passo)";
            infoDist.gameObject.SetActive(false);
            progressBack.gameObject.SetActive(true);
            progressFill.rectTransform.sizeDelta = new Vector2(0, 6);
            controls.text = "segure E para andar mais rápido";

            var path = here >= 0 ? ShortestPath(NodeOf(here), NodeOf(dest)) : new List<int> { NodeOf(dest) };
            float len = PathLength(path), d = 0f, stepTimer = 0f;
            var trail = new Color32(0xFF, 0xD3, 0x7A, 0xFF);
            float drawn = 0f;

            while (d < len)
            {
                var k = Keyboard.current;
                bool fast = k != null && (k.eKey.isPressed || k.enterKey.isPressed || k.spaceKey.isPressed);
                d = Mathf.Min(len, d + WalkSpeed * (fast ? 4f : 1f) * Time.unscaledDeltaTime);

                DrawDots(path, drawn, d, trail, 5f);
                drawn = d;

                float bob = Mathf.Floor(d / 7f) % 2 == 0 ? 0f : 1f;
                PlaceGuard(PointAt(path, d), bob);
                progressFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(380f * d / Mathf.Max(1f, len)), 6);

                stepTimer -= Time.unscaledDeltaTime * (fast ? 2f : 1f);
                if (stepTimer <= 0f) { stepTimer = 0.28f; GameAudio.Sfx("sfx_menu_mover", 0.18f); }
                yield return null;
            }

            PlaceGuard(Nodes[NodeOf(dest)], 0f);
            progressFill.rectTransform.sizeDelta = new Vector2(380, 6);
            yield return new WaitForSecondsRealtime(0.25f);
            var cb = arrived;
            cb?.Invoke(p.id);
        }

        /// <summary>Menor caminho no grafo das ruas (Dijkstra; o grafo é pequeno, então O(n²) basta).</summary>
        private List<int> ShortestPath(int from, int to)
        {
            int n = Nodes.Length;
            var dist = new float[n];
            var prev = new int[n];
            var done = new bool[n];
            for (int i = 0; i < n; i++) { dist[i] = float.MaxValue; prev[i] = -1; }
            dist[from] = 0;
            for (int it = 0; it < n; it++)
            {
                int u = -1;
                for (int i = 0; i < n; i++) if (!done[i] && (u < 0 || dist[i] < dist[u])) u = i;
                if (u < 0 || dist[u] == float.MaxValue) break;
                done[u] = true;
                if (u == to) break;
                for (int e = 0; e < Edges.GetLength(0); e++)
                {
                    int a = Edges[e, 0], b = Edges[e, 1], v;
                    if (a == u) v = b; else if (b == u) v = a; else continue;
                    float nd = dist[u] + Vector2.Distance(Nodes[u], Nodes[v]);
                    if (nd < dist[v]) { dist[v] = nd; prev[v] = u; }
                }
            }
            var path = new List<int>();
            for (int v = to; v >= 0; v = prev[v]) { path.Add(v); if (v == from) break; }
            path.Reverse();
            if (path.Count == 0 || path[0] != from) path = new List<int> { from, to };
            return path;
        }

        private float PathLength(List<int> path)
        {
            float l = 0;
            for (int i = 1; i < path.Count; i++) l += Vector2.Distance(Nodes[path[i - 1]], Nodes[path[i]]);
            return l;
        }

        private Vector2 PointAt(List<int> path, float d)
        {
            for (int i = 1; i < path.Count; i++)
            {
                var a = Nodes[path[i - 1]]; var b = Nodes[path[i]];
                float s = Vector2.Distance(a, b);
                if (d <= s) return Vector2.Lerp(a, b, s > 0 ? d / s : 1f);
                d -= s;
            }
            return Nodes[path[path.Count - 1]];
        }

        // ---- Camada da rota (pontilhado) ----

        private void ClearRoute()
        {
            for (int i = 0; i < routeBuf.Length; i++) routeBuf[i] = default;
            routeDirty = true;
        }

        private void DrawDots(List<int> path, float from, float to, Color32 c, float spacing)
        {
            float start = Mathf.Ceil(from / spacing) * spacing;
            var outline = new Color32(0x0B, 0x0F, 0x1C, 0xFF);
            for (float d = start; d <= to; d += spacing)
            {
                var p = PointAt(path, d);
                int x = Mathf.RoundToInt(p.x), y = Mathf.RoundToInt(p.y);
                for (int oy = -1; oy <= 2; oy++)
                    for (int ox = -1; ox <= 2; ox++)
                        RouteSet(x + ox, y + oy, outline);
                for (int oy = 0; oy <= 1; oy++)
                    for (int ox = 0; ox <= 1; ox++)
                        RouteSet(x + ox, y + oy, c);
            }
            routeDirty = true;
        }

        private void RouteSet(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            routeBuf[(H - 1 - y) * W + x] = c;
        }

        // ------------------------------------------------------------
        // Pixel art gerada por código

        private static Color32 C(int hex) => new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 255);

        private class Canvas2D
        {
            public readonly Color32[] px = new Color32[W * H];
            public readonly bool[] busy = new bool[W * H];
            public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
            public void Set(int x, int y, Color32 c, bool mark = true)
            {
                if (!In(x, y)) return;
                px[(H - 1 - y) * W + x] = c;
                if (mark) busy[(H - 1 - y) * W + x] = true;
            }
            public bool Busy(int x, int y) => !In(x, y) || busy[(H - 1 - y) * W + x];
            public void Rect(int x, int y, int w, int h, Color32 c) { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) Set(i, j, c); }
            public void Disc(float cx, float cy, float r, Color32 c)
            {
                for (int j = Mathf.FloorToInt(cy - r); j <= Mathf.CeilToInt(cy + r); j++)
                    for (int i = Mathf.FloorToInt(cx - r); i <= Mathf.CeilToInt(cx + r); i++)
                        if ((i - cx) * (i - cx) + (j - cy) * (j - cy) <= r * r) Set(i, j, c);
            }
            public void Ellipse(float cx, float cy, float rx, float ry, Func<int, int, Color32> col)
            {
                for (int j = Mathf.FloorToInt(cy - ry); j <= Mathf.CeilToInt(cy + ry); j++)
                    for (int i = Mathf.FloorToInt(cx - rx); i <= Mathf.CeilToInt(cx + rx); i++)
                    {
                        float a = (i - cx) / rx, b = (j - cy) / ry;
                        if (a * a + b * b <= 1f) Set(i, j, col(i, j));
                    }
            }
            public void Line(Vector2 a, Vector2 b, float width, Color32 c)
            {
                float len = Vector2.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(len * 2));
                for (int s = 0; s <= steps; s++) { var p = Vector2.Lerp(a, b, s / (float)steps); Disc(p.x, p.y, width * 0.5f, c); }
            }
        }

        private static Texture2D GenerateMap()
        {
            var cv = new Canvas2D();

            // Grama noturna com textura
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                    h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                    var c = C(0x203B2C);
                    if (h % 9 == 0) c = C(0x1A3225);
                    else if (h % 17 == 0) c = C(0x28473A);
                    cv.Set(x, y, c, false);
                }

            // Ruas: borda, asfalto/pedra e faixa central
            var roadEdge = C(0x3B4260); var road = C(0x6E7488); var dash = C(0xC9C2A0);
            var avA = new Vector2(-4, 200); var avB = new Vector2(W + 4, 200);
            cv.Line(avA, avB, 17, roadEdge);
            Vector2[,] conn = { { ProcNodes[0], ProcNodes[1] }, { ProcNodes[2], ProcNodes[3] }, { ProcNodes[4], ProcNodes[5] }, { ProcNodes[6], ProcNodes[7] }, { ProcNodes[8], ProcNodes[9] } };
            for (int i = 0; i < conn.GetLength(0); i++) cv.Line(conn[i, 0], conn[i, 1], 11, roadEdge);
            cv.Line(avA, avB, 14, road);
            for (int i = 0; i < conn.GetLength(0); i++) cv.Line(conn[i, 0], conn[i, 1], 8, road);
            for (int x = 0; x < W; x += 8) { cv.Set(x, 200, dash); cv.Set(x + 1, 200, dash); cv.Set(x + 2, 200, dash); cv.Set(x + 3, 200, dash); }

            // Caminho do jardim (areia)
            var sandEdge = C(0x6B5A3E); var sand = C(0xB59A6A);
            cv.Line(ProcNodes[2], ProcNodes[10], 7, sandEdge); cv.Line(ProcNodes[10], ProcNodes[6], 7, sandEdge);
            cv.Line(ProcNodes[2], ProcNodes[10], 5, sand); cv.Line(ProcNodes[10], ProcNodes[6], 5, sand);

            // Postes ao longo da avenida
            for (int x = 20; x < W; x += 48)
            {
                foreach (int y in new[] { 189, 211 })
                {
                    cv.Set(x, y, C(0xFFD37A));
                    cv.Set(x - 1, y, C(0x8A6A3A)); cv.Set(x + 1, y, C(0x8A6A3A)); cv.Set(x, y - 1, C(0x8A6A3A)); cv.Set(x, y + 1, C(0x8A6A3A));
                }
            }

            // ---- Espaço Cultural: dois blocos listrados + abóbada de vidro + torre
            Block(cv, 96, 96, 18, 26, C(0xB8C0CC), C(0x6F7787), 4, stripes: true);
            Block(cv, 48, 112, 44, 58, C(0xB8C0CC), C(0x6F7787), 8, stripes: true);
            Block(cv, 116, 112, 44, 58, C(0xB8C0CC), C(0x6F7787), 8, stripes: true);
            for (int y = 118; y < 166; y++)
                for (int x = 92; x < 116; x++)
                    cv.Set(x, y, (y % 4 == 0) ? C(0x9FC7E8) : ((x + y) % 7 == 0 ? C(0x7FA9D6) : C(0x4F7FB4)));
            cv.Rect(100, 164, 8, 6, C(0x2A2F45));
            cv.Set(103, 166, C(0xFFD37A)); cv.Set(104, 166, C(0xFFD37A));   // porta de serviço entreaberta

            // ---- Centro de Convivência: bloco branco com faixa azul e duas torres de vidro
            Block(cv, 182, 70, 76, 62, C(0xE6E9EF), C(0x8D95A8), 8, stripes: false);
            cv.Rect(182, 78, 76, 6, C(0x3F6FB0));
            foreach (float tx in new[] { 180f, 260f })
            {
                cv.Disc(tx, 101, 14, C(0x23365C));
                cv.Ellipse(tx, 101, 13, 13, (i, j) => ((i + j) % 4 == 0) ? C(0x9FC7E8) : ((i % 3 == 0) ? C(0x5D8FC0) : C(0x4F86C6)));
            }

            // ---- Praça da Fonte
            for (int y = 214; y < 280; y++)
                for (int x = 286; x < 374; x++)
                    cv.Set(x, y, (x % 6 == 0 || y % 6 == 0) ? C(0x7E786C) : C(0x9A9384));
            cv.Disc(330, 247, 18, C(0xC8C0AE));
            cv.Disc(330, 247, 15, C(0x3F7FB5));
            foreach (var (sx, sy) in new[] { (324, 242), (336, 250), (330, 238), (322, 252), (338, 243) }) cv.Set(sx, sy, C(0xE9F8FD));
            cv.Disc(330, 247, 3, C(0xC8C0AE));
            foreach (var (px, py) in new[] { (292, 220), (368, 220), (292, 274), (368, 274) }) Palm(cv, px, py);

            // ---- Biblioteca Central: fachada branca com pilares e janelas em grade
            Block(cv, 396, 82, 108, 56, C(0xDFE2E8), C(0x8D95A8), 9, stripes: false);
            for (int x = 400; x < 504; x += 8) for (int y = 84; y < 128; y++) cv.Set(x, y, C(0xB0B6C2));
            foreach (int x in new[] { 404, 420, 480, 496 }) { cv.Disc(x, 144, 2.5f, C(0xA4553A)); cv.Set(x, 141, C(0x2F5A3E)); cv.Set(x, 140, C(0x2F5A3E)); }

            // ---- Ginásio: cobertura metálica em arco com borda azul
            cv.Ellipse(556, 128, 47, 33, (i, j) => C(0x2A3048));
            cv.Ellipse(556, 128, 46, 32, (i, j) =>
                j > 155 ? C(0x3F6FB0) : ((i - 556) % 6 == 0 ? C(0x7C8597) : (j < 108 ? C(0xB4BCCB) : C(0x9AA3B3))));
            cv.Rect(548, 156, 16, 6, C(0x23365C));
            for (int x = 550; x < 563; x += 3) cv.Set(x, 158, C(0xFFD37A));

            // ---- Árvores espalhadas (sem invadir ruas e prédios)
            var rnd = new System.Random(7);
            int placed = 0;
            for (int tries = 0; tries < 4000 && placed < 170; tries++)
            {
                int x = rnd.Next(6, W - 6), y = rnd.Next(34, 294);
                int r = rnd.Next(3, 6);
                bool free = true;
                for (int j = -r - 2; j <= r + 2 && free; j++)
                    for (int i = -r - 2; i <= r + 2; i++)
                        if (cv.Busy(x + i, y + j)) { free = false; break; }
                if (!free) continue;
                Tree(cv, x, y, r);
                placed++;
            }

            // Moldura
            for (int x = 0; x < W; x++) { cv.Set(x, 0, C(0x0B0F1C)); cv.Set(x, H - 1, C(0x0B0F1C)); cv.Set(x, 1, C(0x8EC1DA)); cv.Set(x, H - 2, C(0x8EC1DA)); }
            for (int y = 0; y < H; y++) { cv.Set(0, y, C(0x0B0F1C)); cv.Set(W - 1, y, C(0x0B0F1C)); cv.Set(1, y, C(0x8EC1DA)); cv.Set(W - 2, y, C(0x8EC1DA)); }

            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "mapa_campus (gerado)" };
            t.SetPixels32(cv.px);
            t.Apply();
            return t;
        }

        /// <summary>Prédio visto de cima em 3/4: telhado, filete claro no topo e parede com janelas acesas.</summary>
        private static void Block(Canvas2D cv, int x, int y, int w, int h, Color32 roof, Color32 wall, int wallH, bool stripes)
        {
            cv.Rect(x - 1, y - 1, w + 2, h + 2, C(0x10141F));
            for (int j = y; j < y + h - wallH; j++)
                for (int i = x; i < x + w; i++)
                    cv.Set(i, j, j == y ? C(0xF2F5FA) : (stripes && (i - x) % 3 == 2 ? Darken(roof, 0.78f) : roof));
            for (int j = y + h - wallH; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                {
                    bool window = (i - x) % 6 >= 2 && (i - x) % 6 <= 3 && (j - (y + h - wallH)) % 4 >= 1 && (j - (y + h - wallH)) % 4 <= 2;
                    cv.Set(i, j, window ? ((i * 7 + j * 3) % 5 == 0 ? C(0x3A3F55) : C(0xFFD37A)) : wall);
                }
        }

        private static Color32 Darken(Color32 c, float f) => new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), 255);

        private static void Tree(Canvas2D cv, int x, int y, int r)
        {
            cv.Disc(x + 1, y + 2, r, C(0x10241B));
            cv.Disc(x, y, r, C(0x173226));
            cv.Disc(x - 1, y - 1, r - 1.5f, C(0x21412F));
            cv.Set(x - r / 2, y - r / 2, C(0x2F5A3E));
            cv.Set(x - r / 2 + 1, y - r / 2, C(0x2F5A3E));
        }

        private static void Palm(Canvas2D cv, int x, int y)
        {
            var leaf = C(0x2F5A3E); var dark = C(0x173226);
            for (int k = -5; k <= 5; k++) { cv.Set(x + k, y, Mathf.Abs(k) > 3 ? dark : leaf); cv.Set(x, y + k, Mathf.Abs(k) > 3 ? dark : leaf); }
            for (int k = -3; k <= 3; k++) { cv.Set(x + k, y + k, dark); cv.Set(x + k, y - k, dark); }
            cv.Set(x, y, C(0x6B5A3E));
        }

        private static Texture2D FromRows(string[] rows, Dictionary<char, Color32> pal, bool outline)
        {
            int h = rows.Length, w = rows[0].Length;
            int pad = outline ? 1 : 0;
            var t = new Texture2D(w + pad * 2, h + pad * 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[t.width * t.height];
            Func<int, int, bool> filled = (x, y) => x >= 0 && y >= 0 && x < w && y < h && rows[y][x] != '.';
            for (int y = -pad; y < h + pad; y++)
                for (int x = -pad; x < w + pad; x++)
                {
                    Color32 c = default;
                    if (filled(x, y)) c = pal[rows[y][x]];
                    else if (outline && (filled(x - 1, y) || filled(x + 1, y) || filled(x, y - 1) || filled(x, y + 1)))
                        c = new Color32(0x0B, 0x0F, 0x1C, 0xFF);
                    int ty = (h + pad * 2 - 1) - (y + pad);
                    px[ty * t.width + (x + pad)] = c;
                }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        /// <summary>Versão "acesa" do ícone: contorno branco de 1 px e cores um pouco mais claras (hover do Punch Club).</summary>
        private static Texture2D MakeHighlight(Texture2D src)
        {
            Color32[] px;
            try { px = src.GetPixels32(); }
            catch { Debug.LogWarning("[Mapa] Ative Read/Write no ícone " + src.name + " para o destaque."); return src; }
            int w = src.width, h = src.height, W2 = w + 2, H2 = h + 2;
            var o = new Color32[W2 * H2];
            Func<int, int, bool> solid = (x, y) => x >= 0 && y >= 0 && x < w && y < h && px[y * w + x].a > 40;
            var edge = new Color32(0xF2, 0xF5, 0xFA, 0xFF);
            for (int y = -1; y <= h; y++)
                for (int x = -1; x <= w; x++)
                {
                    Color32 c = default;
                    if (solid(x, y))
                    {
                        var s = px[y * w + x];
                        c = new Color32((byte)Mathf.Min(255, s.r * 1.18f + 10), (byte)Mathf.Min(255, s.g * 1.18f + 10), (byte)Mathf.Min(255, s.b * 1.18f + 10), s.a);
                    }
                    else if (solid(x - 1, y) || solid(x + 1, y) || solid(x, y - 1) || solid(x, y + 1)) c = edge;
                    o[(y + 1) * W2 + (x + 1)] = c;
                }
            var t = new Texture2D(W2, H2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = src.name + "_hover" };
            t.SetPixels32(o);
            t.Apply();
            return t;
        }

        /// <summary>O ponto (px do mapa) cai num pixel não transparente do ícone?</summary>
        private bool IconOpaqueAt(int i, Vector2 p)
        {
            var t = iconTex[i]; var f = Places[i].artFeet;
            int x = Mathf.FloorToInt(p.x - (f.x - t.width * 0.5f));
            int y = Mathf.FloorToInt(f.y - p.y);           // textura: y de baixo para cima
            if (x < 0 || y < 0 || x >= t.width || y >= t.height) return false;
            try { return t.GetPixel(x, y).a > 0.15f; } catch { return true; }
        }

        private static Texture2D MakeGuard() => FromRows(new[]
        {
            "..NNNNN..",
            ".NNNNNNN.",
            "..SSSSS..",
            "..SKSKS..",
            "..SSSSS..",
            "...SSS...",
            ".BBBBBBB.",
            "BBBBYBBBB",
            "SBBBBBBBS",
            ".BBBBBBB.",
            "..DD.DD..",
            "..DD.DD..",
            ".DDD.DDD.",
        }, new Dictionary<char, Color32>
        {
            { 'N', C(0x1E234D) }, { 'S', C(0xE0A878) }, { 'K', C(0x1E234D) },
            { 'B', C(0x3F6FB0) }, { 'Y', C(0xFFD37A) }, { 'D', C(0x23283A) },
        }, true);

        private static Texture2D MakePin() => FromRows(new[]
        {
            "..WWW..",
            ".WYYYW.",
            "WYYYYYW",
            "WYYOYYW",
            "WYYYYYW",
            ".WYYYW.",
            "..WWW..",
        }, new Dictionary<char, Color32> { { 'W', C(0xE9F8FD) }, { 'Y', C(0xFFD37A) }, { 'O', C(0x8A6A3A) } }, true);

        private static Texture2D MakeCursor() => FromRows(new[]
        {
            "YYYYYYY",
            ".YYYYY.",
            "..YYY..",
            "...Y...",
        }, new Dictionary<char, Color32> { { 'Y', C(0x8EC1DA) } }, false);
    }
}
