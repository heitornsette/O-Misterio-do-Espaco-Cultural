using UnityEngine;

namespace Misterio.Puzzles
{
    /// <summary>
    /// The night clock as pure logic, so the rules that govern the whole game can be unit tested.
    /// The MonoBehaviour session owns an instance of this and forwards its events.
    /// </summary>
    public sealed class NightClock
    {
        public const float StartFictionMinutes = 0f;      // 02:00
        public const float DawnFictionMinutes = 240f;     // 06:00
        public const float FictionMinutesPerSecond = 0.25f; // 15 fiction minutes per real minute

        public const float CreditPerFloorCap = 30f;
        public const float CreditTotalCap = 120f;
        public const float FloorTargetSeconds = 240f;

        public float FictionMinutes { get; private set; }
        public float CreditSeconds { get; private set; }
        public bool Running { get; set; }

        public bool ReachedDawn => FictionMinutes >= DawnFictionMinutes;

        public void Reset()
        {
            FictionMinutes = StartFictionMinutes;
            CreditSeconds = 0f;
            Running = false;
        }

        /// <summary>Adds credit, respecting the per-floor and total caps from the design document.</summary>
        public void AddCredit(float seconds)
        {
            if (seconds <= 0f) return;
            float room = Mathf.Min(CreditPerFloorCap, CreditTotalCap - CreditSeconds);
            CreditSeconds = Mathf.Clamp(CreditSeconds + Mathf.Min(seconds, room), 0f, CreditTotalCap);
        }

        /// <summary>Credit for finishing a floor under its target; capped per floor.</summary>
        public void AwardFloorCredit(float elapsedInFloor) =>
            AddCredit(Mathf.Max(0f, FloorTargetSeconds - Mathf.Max(0f, elapsedInFloor)));

        /// <summary>
        /// Advances the night. One second of credit pauses one second of the clock, and the clock
        /// never runs past dawn. Returns true when dawn is reached on this tick.
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (!Running || deltaTime <= 0f || ReachedDawn) return false;

            float remaining = deltaTime;
            if (CreditSeconds > 0f)
            {
                float spend = Mathf.Min(CreditSeconds, remaining);
                CreditSeconds -= spend;
                remaining -= spend;
            }
            if (remaining <= 0f) return false;

            float before = FictionMinutes;
            FictionMinutes = Mathf.Min(DawnFictionMinutes, FictionMinutes + remaining * FictionMinutesPerSecond);
            return before < DawnFictionMinutes && FictionMinutes >= DawnFictionMinutes;
        }

        /// <summary>Jumps to dawn without consuming play time, used by the dawn cutscenes.</summary>
        public bool JumpToDawn()
        {
            if (ReachedDawn) return false;
            FictionMinutes = DawnFictionMinutes;
            return true;
        }

        public string ClockText()
        {
            int totalMinutes = 120 + Mathf.FloorToInt(FictionMinutes);
            int hours = (totalMinutes / 60) % 24;
            int minutes = totalMinutes % 60;
            return hours.ToString("00") + ":" + minutes.ToString("00");
        }
    }
}
