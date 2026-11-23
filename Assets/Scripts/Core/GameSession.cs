using System;
using Misterio.Puzzles;
using UnityEngine;

namespace Misterio.Core
{
    /// <summary>
    /// Persistent state for one playthrough: the night clock (rules live in NightClock, which is
    /// unit tested), the time credit, which floors are restored and what the archive run carries.
    /// The clock freezes during cutscenes; callers simply stop ticking it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        readonly NightClock clock = new NightClock();
        readonly bool[] solvedFloors = new bool[5];

        /// <summary>Raised whenever the clock, the credit or a floor flag changes.</summary>
        public event Action Changed;

        /// <summary>Raised once, when the clock reaches 06:00.</summary>
        public event Action DawnReached;

        // The caps are re-exposed so callers keep a single source of truth.
        public const float CreditPerFloorCap = NightClock.CreditPerFloorCap;
        public const float CreditTotalCap = NightClock.CreditTotalCap;
        public const float FloorTargetSeconds = NightClock.FloorTargetSeconds;
        public const float FictionMinutesPerSecond = NightClock.FictionMinutesPerSecond;
        public const float DawnFictionMinutes = NightClock.DawnFictionMinutes;
        public const float StartFictionMinutes = NightClock.StartFictionMinutes;

        public float FictionMinutes => clock.FictionMinutes;
        public float CreditSeconds => clock.CreditSeconds;
        public bool ClockRunning => clock.Running;
        public bool ReachedDawn => clock.ReachedDawn;
        public int CollectedPieces { get; private set; }

        /// <summary>Which floor was being played when the clock reached 06:00 (0 when none).</summary>
        public int TimedOutFloor { get; private set; }

        /// <summary>Floor that must be resumed after a failure cutscene (0 when none).</summary>
        public int ResumeFloor { get; private set; }

        /// <summary>
        /// Bitmask of the archive pieces already collected. It survives the CC04A failure cutscene,
        /// so being seen costs time and position but never the pieces the player already recovered.
        /// </summary>
        public int ArchivePieces { get; private set; }

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
            clock.Reset();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ResetRun()
        {
            clock.Reset();
            CollectedPieces = 0;
            ArchivePieces = 0;
            TimedOutFloor = 0;
            ResumeFloor = 0;
            for (int i = 0; i < solvedFloors.Length; i++) solvedFloors[i] = false;
            Changed?.Invoke();
        }

        public bool IsFloorSolved(int floor) =>
            floor > 0 && floor < solvedFloors.Length && solvedFloors[floor];

        public void MarkFloorSolved(int floor, float elapsedInFloor)
        {
            if (floor <= 0 || floor >= solvedFloors.Length || solvedFloors[floor]) return;
            solvedFloors[floor] = true;
            clock.AwardFloorCredit(elapsedInFloor);
            Changed?.Invoke();
        }

        public void AddCredit(float seconds)
        {
            clock.AddCredit(seconds);
            Changed?.Invoke();
        }

        public void CollectPiece(int amount = 1)
        {
            CollectedPieces = Mathf.Max(0, CollectedPieces + amount);
            Changed?.Invoke();
        }

        public bool HasArchivePiece(int index) =>
            index >= 0 && index < 32 && (ArchivePieces & (1 << index)) != 0;

        public void SetArchivePiece(int index, bool collected)
        {
            if (index < 0 || index >= 32) return;
            if (collected) ArchivePieces |= 1 << index;
            else ArchivePieces &= ~(1 << index);
        }

        public void SetClockRunning(bool running)
        {
            clock.Running = running;
            Changed?.Invoke();
        }

        public void MarkTimedOut(int floor) => TimedOutFloor = floor;

        public void RequestResumeFloor(int floor) => ResumeFloor = floor;

        public int ConsumeResumeFloor()
        {
            int floor = ResumeFloor;
            ResumeFloor = 0;
            return floor;
        }

        /// <summary>Advances the night through the tested rules and forwards the dawn event.</summary>
        public void Tick(float deltaTime)
        {
            float creditBefore = clock.CreditSeconds;
            bool dawn = clock.Tick(deltaTime);
            if (!Mathf.Approximately(creditBefore, clock.CreditSeconds)) Changed?.Invoke();
            if (dawn) DawnReached?.Invoke();
        }

        /// <summary>Skips ahead without consuming play time (used by the dawn cutscenes).</summary>
        public void JumpToDawn()
        {
            if (!clock.JumpToDawn()) return;
            Changed?.Invoke();
            DawnReached?.Invoke();
        }

        public string ClockText() => clock.ClockText();

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
