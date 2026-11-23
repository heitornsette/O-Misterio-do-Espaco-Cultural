using Misterio.Core;
using NUnit.Framework;
using UnityEngine;

namespace Misterio.Session.Tests
{
    /// <summary>
    /// Rules of the global clock. These are the rules the whole night depends on, so they are
    /// tested without a scene: the 02:00 start, the 15-minute-per-minute rate, the hourglass credit
    /// that pauses the clock, and the 06:00 ending.
    /// </summary>
    public sealed class GameSessionTests
    {
        GameObject host;
        GameSession session;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("SessionTest");
            session = host.AddComponent<GameSession>();
            session.ResetRun();
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
        }

        [Test]
        public void StartsAtTwoInTheMorning()
        {
            Assert.AreEqual("02:00", session.ClockText());
            Assert.IsFalse(session.ReachedDawn);
        }

        [Test]
        public void ClockOnlyRunsWhenEnabled()
        {
            session.Tick(10f);
            Assert.AreEqual("02:00", session.ClockText());

            session.SetClockRunning(true);
            session.Tick(10f);
            // 10 real seconds = 2.5 fiction minutes, rounded down on the display.
            Assert.AreEqual("02:02", session.ClockText());
        }

        [Test]
        public void FifteenFictionMinutesPerRealMinute()
        {
            session.SetClockRunning(true);
            session.Tick(60f); // one real minute
            Assert.AreEqual("02:15", session.ClockText());
            session.Tick(60f);
            Assert.AreEqual("02:30", session.ClockText());
        }

        [Test]
        public void SixteenMinutesOfPlayReachesDawn()
        {
            session.SetClockRunning(true);
            bool dawn = false;
            session.DawnReached += () => dawn = true;
            for (int i = 0; i < 960; i++) session.Tick(1f); // 16 real minutes
            Assert.IsTrue(dawn);
            Assert.IsTrue(session.ReachedDawn);
            Assert.AreEqual("06:00", session.ClockText());
        }

        [Test]
        public void DawnFiresOnlyOnce()
        {
            session.SetClockRunning(true);
            int fired = 0;
            session.DawnReached += () => fired++;
            for (int i = 0; i < 1200; i++) session.Tick(1f);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void ClockNeverGoesPastDawn()
        {
            session.SetClockRunning(true);
            for (int i = 0; i < 2000; i++) session.Tick(1f);
            Assert.AreEqual("06:00", session.ClockText());
            Assert.AreEqual(240f, session.FictionMinutes, 0.01f);
        }

        [Test]
        public void CreditPausesTheClockSecondForSecond()
        {
            session.SetClockRunning(true);
            session.AddCredit(10f);
            session.Tick(10f); // fully absorbed by the credit
            Assert.AreEqual("02:00", session.ClockText());
            Assert.AreEqual(0f, session.CreditSeconds, 0.01f);

            session.Tick(10f); // credit is gone, the clock moves again
            Assert.AreEqual("02:02", session.ClockText());
        }

        [Test]
        public void CreditIsCappedPerFloor()
        {
            session.AddCredit(999f);
            Assert.AreEqual(GameSession.CreditPerFloorCap, session.CreditSeconds, 0.01f);
        }

        [Test]
        public void CreditIsCappedInTotal()
        {
            // The document allows 2 minutes of credit across the whole night.
            session.AddCredit(999f);
            session.AddCredit(999f);
            session.AddCredit(999f);
            session.AddCredit(999f);
            Assert.AreEqual(GameSession.CreditTotalCap, session.CreditSeconds, 0.01f);

            session.AddCredit(999f);
            Assert.AreEqual(GameSession.CreditTotalCap, session.CreditSeconds, 0.01f);
        }

        [Test]
        public void SolvingAFreshFloorAwardsTheRemainingTimeUpToTheFloorCap()
        {
            // 60 s of the target remain, but the document caps a floor at 30 s of credit.
            session.MarkFloorSolved(1, 180f);
            Assert.AreEqual(GameSession.CreditPerFloorCap, session.CreditSeconds, 0.01f);
            Assert.IsTrue(session.IsFloorSolved(1));
        }

        [Test]
        public void SolvingQuicklyStillAwardsOnlyTheFloorCap()
        {
            session.MarkFloorSolved(2, 0f); // 240 s remain, cap is 30
            Assert.AreEqual(GameSession.CreditPerFloorCap, session.CreditSeconds, 0.01f);
        }

        [Test]
        public void SolvingAfterTheTargetAwardsNothing()
        {
            session.MarkFloorSolved(1, 300f);
            Assert.AreEqual(0f, session.CreditSeconds, 0.01f);
            Assert.IsTrue(session.IsFloorSolved(1));
        }

        [Test]
        public void AFloorCannotBeSolvedTwice()
        {
            session.MarkFloorSolved(1, 0f);
            float first = session.CreditSeconds;
            session.MarkFloorSolved(1, 0f);
            Assert.AreEqual(first, session.CreditSeconds, 0.01f);
            Assert.AreEqual(1, session.SolvedCount);
        }

        [Test]
        public void TimedOutFloorIsRecordedForTheCutsceneVariant()
        {
            session.MarkTimedOut(3);
            Assert.AreEqual(3, session.TimedOutFloor);
        }

        [Test]
        public void ResumeFloorIsConsumedOnce()
        {
            session.RequestResumeFloor(3);
            Assert.AreEqual(3, session.ConsumeResumeFloor());
            Assert.AreEqual(0, session.ConsumeResumeFloor());
        }

        [Test]
        public void ArchivePiecesSurviveAFailure()
        {
            session.SetArchivePiece(0, true);
            session.SetArchivePiece(2, true);
            Assert.IsTrue(session.HasArchivePiece(0));
            Assert.IsFalse(session.HasArchivePiece(1));
            Assert.IsTrue(session.HasArchivePiece(2));

            // A failure does not clear them; only a new run does.
            session.RequestResumeFloor(3);
            Assert.IsTrue(session.HasArchivePiece(0));

            session.ResetRun();
            Assert.IsFalse(session.HasArchivePiece(0));
        }

        [Test]
        public void ClockTextRollsOverCorrectly()
        {
            session.SetClockRunning(true);
            session.Tick(240f); // one fiction hour
            Assert.AreEqual("03:00", session.ClockText());
            session.Tick(480f); // two more hours
            Assert.AreEqual("05:00", session.ClockText());
        }
    }
}
