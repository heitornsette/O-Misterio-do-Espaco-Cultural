using System;
using UnityEngine;
using UnityEngine.Events;

namespace EspacoCultural.Tempo
{
    /// <summary>
    /// Relógio global da madrugada (02:00 → 06:00).
    ///
    /// Regras (Documento de Narrativas, seção "Tempo e atmosfera"):
    /// - Começa às 02:00 e avança 24 minutos ficcionais por minuto real de controle ativo
    ///   (sem bônus, chega às 06:00 após 10 minutos de jogo ativo). Fica parado no campus.
    /// - Congela em todas as cutscenes (inclusive falha e resultado).
    /// - Cada puzzle tem meta de 4 min. Resolver antes da meta gera crédito igual ao tempo
    ///   restante, limitado a 30 s por andar e 2 min no total.
    /// - O crédito é consumido automaticamente enquanto o jogador está em controle:
    ///   cada segundo de crédito pausa o relógio por um segundo. O horário nunca volta.
    /// - Se chegar às 06:00 antes do circuito final, a partida termina (final de tempo).
    ///
    /// Persistente entre cenas (DontDestroyOnLoad). Acesse por GameClock.Instance.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameClock : MonoBehaviour
    {
        public enum ClockState { NotStarted, Running, Frozen, Finished }

        public static GameClock Instance { get; private set; }

        [Header("Horário (minutos desde 00:00)")]
        [Tooltip("02:00 = 120 minutos")]
        [SerializeField] private float startMinutes = 120f;
        [Tooltip("06:00 = 360 minutos")]
        [SerializeField] private float dawnMinutes = 360f;

        [Header("Ritmo")]
        [Tooltip("Minutos ficcionais por minuto real de controle ativo")]
        [SerializeField] private float fictionalMinutesPerRealMinute = 15f;

        [Header("Puzzles e crédito")]
        [SerializeField] private float puzzleGoalSeconds = 240f;
        [SerializeField] private float maxCreditPerFloorSeconds = 30f;
        [SerializeField] private float maxCreditTotalSeconds = 120f;
        [SerializeField] private int floorCount = 4;

        [Header("Teste")]
        [Tooltip("Multiplica a velocidade de tudo (só para testar). Deixe 1 no jogo final.")]
        [Min(0f)] public float debugSpeed = 1f;
        [Tooltip("Inicia o relógio sozinho ao dar Play (útil para testar uma cena isolada).")]
        [SerializeField] private bool autoStart = false;

        [Header("Eventos")]
        public UnityEvent onClockStarted;
        public UnityEvent<int> onPuzzleSolvedWithinGoal;   // andar (1–4)
        public UnityEvent<int> onTimeUp;                   // andar em que o tempo acabou (0 = fora de andar)

        // Eventos C# (para HUD e outros sistemas)
        public event Action<float> TimeChanged;            // minutos ficcionais atuais
        public event Action<float> CreditChanged;          // segundos de crédito disponíveis
        public event Action<ClockState> StateChanged;

        // ---- Estado ----
        public ClockState State { get; private set; } = ClockState.NotStarted;
        public float CurrentMinutes { get; private set; }
        public float CreditSeconds { get; private set; }
        public float TotalCreditGranted { get; private set; }
        public float ActivePlaySeconds { get; private set; }

        /// <summary>Andar cujo puzzle está ativo (1–4) ou 0 se nenhum.</summary>
        public int ActiveFloor { get; private set; }
        public float ActivePuzzleElapsed { get; private set; }
        public float ActivePuzzleRemaining => Mathf.Max(0f, puzzleGoalSeconds - ActivePuzzleElapsed);

        public bool IsCrediting => State == ClockState.Running && CreditSeconds > 0f;
        public bool IsDawn => CurrentMinutes >= dawnMinutes;
        public float PuzzleGoalSeconds => puzzleGoalSeconds;

        private bool[] solved;
        private bool[] solvedWithinGoal;
        private int freezeCount;
        private float lastNotifiedMinute = -1f;

        // ------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Ritmo da expansão do campus (25/11/2026): o relógio só corre dentro do museu,
            // então a madrugada passa mais rápido — 06:00 aos 10 min ativos. Fixado aqui para valer
            // mesmo nas cenas antigas que têm outros valores salvos no componente.
            fictionalMinutesPerRealMinute = 24f;
            puzzleGoalSeconds = 150f;
            maxCreditPerFloorSeconds = 20f;
            maxCreditTotalSeconds = 80f;
            ResetClock();
        }

