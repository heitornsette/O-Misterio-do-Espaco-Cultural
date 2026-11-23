using Misterio.Puzzles;
using NUnit.Framework;

namespace Misterio.Puzzles.Tests
{
    /// <summary>
    /// Rules of the global clock, the hourglass credit and the 06:00 ending. The whole night depends
    /// on these, so they are tested as pure logic without any scene.
    /// </summary>
    public sealed class NightClockTests
    {
        static NightClock Running()
        {
            var clock = new NightClock();
            clock.Reset();
            clock.Running = true;
            return clock;
        }

        [Test]
        public void StartsAtTwoInTheMorning()
        {
            var clock = new NightClock();
            clock.Reset();
            Assert.AreEqual("02:00", clock.ClockText());
            Assert.IsFalse(clock.ReachedDawn);
        }

        [Test]
        public void DoesNotAdvanceWhileStopped()
        {
            var clock = new NightClock();
            clock.Reset();
            clock.Tick(30f);
            Assert.AreEqual("02:00", clock.ClockText());
        }

        [Test]
        public void FifteenFictionMinutesPerRealMinute()
        {
            var clock = Running();
            clock.Tick(60f);
            Assert.AreEqual("02:15", clock.ClockText());
            clock.Tick(60f);
            Assert.AreEqual("02:30", clock.ClockText());
            clock.Tick(240f);
            Assert.AreEqual("03:30", clock.ClockText());
        }

        [Test]
        public void SixteenMinutesOfPlayReachesDawn()
        {
            var clock = Running();
            bool dawn = false;
            for (int i = 0; i < 960; i++)
                if (clock.Tick(1f)) dawn = true;
            Assert.IsTrue(dawn);
            Assert.IsTrue(clock.ReachedDawn);
            Assert.AreEqual("06:00", clock.ClockText());
        }

        [Test]
        public void DawnIsReportedOnlyOnce()
        {
            var clock = Running();
            int fired = 0;
            for (int i = 0; i < 1200; i++)
                if (clock.Tick(1f)) fired++;
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void ClockNeverPassesDawn()
        {
            var clock = Running();
            for (int i = 0; i < 2000; i++) clock.Tick(1f);
            Assert.AreEqual("06:00", clock.ClockText());
            Assert.AreEqual(NightClock.DawnFictionMinutes, clock.FictionMinutes, 0.001f);
        }

        [Test]
        public void CreditPausesTheClockSecondForSecond()
        {
            var clock = Running();
            clock.AddCredit(10f);
            clock.Tick(10f);
            Assert.AreEqual("02:00", clock.ClockText());
            Assert.AreEqual(0f, clock.CreditSeconds, 0.001f);

            clock.Tick(60f);
            Assert.AreEqual("02:15", clock.ClockText());
        }

        [Test]
        public void CreditIsCappedPerFloor()
        {
            var clock = Running();
            clock.AddCredit(999f);
            Assert.AreEqual(NightClock.CreditPerFloorCap, clock.CreditSeconds, 0.001f);
        }

        [Test]
        public void CreditIsCappedInTotal()
        {
            var clock = Running();
            for (int i = 0; i < 10; i++) clock.AddCredit(999f);
            Assert.AreEqual(NightClock.CreditTotalCap, clock.CreditSeconds, 0.001f);
        }

        [Test]
        public void SolvingAFreshFloorAwardsTheRemainingTimeUpToTheFloorCap()
        {
            var clock = Running();
            clock.AwardFloorCredit(180f); // 60 s remain, cap is 30
            Assert.AreEqual(NightClock.CreditPerFloorCap, clock.CreditSeconds, 0.001f);
        }

        [Test]
        public void SolvingAfterTheTargetAwardsNothing()
        {
            var clock = Running();
            clock.AwardFloorCredit(300f);
            Assert.AreEqual(0f, clock.CreditSeconds, 0.001f);
        }

        [Test]
        public void NegativeCreditIsIgnored()
        {
            var clock = Running();
            clock.AddCredit(-50f);
            Assert.AreEqual(0f, clock.CreditSeconds, 0.001f);
        }

        [Test]
        public void JumpToDawnConsumesNoPlayTime()
        {
            var clock = new NightClock();
            clock.Reset();
            Assert.IsTrue(clock.JumpToDawn());
            Assert.AreEqual("06:00", clock.ClockText());
            Assert.IsFalse(clock.JumpToDawn(), "Dawn must only be jumped to once.");
        }

        [Test]
        public void CreditCannotPushTheClockPastDawn()
        {
            var clock = Running();
            clock.AddCredit(NightClock.CreditPerFloorCap);
            for (int i = 0; i < 1200; i++) clock.Tick(1f);
            Assert.AreEqual("06:00", clock.ClockText());
        }

        [Test]
        public void ResetClearsEverything()
        {
            var clock = Running();
            clock.AddCredit(20f);
            clock.Tick(120f);
            clock.Reset();
            Assert.AreEqual("02:00", clock.ClockText());
            Assert.AreEqual(0f, clock.CreditSeconds, 0.001f);
            Assert.IsFalse(clock.Running);
        }
    }
}
