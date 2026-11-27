using System;
using System.Collections;
using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Gameplay;
using EspacoCultural.Jogo;
using EspacoCultural.Tempo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EspacoCultural.Campus
{
    /// <summary>
    /// Base das cenas do campus (praça/exteriores, CC por dentro, biblioteca, ginásio).
    /// Diferente dos andares (UI num Canvas), aqui o mundo é feito de SpriteRenderers iluminados por
    /// Light 2D (URP): luz global noturna, postes, janelas e a lanterna do guarda. A câmera segue o
    /// guarda com Pixel Perfect Camera (640×360, 1 px = 1 unidade) e há camadas com parallax.
    /// O relógio da madrugada fica PARADO no campus (só corre dentro do museu).
    /// Cada cena filha descreve o mundo e os pontos de interação (Spot).
    /// </summary>
    public abstract class CampusScene : MonoBehaviour
    {
        [Header("Fontes (preenchidas pelo construtor de cenas)")]
        public Font fontTitle;
        public Font fontBody;

        public class Spot
        {
            public string name;
            public Vector2 pos;
            public float radius = 34f;
            public Func<string> label;          // texto da dica (null = esconde)
            public Action act;
            public Func<bool> visible;
        }

        // ---- O que cada cena define ----
        protected abstract float WorldWidth { get; }
        protected abstract Rect WalkArea { get; }
        protected abstract Dictionary<string, Vector2> SpawnPoints { get; }
        protected abstract string DefaultSpawn { get; }
        protected abstract void BuildWorld();
        protected virtual string Objective() => CampusGoals.Text();
        protected virtual Color GlobalLight => new Color(0.55f, 0.62f, 0.95f);
        protected virtual float GlobalIntensity => 0.6f;
        protected virtual IEnumerable<Rect> Blockers => new Rect[0];
        protected virtual string Controls => "SETAS/WASD andar    E interagir    I itens    M mapa    ESC pausa";

        // ---- Mapa de viagem (estilo Punch Club) ----
        /// <summary>Id do local no mapa (TravelMap.Places) onde o guarda está agora.</summary>
        protected virtual string MapHere => null;
        /// <summary>true dentro de prédio: escolher o próprio local no mapa = sair pela frente.</summary>
        protected virtual bool MapInside => true;
        /// <summary>Se a chegada é nesta mesma cena, posiciona o guarda e devolve true.</summary>
        protected virtual bool ArriveInScene(string spawn) => false;
        /// <summary>Faixa onde o centro da câmera pode ficar (x mínimo, x máximo).</summary>
        protected virtual Vector2 CameraRange
        {
            get { float half = Px.W * 0.5f; return new Vector2(half, Mathf.Max(half, WorldWidth - half)); }
        }

        // ---- Estado comum ----
        protected Walker player;
        protected FloorHUD hud;
        protected ChoiceMenu choice;
        protected TravelMap map;
        protected Transform worldRoot;
        /// <summary>Ponto de chegada usado ao abrir a cena (ex.: "chegada_bi_esq").</summary>
        protected string spawnUsed;
        protected readonly List<Spot> spots = new List<Spot>();
        private PauseMenu pauseMenu;
        private RectTransform screen;
        private Camera cam;
        private bool transitioning, paused, froze;
        private int resumedFrame = -1;
        private readonly List<(Transform t, float baseX, float factor)> parallax = new List<(Transform, float, float)>();
        private Spot nearest;

        protected bool Locked =>
            transitioning || paused || hud == null || hud.PopupOpen || hud.Fading || hud.ClosedFrame == Time.frameCount ||
            choice.Open || choice.ClosedFrame == Time.frameCount || resumedFrame == Time.frameCount ||
            (map != null && (map.Open || map.ClosedFrame == Time.frameCount));

        protected virtual void Start()
        {
            Time.timeScale = 1f;
            var clock = GameClock.Instance;
            if (clock != null && clock.State != GameClock.ClockState.Finished) { clock.Freeze(); froze = true; }

            worldRoot = new GameObject("Mundo").transform;
            var gl = new GameObject("Luz_Global").AddComponent<Light2D>();
            gl.lightType = Light2D.LightType.Global;
            gl.color = GlobalLight;
            gl.intensity = GlobalIntensity;

            BuildCanvas();
            string sp = GameState.ConsumeSpawn() ?? DefaultSpawn;
            if (!SpawnPoints.TryGetValue(sp, out var start)) { sp = DefaultSpawn; start = SpawnPoints[DefaultSpawn]; }
            spawnUsed = sp;
            player = Walker.Create(worldRoot, start, WalkArea);
            if (start.x > WorldWidth * 0.5f) player.Face(Vector2.left);
            player.blockers.AddRange(Blockers);

            cam = Camera.main;
            BuildWorld();
            hud.SetObjective(Objective());
            hud.SetHint(Controls);
            SnapCamera();
            StartCoroutine(hud.FadeTo(0f, 0.5f));
            OnEntered();
        }

        protected virtual void OnEntered() { }

        protected virtual void OnDestroy()
        {
            if (froze && GameClock.Instance != null) GameClock.Instance.Unfreeze();
            Time.timeScale = 1f;
        }

        private void BuildCanvas()
        {
            var cgo = new GameObject("Canvas_Campus", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            cgo.layer = 5;
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.pixelPerfect = true;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Px.W, Px.H);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            screen = Px.Rect("Tela", cgo.transform);
            screen.anchorMin = screen.anchorMax = screen.pivot = new Vector2(0.5f, 0.5f);
            screen.anchoredPosition = Vector2.zero;
            screen.sizeDelta = new Vector2(Px.W, Px.H);

            hud = screen.gameObject.AddComponent<FloorHUD>();
            hud.Build(screen, fontTitle, fontBody);
            var inv = screen.gameObject.AddComponent<InventoryHUD>();
            inv.Build(screen, fontBody);
            choice = screen.gameObject.AddComponent<ChoiceMenu>();
            choice.Build(screen, fontTitle, fontBody);
            pauseMenu = screen.gameObject.AddComponent<PauseMenu>();
            pauseMenu.Build(screen, fontTitle, fontBody);
            map = screen.gameObject.AddComponent<TravelMap>();
            map.Build(screen, fontTitle, fontBody);
        }

        // ------------------------------------------------------------

        protected virtual void Update()
        {
            if (player == null) return;
            player.locked = Locked;
            var k = Keyboard.current;
            if (k == null || paused) return;

            if (!Locked)
            {
                if (k.escapeKey.wasPressedThisFrame)
                {
                    paused = true;
                    Time.timeScale = 0f;
                    GameAudio.Sfx("sfx_menu_confirmar");
                    pauseMenu.Show(() => { paused = false; resumedFrame = Time.frameCount; Time.timeScale = 1f; },
                                   () => { paused = false; Time.timeScale = 1f; GoTo(FloorBase.MenuScene, null); });
                    return;
                }
                if (k.mKey.wasPressedThisFrame)
                {
                    OpenMap();
                    return;
                }
                if (k.iKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame)
                {
                    hud.Popup("ITENS", InventoryHUD.ListText());
                    return;
                }

                nearest = null;
                float best = float.MaxValue;
                Vector2 pp = player.transform.position;
                foreach (var s in spots)
                {
                    if (s.visible != null && !s.visible()) continue;
                    float d = Vector2.Distance(new Vector2(pp.x, pp.y * 1.6f), new Vector2(s.pos.x, s.pos.y * 1.6f));
                    if (d < s.radius && d < best) { best = d; nearest = s; }
                }
                string lbl = nearest != null ? nearest.label?.Invoke() : null;
                hud.SetHint(lbl != null ? "E  " + lbl : Controls);

                if (nearest != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                {
                    if (Mathf.Abs(nearest.pos.x - pp.x) > 4f) player.Face(new Vector2(Mathf.Sign(nearest.pos.x - pp.x), 0));
                    nearest.act?.Invoke();
                    hud.SetObjective(Objective());
                }
            }
        }

        protected virtual void LateUpdate()
        {
            if (cam == null || player == null) return;
            float half = Px.W * 0.5f;
            var range = CameraRange;
            float x = Mathf.Clamp(player.transform.position.x, range.x, Mathf.Max(range.x, range.y));
            cam.transform.position = new Vector3(Mathf.Round(x), Px.H * 0.5f, -10f);
            float shift = x - half;
            foreach (var p in parallax)
                if (p.t != null) p.t.position = new Vector3(Mathf.Round(p.baseX - shift * (p.factor - 1f)), p.t.position.y, p.t.position.z);
        }

        protected void SnapCamera() => LateUpdate();

        // ---- Utilitários para as cenas ----

        /// <summary>Fundo pintado com o canto inferior esquerdo em (x, 0).</summary>
        protected SpriteRenderer Background(string tex, float x, int order = -3000)
        {
            var go = new GameObject("Fundo_" + tex);
            go.transform.SetParent(worldRoot, false);
            go.transform.position = new Vector3(x, 0, 0);
            var sr = CampusArt.AddRenderer(go);
            sr.sprite = CampusArt.Whole(tex, Vector2.zero);
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Objeto de pé no chão: pivô nos pés (footRow a partir do topo), ordenado pela altura.</summary>
        protected SpriteRenderer Prop(string name, string tex, Vector2 feet, int footRowFromTop = -1, bool sortByFeet = true)
        {
            var t = CampusArt.Tex(tex);
            if (t == null) return null;
            int fr = footRowFromTop < 0 ? t.height : footRowFromTop;
            return PropSprite(name, CampusArt.FromTexture(t, new Vector2(t.width * 0.5f, t.height - fr)), feet, sortByFeet);
        }

        protected SpriteRenderer PropSprite(string name, Sprite s, Vector2 feet, bool sortByFeet = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(worldRoot, false);
            go.transform.position = new Vector3(Mathf.Round(feet.x), Mathf.Round(feet.y), 0);
            var sr = CampusArt.AddRenderer(go);
            sr.sprite = s;
            sr.sortingOrder = sortByFeet ? Walker.SortOrder(feet.y) : -2000;
            return sr;
        }

        /// <summary>Camada com parallax (factor &gt; 1 = primeiro plano, passa mais rápido).</summary>
        protected void Parallax(Transform t, float factor) => parallax.Add((t, t.position.x, factor));

        protected Light2D PointLight(string name, Vector2 pos, float radius, Color color, float intensity = 1f)
        {
            var go = new GameObject("Luz_" + name);
            go.transform.SetParent(worldRoot, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.pointLightInnerRadius = radius * 0.15f;
            l.pointLightOuterRadius = radius;
            l.falloffIntensity = 0.7f;
            return l;
        }

        protected static readonly Color LampWarm = new Color(1f, 0.78f, 0.45f);
        protected static readonly Color WindowWarm = new Color(1f, 0.85f, 0.6f);
        protected static readonly Color CoolWhite = new Color(0.8f, 0.9f, 1f);

        protected Spot AddSpot(string name, Vector2 pos, Func<string> label, Action act, Func<bool> visible = null, float radius = 34f)
        {
            var s = new Spot { name = name, pos = pos, label = label, act = act, visible = visible, radius = radius };
            spots.Add(s);
            return s;
        }

        /// <summary>Uma sequência de falas, uma janela por vez.</summary>
        protected void Say(string speaker, params string[] lines) => SayThen(speaker, null, lines);

        protected void SayThen(string speaker, Action done, params string[] lines)
        {
            int i = 0;
            Action next = null;
            next = () =>
            {
                if (i >= lines.Length) { done?.Invoke(); hud.SetObjective(Objective()); return; }
                string l = lines[i++];
                hud.Popup(speaker, l, next, i < lines.Length ? "[E] continuar" : "[E] fechar");
            };
            next();
        }

        /// <summary>Pista opcional da Moema (conquista): marca e comenta só na primeira vez.</summary>
        protected void Clue(string id, string text)
        {
            if (GameState.Flag("pista_" + id)) { hud.Popup("PISTA", text); return; }
            GameState.Set("pista_" + id);
            GameAudio.Sfx("sfx_pegar", 0.6f);
            hud.Popup($"PISTA DA MOEMA ({GameState.CluesFound}/{GameState.CluesTotal})", text);
        }

        /// <summary>Abre o mapa do campus. Ao fim da caminhada no mapa, a tela escurece e o guarda aparece no destino.</summary>
        protected void OpenMap() => OpenMap(0);

        /// <param name="dir">-1/+1: já deixa escolhido o local vizinho à esquerda/direita.</param>
        protected void OpenMap(int dir)
        {
            if (map == null || Locked) return;
            GameAudio.Sfx("sfx_menu_confirmar", 0.6f);
            map.Show(MapHere, MapInside, dest => StartCoroutine(TravelTo(dest)), dir);
        }

        private IEnumerator TravelTo(string dest)
        {
            transitioning = true;
            yield return hud.FadeTo(1f, 0.35f);
            map.Hide();
            int from = TravelMap.IndexOf(MapHere), to = TravelMap.IndexOf(dest);
            // Chega pela esquerda quem vem de um local à esquerda (e vice-versa).
            string spawn = "chegada_" + dest + (to >= from ? "_esq" : "_dir");
            if (ArriveInScene(spawn))
            {
                SnapCamera();
                hud.SetObjective(Objective());
                hud.SetHint(Controls);
                yield return new WaitForSecondsRealtime(0.1f);
                yield return hud.FadeTo(0f, 0.4f);
                transitioning = false;
            }
            else
            {
                transitioning = false;
                GoTo("Campus", spawn);
            }
        }

        protected void GoTo(string scene, string spawn)
        {
            if (transitioning) return;
            StartCoroutine(Transition(scene, spawn));
        }

        private IEnumerator Transition(string scene, string spawn)
        {
            transitioning = true;
            yield return hud.FadeTo(1f, 0.5f);
            if (Application.CanStreamedLevelBeLoaded(scene))
            {
                GameState.Spawn = spawn;
                SceneManager.LoadScene(scene);
            }
            else
            {
                Debug.LogWarning($"[Campus] A cena '{scene}' não está na lista de cenas do build.");
                yield return hud.FadeTo(0f, 0.3f);
                transitioning = false;
            }
        }
    }
}
