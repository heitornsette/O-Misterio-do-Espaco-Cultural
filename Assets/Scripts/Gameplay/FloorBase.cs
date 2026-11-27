using System;
using System.Collections;
using System.Collections.Generic;
using EspacoCultural.Audio;
using EspacoCultural.Jogo;
using EspacoCultural.Tempo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EspacoCultural.Gameplay
{
    public enum StepResult { Blocked, Moved }

    /// <summary>
    /// Base de todos os andares. Monta a tela em tempo de execução (Canvas 640×360 em pixels):
    /// fundo, camada do piso, camada do mundo (ordenada pelos pés), efeitos e HUD; cria o guarda;
    /// liga o puzzle ao relógio global (PuzzleTimer) e cuida de pausa, saídas, fim de tempo,
    /// inventário e cansaço. Cada andar herda daqui e só descreve o seu puzzle.
    ///
    /// Campus: o jogador pode sair do museu e voltar. Por isso cada andar
    ///  - escolhe onde o guarda aparece pelo GameState.Spawn (SpawnPoints);
    ///  - mostra a introdução só na primeira visita;
    ///  - lembra se já foi resolvido (flag "resolvido{N}") e salva o progresso parcial (SaveProgress).
    /// </summary>
    public abstract class FloorBase : MonoBehaviour
    {
        [Header("Fontes (preenchidas pelo construtor de cenas)")]
        public Font fontTitle;
        public Font fontBody;

        public const string MenuScene = "MenuInicial";
        public const string CampusScene = "Campus";

        /// <summary>Quando o andar é recarregado depois da CC04A, pula a janela de introdução.</summary>
        public static bool SkipNextIntro;

        // ---- O que cada andar define ----
        protected abstract int FloorNumber { get; }
        protected abstract string BackgroundName { get; }
        protected abstract FloorGrid CreateGrid();
        protected abstract Vector2Int DefaultStart { get; }
        protected abstract string IntroTitle { get; }
        protected abstract string IntroText { get; }
        protected abstract string ObjectiveText();
        protected abstract void BuildLevel();
        protected virtual string Controls => "SETAS/WASD andar    E interagir    I itens    ESC pausa";

        /// <summary>Pontos de chegada (id do GameState.Spawn → célula e direção).</summary>
        protected virtual Dictionary<string, (Vector2Int, Vector2Int)> SpawnPoints =>
            new Dictionary<string, (Vector2Int, Vector2Int)>();

        /// <summary>O que fica atrás do fundo (ex.: o céu visto pelas partes transparentes da janela).</summary>
        protected virtual void BuildBackdrop(RectTransform parent) { }

        /// <summary>Obstáculos fixos (vitrines, barreiras, objetos no chão...).</summary>
        public virtual bool IsBlocked(Vector2Int c) => false;

        /// <summary>Regra padrão de passo: anda se a célula existe e está livre.</summary>
        public virtual StepResult TryStep(PlayerController p, Vector2Int d)
        {
            var t = p.actor.cell + d;
            if (!Grid.Inside(t) || IsBlocked(t)) return StepResult.Blocked;
            p.Step(d);
            return StepResult.Moved;
        }

        public virtual void OnPlayerArrived(PlayerController p, Vector2Int c)
        {
            if (stepExits.TryGetValue(c, out var exit) && (exit.open == null || exit.open()))
                exit.go();
        }

        public virtual void Interact(PlayerController p, Vector2Int cell, Vector2Int facing) { }

        /// <summary>Texto da barra de baixo conforme onde o guarda está (null = controles).</summary>
        protected virtual string ContextHint(Vector2Int cell, Vector2Int facing) => null;

        /// <summary>Guarda o progresso parcial no GameState (chamado ao sair da cena).</summary>
        protected virtual void SaveProgress() { }

        // ---- Estado comum ----
        public FloorGrid Grid { get; private set; }
        public FloorHUD Hud { get; private set; }
        public ChoiceMenu Choice { get; private set; }
        public PlayerController Player { get; private set; }
        public bool Solved { get; private set; }
        /// <summary>O puzzle já tinha sido resolvido numa visita anterior.</summary>
        protected bool AlreadySolved { get; private set; }
        protected Vector2Int PlayerStart { get; private set; }
        protected Vector2Int StartFacing { get; private set; } = new Vector2Int(0, 1);

        protected RectTransform screen, floorLayer, world, fx;
        protected readonly List<GridActor> sortables = new List<GridActor>();
        protected bool extraLock;

        private readonly Dictionary<Vector2Int, (Func<bool> open, Action go)> stepExits =
            new Dictionary<Vector2Int, (Func<bool>, Action)>();

        private bool transitioning, paused, intro;
        private int resumedFrame = -1;
        private PuzzleTimer timer;
        private PauseMenu pauseMenu;
        private GameClock boundClock;

        public bool InputLocked =>
            intro || paused || transitioning || extraLock || Hud == null || Hud.PopupOpen ||
            Hud.ClosedFrame == Time.frameCount || resumedFrame == Time.frameCount || Hud.Fading ||
            (Choice != null && (Choice.Open || Choice.ClosedFrame == Time.frameCount));

        // ------------------------------------------------------------

        protected virtual void Start()
        {
            Time.timeScale = 1f;
            EnsureClock();
            BuildCanvas();
            Grid = CreateGrid();
            AlreadySolved = GameState.Flag("resolvido" + FloorNumber);

            string spawn = GameState.ConsumeSpawn();
            PlayerStart = DefaultStart;
            if (spawn != null && SpawnPoints.TryGetValue(spawn, out var sp)) { PlayerStart = sp.Item1; StartFacing = sp.Item2; }

            BuildBackdrop(screen);
            Px.Raw("Fundo", screen, Px.Tex(BackgroundName), 0, 0, 640, 360);
            floorLayer = Layer("Piso");
            world = Layer("Mundo");
            fx = Layer("Efeitos");

            Hud = screen.gameObject.AddComponent<FloorHUD>();
            Hud.Build(screen, fontTitle, fontBody);
            var inv = screen.gameObject.AddComponent<InventoryHUD>();
            inv.Build(screen, fontBody);
            Choice = screen.gameObject.AddComponent<ChoiceMenu>();
            Choice.Build(screen, fontTitle, fontBody);
            pauseMenu = screen.gameObject.AddComponent<PauseMenu>();
            pauseMenu.Build(screen, fontTitle, fontBody);

            Player = SpawnPlayer(PlayerStart);
            Player.Face(StartFacing);
            Player.Idle();
            if (AlreadySolved) Solved = true;
            BuildLevel();
            Hud.SetObjective(ObjectiveText());
            Hud.SetHint(Controls);

            timer = gameObject.AddComponent<PuzzleTimer>();
            timer.floor = FloorNumber;
            timer.beginOnStart = false;

            gameObject.AddComponent<FatigueTicker>();

            boundClock = GameClock.Instance;
            if (boundClock != null) boundClock.onTimeUp.AddListener(OnTimeUp);

            StartCoroutine(IntroRoutine());
        }

        protected virtual void OnDestroy()
        {
            if (boundClock != null) boundClock.onTimeUp.RemoveListener(OnTimeUp);
            Time.timeScale = 1f;
        }

        private static void EnsureClock()
        {
            var c = GameClock.Instance;
            if (c == null)
            {
                var go = new GameObject("GameClock");
                c = go.AddComponent<GameClock>();
            }
            if (c.State == GameClock.ClockState.NotStarted) c.StartClock();
        }

        private void BuildCanvas()
        {
            var cgo = new GameObject("Canvas_Andar", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            cgo.layer = 5;
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            canvas.pixelPerfect = true;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Px.W, Px.H);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            screen = Px.Rect("Tela", cgo.transform);
            screen.anchorMin = screen.anchorMax = screen.pivot = new Vector2(0.5f, 0.5f);
            screen.anchoredPosition = Vector2.zero;
            screen.sizeDelta = new Vector2(Px.W, Px.H);
            screen.gameObject.AddComponent<RectMask2D>();
        }

        private RectTransform Layer(string name)
        {
            var rt = Px.Rect(name, screen);
            Px.Place(rt, 0, 0, Px.W, Px.H);
            return rt;
        }

        private PlayerController SpawnPlayer(Vector2Int start)
        {
            var a = GridActor.Create("Guarda", world, Grid, start, Px.Tex("guarda_idle_frente_sheet"), 113, 128, 128, true);
            var p = a.gameObject.AddComponent<PlayerController>();
            p.floor = this;
            p.actor = a;
            p.idleFront = Px.Tex("guarda_idle_frente_sheet");
            p.idleBack = Px.Tex("guarda_idle_costas_sheet");
            p.walkFront = Px.Tex("guarda_andar_frente_sheet");
            p.walkBack = Px.Tex("guarda_andar_costas_sheet");
            sortables.Add(a);
            p.Idle();
            return p;
        }

        /// <summary>Cria um objeto de pé no grid e o inclui na ordenação por profundidade.</summary>
        protected GridActor Spawn(string name, string texName, Vector2Int cell, int footRow, bool animated = false, int frameW = -1, int frameH = -1)
        {
            var a = GridActor.Create(name, world, Grid, cell, Px.Tex(texName), footRow, frameW, frameH, animated);
            sortables.Add(a);
            return a;
        }

        /// <summary>Saída ao pisar na célula (open = null: sempre aberta).</summary>
        protected void AddStepExit(Vector2Int cell, Func<bool> open, Action go) => stepExits[cell] = (open, go);

        /// <summary>Placa "SAÍDA" com seta, desenhada no piso da célula (escada de serviço, porta...).</summary>
        protected void ExitSign(Vector2Int cell, string text, bool down = true)
        {
            var p = Grid.TopLeft(cell);
            var box = Px.Box("Saida_" + text, floorLayer, new Color(0.18f, 0.55f, 0.35f, 0.85f), p.x + 4, p.y + 5, Grid.cellW - 8, Grid.cellH - 10);
            Px.Label("Texto", box.rectTransform, fontBody, 8, Color.white, 0, 3, Grid.cellW - 8, 12, TextAnchor.UpperCenter).text =
                (down ? "▼ " : "▲ ") + text;
        }

        private IEnumerator IntroRoutine()
        {
            var clock = GameClock.Instance;
            bool firstVisit = !GameState.Flag("intro" + FloorNumber);
            if (SkipNextIntro || !firstVisit)
            {
                SkipNextIntro = false;
                yield return Hud.FadeTo(0f, 0.4f);
                if (!AlreadySolved) timer.BeginPuzzle();
                OnEntered();
                yield break;
            }
            GameState.Set("intro" + FloorNumber);
            intro = true;
            if (clock != null) clock.Freeze();
            yield return Hud.FadeTo(0f, 0.6f);
            Hud.Popup(IntroTitle, IntroText, () =>
            {
                intro = false;
                if (clock != null) clock.Unfreeze();
                timer.BeginPuzzle();
                OnEntered();
            }, "[E] começar");
        }

        /// <summary>Depois da introdução (ou da chegada, em visitas seguintes).</summary>
        protected virtual void OnEntered() { }

        protected virtual void Update()
        {
            var k = Keyboard.current;
            if (k == null || Hud == null) return;

            if (paused) return;

            if (!transitioning && !intro && !extraLock && !Hud.PopupOpen && Hud.ClosedFrame != Time.frameCount &&
                resumedFrame != Time.frameCount && (Choice == null || (!Choice.Open && Choice.ClosedFrame != Time.frameCount)))
            {
                if (k.escapeKey.wasPressedThisFrame)
                {
                    paused = true;
                    Time.timeScale = 0f;
                    GameAudio.Sfx("sfx_menu_confirmar");
                    pauseMenu.Show(() =>
                    {
                        paused = false;
                        resumedFrame = Time.frameCount;
                        Time.timeScale = 1f;
                    }, () =>
                    {
                        paused = false;
                        Time.timeScale = 1f;
                        GoTo(MenuScene);
                    });
                    return;
                }
                if (k.iKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame)
                {
                    Hud.Popup("ITENS", InventoryHUD.ListText());
                    return;
                }
            }

            if (!InputLocked && Player != null)
                Hud.SetHint(ContextHint(Player.actor.cell, Player.Facing) ?? Controls);
        }

        protected virtual void LateUpdate()
        {
            sortables.RemoveAll(a => a == null);
            sortables.Sort((a, b) => a.FeetPos.y.CompareTo(b.FeetPos.y));
            for (int i = 0; i < sortables.Count; i++) sortables[i].transform.SetSiblingIndex(i);
        }

        // ---- Fluxo ----

        /// <summary>Puzzle resolvido: avisa o relógio (crédito) e mostra a mensagem.</summary>
        protected void MarkSolved(string message)
        {
            if (Solved) return;
            Solved = true;
            GameState.Set("resolvido" + FloorNumber);
            var clock = GameClock.Instance;
            float before = clock != null ? clock.TotalCreditGranted : 0f;
            timer.Complete();
            float gained = clock != null ? clock.TotalCreditGranted - before : 0f;
            GameAudio.Sfx("sfx_sucesso");
            Hud.Toast(gained > 0.5f ? $"{message}\n+{Mathf.RoundToInt(gained)}s de crédito no relógio!" : message, 4f);
            Hud.SetObjective(ObjectiveText());
        }

        protected void GoTo(string scene, string spawn = null)
        {
            if (transitioning) return;
            StartCoroutine(TransitionRoutine(scene, spawn));
        }

        /// <summary>Desce ao térreo pela escada de serviço.</summary>
        protected void GoDown() => GoTo("Andar1", "terreo_escada");

        private IEnumerator TransitionRoutine(string scene, string spawn)
        {
            transitioning = true;
            SaveProgress();
            yield return Hud.FadeTo(1f, 0.6f);
            if (Application.CanStreamedLevelBeLoaded(scene))
            {
                GameState.Spawn = spawn;
                SceneManager.LoadScene(scene);
            }
            else
            {
                Debug.LogWarning($"[Andar] A cena '{scene}' não está na lista de cenas do build.");
                yield return Hud.FadeTo(0f, 0.4f);
                transitioning = false;
            }
        }

        /// <summary>06:00 antes de terminar: CC09 (andares 1–3) ou CC10 (salão).</summary>
        private void OnTimeUp(int floor)
        {
            extraLock = true;
            if (floor <= 0) floor = FloorNumber;
            Misterio.Core.GameSession.Instance.TimedOutFloor = floor;
            GoTo(floor == 4 ? Misterio.Puzzles.RouteRules.Cutscene10 : Misterio.Puzzles.RouteRules.Cutscene09);
        }

        // ---- Utilitários para os andares ----

        protected static readonly Vector2Int Up = new Vector2Int(0, -1);
        protected static readonly Vector2Int Down = new Vector2Int(0, 1);

        protected RawImage FloorTile(string name, string tex, Vector2Int cell)
        {
            var p = Grid.TopLeft(cell);
            return Px.Raw(name, floorLayer, Px.Tex(tex), p.x, p.y, Grid.cellW, Grid.cellH);
        }

        protected static int Idx(Vector2Int c) => c.x + c.y * 13;
        protected static Vector2Int Cell(int i) => new Vector2Int(i % 13, i / 13);
    }
}