        private void Start()
        {
            if (autoStart) StartClock();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Zera tudo para uma nova partida (não inicia o relógio).</summary>
        public void ResetClock()
        {
            CurrentMinutes = startMinutes;
            CreditSeconds = 0f;
            TotalCreditGranted = 0f;
            ActivePlaySeconds = 0f;
            ActiveFloor = 0;
            ActivePuzzleElapsed = 0f;
            freezeCount = 0;
            solved = new bool[floorCount + 1];
            solvedWithinGoal = new bool[floorCount + 1];
            SetState(ClockState.NotStarted);
            NotifyTime(true);
            CreditChanged?.Invoke(CreditSeconds);
        }

        /// <summary>Chamar quando o controle é liberado ao final da CC01.</summary>
        public void StartClock()
        {
            if (State != ClockState.NotStarted) return;
            SetState(freezeCount > 0 ? ClockState.Frozen : ClockState.Running);
            onClockStarted?.Invoke();
        }

        // ---------------- Cutscenes ----------------

        /// <summary>Congela relógio, meta do puzzle e crédito. Aceita chamadas aninhadas.</summary>
        public void Freeze()
        {
            freezeCount++;
            if (State == ClockState.Running) SetState(ClockState.Frozen);
        }

        /// <summary>Libera o congelamento (o controle voltou ao jogador).</summary>
        public void Unfreeze()
        {
            freezeCount = Mathf.Max(0, freezeCount - 1);
            if (freezeCount == 0 && State == ClockState.Frozen) SetState(ClockState.Running);
        }

        // ---------------- Puzzles ----------------

        /// <summary>
        /// Começa a contar a meta do puzzle do andar (1–4). Chamar quando o controle
        /// é devolvido ao jogador naquele andar. Repetir a chamada não zera a contagem.
        /// </summary>
        public void BeginPuzzle(int floor)
        {
            if (!ValidFloor(floor) || solved[floor]) return;
            if (ActiveFloor == floor) return;
            ActiveFloor = floor;
            ActivePuzzleElapsed = 0f;
        }

        /// <summary>
        /// Puzzle resolvido. Se foi antes da meta, gera crédito. Retorna o crédito ganho (s).
        /// </summary>
        public float CompletePuzzle(int floor)
        {
            if (!ValidFloor(floor) || solved[floor]) return 0f;
            solved[floor] = true;

            float gained = 0f;
            if (ActiveFloor == floor)
            {
                float remaining = ActivePuzzleRemaining;
                if (remaining > 0f)
                {
                    solvedWithinGoal[floor] = true;
                    gained = Mathf.Min(remaining, maxCreditPerFloorSeconds);
                    gained = Mathf.Min(gained, maxCreditTotalSeconds - TotalCreditGranted);
                    gained = Mathf.Max(0f, gained);
                    TotalCreditGranted += gained;
                    CreditSeconds += gained;
                    CreditChanged?.Invoke(CreditSeconds);
                    onPuzzleSolvedWithinGoal?.Invoke(floor);
                }
                ActiveFloor = 0;
                ActivePuzzleElapsed = 0f;
            }
            return gained;
        }

        public bool IsSolved(int floor) => ValidFloor(floor) && solved[floor];
        public bool WasSolvedWithinGoal(int floor) => ValidFloor(floor) && solvedWithinGoal[floor];

        /// <summary>Para conquistas: todos os puzzles dentro da meta.</summary>
        public bool AllSolvedWithinGoal()
        {
            for (int i = 1; i <= floorCount; i++) if (!solvedWithinGoal[i]) return false;
            return true;
        }

        /// <summary>Chamar quando o circuito final for resolvido: para o relógio sem final de tempo.</summary>
        public void FinishSuccess()
        {
            SetState(ClockState.Finished);
        }

        // ---------------- Loop ----------------

        private void Update()
        {
            if (State != ClockState.Running) return;

            float dt = Time.deltaTime * debugSpeed;   // respeita Time.timeScale (menu de pausa)
            if (dt <= 0f) return;

            ActivePlaySeconds += dt;
            if (ActiveFloor != 0) ActivePuzzleElapsed += dt;

            // Crédito pausa o relógio segundo a segundo.
            float advance = dt;
            if (CreditSeconds > 0f)
            {
                float used = Mathf.Min(CreditSeconds, dt);
                CreditSeconds -= used;
                advance -= used;
                CreditChanged?.Invoke(CreditSeconds);
            }

            if (advance > 0f)
            {
                CurrentMinutes += advance * (fictionalMinutesPerRealMinute / 60f);
                if (CurrentMinutes >= dawnMinutes)
                {
                    CurrentMinutes = dawnMinutes;
                    NotifyTime(true);
                    TimeUp();
                    return;
                }
            }
            NotifyTime(false);
        }

        private void TimeUp()
        {
            SetState(ClockState.Finished);
            onTimeUp?.Invoke(ActiveFloor);
        }

        // ---------------- Utilitários ----------------

        /// <summary>Formata minutos ficcionais como "HH:MM".</summary>
        public static string Format(float minutes)
        {
            int total = Mathf.FloorToInt(minutes);
            return $"{(total / 60) % 24:00}:{total % 60:00}";
        }

        public string CurrentTimeText => Format(CurrentMinutes);

        private bool ValidFloor(int floor) => floor >= 1 && floor <= floorCount;

        private void SetState(ClockState s)
        {
            if (State == s) return;
            State = s;
            StateChanged?.Invoke(s);
        }

        private void NotifyTime(bool force)
        {
            float minute = Mathf.Floor(CurrentMinutes);
            if (!force && minute == lastNotifiedMinute) return;
            lastNotifiedMinute = minute;
            TimeChanged?.Invoke(CurrentMinutes);
        }
    }
}
