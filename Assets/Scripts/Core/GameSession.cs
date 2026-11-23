using System;
using UnityEngine;

namespace Misterio.Core
{
    /// <summary>
    /// Persistent state for one playthrough: the global clock (02:00 -> 06:00), the time credit
    /// earned by solving puzzles early, and which floors are already restored.
    /// The clock freezes during cutscenes; callers simply stop ticking it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSession : MonoBehaviour
    {
        public const float StartFictionMinutes = 0f;    // 02:00
        public const float DawnFictionMinutes = 240f;   // 06:00
        public const float FictionMinutesPerSecond = 0.25f; // 15 fiction minutes per real minute

        public const float CreditPerFloorCap = 30f;
        public const float CreditTotalCap = 120f;
        public const float FloorTargetSeconds = 240f;

        public static GameSession Instance { get; private set; }

        public float FictionMinutes { get; private set; }
        public float CreditSeconds { get; private set; }
        public bool ClockRunning { get; private set; }
        public bool ReachedDawn => FictionMinutes >= DawnFictionMinutes;

        readonly bool[] solvedFloors = new bool[5];
        int collectedPieces;

        /// <summary>Raised whenever the clock, the credit or a floor flag changes.</summary>
        public event Action Changed;

        /// <summary>Raised once, when the clock reaches 06:00.</summary>
        public event Action DawnReached;

        public int CollectedPieces => collectedPieces;

        /// <summary>Which floor was being played when the clock reached 06:00 (0 when none).</summary>
        public int TimedOutFloor { get; private set; }

        /// <summary>Floor that must be resumed after a failure cutscene (0 when none).</summary>
        public int ResumeFloor { get; private set; }

        public void MarkTimedOut(int floor) => TimedOutFloor = floor;

        public void RequestResumeFloor(int floor) => ResumeFloor = floor;

        public int ConsumeResumeFloor()
        {
            int floor = ResumeFloor;
            ResumeFloor = 0;
            return floor;
        }
        public int SolvedCount
        {
            get
            {
                int total = 0;
                for (int i = 1; i < solvedFloors.Length; i++) if (solvedFloors[i]) total++;
                return total;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            FictionMinutes = StartFictionMinutes;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ResetRun()
        {
            FictionMinutes = StartFictionMinutes;
            CreditSeconds = 0f;
            collectedPieces = 0;
            ArchivePieces = 0;
            TimedOutFloor = 0;
            ResumeFloor = 0;
            for (int i = 0; i < solvedFloors.Length; i++) solvedFloors[i] = false;
            ClockRunning = false;
            Changed?.Invoke();
        }

        public bool IsFloorSolved(int floor) =>
            floor > 0 && floor < solvedFloors.Length && solvedFloors[floor];

        public void MarkFloorSolved(int floor, float elapsedInFloor)
        {
            if (floor <= 0 || floor >= solvedFloors.Length || solvedFloors[floor]) return;
            solvedFloors[floor] = true;
            AddCredit(Mathf.Max(0f, FloorTargetSeconds - Mathf.Max(0f, elapsedInFloor)));
            Changed?.Invoke();
        }

        public void AddCredit(float seconds)
        {
            if (seconds <= 0f) return;
            float room = Mathf.Min(CreditPerFloorCap, CreditTotalCap - CreditSeconds);
            CreditSeconds = Mathf.Clamp(CreditSeconds + Mathf.Min(seconds, room), 0f, CreditTotalCap);
            Changed?.Invoke();
        }

        public void CollectPiece(int amount = 1)
        {
            collectedPieces = Mathf.Max(0, collectedPieces + amount);
            Changed?.Invoke();
        }

        /// <summary>
        /// Bitmask of the archive pieces already collected. It survives the CC04A failure cutscene,
        /// so being seen costs time and position but never the pieces the player already recovered.
        /// </summary>
        public int ArchivePieces { get; private set; }

        public bool HasArchivePiece(int index) => index >= 0 && index < 32 && (ArchivePieces & (1 << index)) != 0;

        public void SetArchivePiece(int index, bool collected)
        {
            if (index < 0 || index >= 32) return;
            if (collected) ArchivePieces |= 1 << index;
            else ArchivePieces &= ~(1 << index);
        }

        public void SetClockRunning(bool running)
        {
            ClockRunning = running;
        }

        /// <summary>Advances the night. A second of credit pauses a second of the clock.</summary>
        public void Tick(float deltaTime)
        {
            if (!ClockRunning || deltaTime <= 0f) return;
            if (ReachedDawn) return;

            float remaining = deltaTime;
            if (CreditSeconds > 0f)
            {
                float spend = Mathf.Min(CreditSeconds, remaining);
                CreditSeconds -= spend;
                remaining -= spend;
                Changed?.Invoke();
            }
            if (remaining <= 0f) return;

            float before = FictionMinutes;
            FictionMinutes = Mathf.Min(DawnFictionMinutes, FictionMinutes + remaining * FictionMinutesPerSecond);
            if (FictionMinutes != before) Changed?.Invoke();
            if (before < DawnFictionMinutes && FictionMinutes >= DawnFictionMinutes) DawnReached?.Invoke();
        }

        /// <summary>Skips ahead without consuming play time (used by the dawn cutscenes).</summary>
        public void JumpToDawn()
        {
            if (ReachedDawn) return;
            FictionMinutes = DawnFictionMinutes;
            Changed?.Invoke();
            DawnReached?.Invoke();
        }

        public string ClockText()
        {
            int totalMinutes = 120 + Mathf.FloorToInt(FictionMinutes);
            int hours = (totalMinutes / 60) % 24;
            int minutes = totalMinutes % 60;
            return hours.ToString("00") + ":" + minutes.ToString("00");
        }

        /// <summary>Creates the session on demand so any scene can be played in isolation.</summary>
        public static GameSession EnsureExists()
        {
            if (Instance != null) return Instance;
            var existing = FindAnyObjectByType<GameSession>();
            if (existing != null) return existing;
            var host = new GameObject("GameSession");
            return host.AddComponent<GameSession>();
        }
    }
}
