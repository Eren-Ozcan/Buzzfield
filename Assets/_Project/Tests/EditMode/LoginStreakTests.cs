using System;
using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class LoginStreakTests
    {
        const double DayStartHour = 4;
        const int Cycle = 7;

        /// <summary>Unix seconds of a UTC calendar moment.</summary>
        static double Utc(int year, int month, int day, int hour, int minute = 0) =>
            new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero).ToUnixTimeSeconds();

        static long Day(double utc, double offsetHours = 0) => LoginStreak.DayNumber(utc, offsetHours * 3600, DayStartHour);

        [Test]
        public void Day_TurnsAtFourInTheMorning_NotAtMidnight()
        {
            long evening = Day(Utc(2026, 10, 2, 22));
            Assert.That(Day(Utc(2026, 10, 3, 0, 30)), Is.EqualTo(evening), "Half past midnight still belongs to the evening before.");
            Assert.That(Day(Utc(2026, 10, 3, 3, 59)), Is.EqualTo(evening));
            Assert.That(Day(Utc(2026, 10, 3, 4, 0)), Is.EqualTo(evening + 1));
        }

        [Test]
        public void Day_FollowsTheLocalTimeZone()
        {
            // 02:00 UTC is 05:00 in UTC+3: already the next day there, still the day before in UTC.
            double moment = Utc(2026, 10, 3, 2);
            Assert.That(Day(moment, 3), Is.EqualTo(Day(moment) + 1));
            Assert.That(Day(moment, -5), Is.EqualTo(Day(moment)));
        }

        [Test]
        public void FirstDay_IsNotedOnce()
        {
            DailyStreak data = LoginStreak.NoteDay(default, 100);
            Assert.That(data.firstDay, Is.EqualTo(100));
            data = LoginStreak.NoteDay(data, 105);
            Assert.That(data.firstDay, Is.EqualTo(100));
            Assert.That(LoginStreak.NoteDay(default, 0).firstDay, Is.Zero, "An unknown day notes nothing.");
        }

        [Test]
        public void Streak_OpensOnTheSecondDay()
        {
            DailyStreak data = LoginStreak.NoteDay(default, 100);
            Assert.That(LoginStreak.IsUnlocked(data, 100, 1), Is.False);
            Assert.That(LoginStreak.IsUnlocked(data, 101, 1));
            Assert.That(LoginStreak.IsUnlocked(data, 140, 1));
            Assert.That(LoginStreak.IsUnlocked(default, 101, 1), Is.False, "Not before the first day is known.");
        }

        [Test]
        public void ClaimsOnBackToBackDays_WalkTheCycle_AndWrapAfterTheLastDay()
        {
            DailyStreak data = LoginStreak.NoteDay(default, 100);
            for (int i = 0; i < 9; i++)
            {
                long today = 101 + i;
                Assert.That(LoginStreak.CanClaim(data, today));
                Assert.That(LoginStreak.TodayIndex(data, today, Cycle), Is.EqualTo(i % Cycle));
                data = LoginStreak.Claim(data, today);
                Assert.That(data.streak, Is.EqualTo(i + 1));
                Assert.That(data.lastClaimDay, Is.EqualTo(today));
            }
            Assert.That(LoginStreak.CycleIndex(8, Cycle), Is.EqualTo(0), "Day 8 starts the cycle again.");
        }

        [Test]
        public void OneClaimADay()
        {
            DailyStreak data = LoginStreak.Claim(LoginStreak.NoteDay(default, 100), 101);
            Assert.That(LoginStreak.CanClaim(data, 101), Is.False);
            DailyStreak again = LoginStreak.Claim(data, 101);
            Assert.That(again.streak, Is.EqualTo(1));
            Assert.That(LoginStreak.TodayIndex(data, 101, Cycle), Is.EqualTo(0), "Today's tile is the one just claimed.");
        }

        [Test]
        public void MissedDay_StartsTheStreakOver()
        {
            DailyStreak data = LoginStreak.NoteDay(default, 100);
            data = LoginStreak.Claim(data, 101);
            data = LoginStreak.Claim(data, 102);
            data = LoginStreak.Claim(data, 103);
            Assert.That(data.streak, Is.EqualTo(3));

            Assert.That(LoginStreak.Missed(data, 104), Is.False);
            Assert.That(LoginStreak.Missed(data, 105));
            Assert.That(LoginStreak.StreakIfClaimed(data, 105), Is.EqualTo(1));
            Assert.That(LoginStreak.TodayIndex(data, 105, Cycle), Is.EqualTo(0));
            data = LoginStreak.Claim(data, 105);
            Assert.That(data.streak, Is.EqualTo(1));
        }

        [Test]
        public void ClockMovedBack_ClaimsNothing()
        {
            DailyStreak data = LoginStreak.Claim(LoginStreak.NoteDay(default, 100), 110);
            Assert.That(LoginStreak.CanClaim(data, 109), Is.False);
            Assert.That(LoginStreak.Claim(data, 109).lastClaimDay, Is.EqualTo(110));
            Assert.That(LoginStreak.CanClaim(data, 0), Is.False, "An unknown day claims nothing.");
        }
    }
}
