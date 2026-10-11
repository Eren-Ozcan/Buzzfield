using System;
using System.IO;
using Buzzfield.Core;
using NUnit.Framework;
using UnityEngine;

namespace Buzzfield.Tests.EditMode
{
    public class SaveTests
    {
        string directory;
        SaveFileStore store;

        [SetUp]
        public void CreateFolder()
        {
            directory = Path.Combine(Path.GetTempPath(), "bz_save_tests_" + Guid.NewGuid().ToString("N"));
            store = new SaveFileStore(directory, "save.json");
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        static bool Accept(string text, out string read)
        {
            read = null;
            if (!SaveEnvelope.TryUnwrap(text, out string payload))
                return false;
            read = payload;
            return true;
        }

        SaveSource Read(out string payload)
        {
            string result = null;
            SaveSource source = store.Read(text => Accept(text, out result));
            payload = result;
            return source;
        }

        // ---- SaveEnvelope ----

        [Test]
        public void Envelope_RoundTrips()
        {
            string wrapped = SaveEnvelope.Wrap("{\"a\":1|2}");
            Assert.That(SaveEnvelope.TryUnwrap(wrapped, out string payload));
            Assert.That(payload, Is.EqualTo("{\"a\":1|2}"));
        }

        [TestCase("")]
        [TestCase("{\"a\":1}")]
        [TestCase("BZ1|abc")]
        [TestCase("XX1|abc|{}")]
        public void Envelope_RejectsMalformed(string text)
        {
            Assert.That(SaveEnvelope.TryUnwrap(text, out _), Is.False);
        }

        [Test]
        public void Envelope_RejectsChangedOrTruncatedPayload()
        {
            string wrapped = SaveEnvelope.Wrap("{\"honey\":100}");
            Assert.That(SaveEnvelope.TryUnwrap(wrapped.Replace("100", "900"), out _), Is.False);
            Assert.That(SaveEnvelope.TryUnwrap(wrapped.Substring(0, wrapped.Length - 3), out _), Is.False);
        }

        // ---- SaveFileStore ----

        [Test]
        public void Store_NoFiles_IsNone()
        {
            Assert.That(Read(out _), Is.EqualTo(SaveSource.None));
        }

        [Test]
        public void Store_FirstWrite_ReadsMainWithoutBackup()
        {
            store.Write(SaveEnvelope.Wrap("one"));
            Assert.That(Read(out string payload), Is.EqualTo(SaveSource.Main));
            Assert.That(payload, Is.EqualTo("one"));
            Assert.That(File.Exists(store.BackupPath), Is.False);
            Assert.That(File.Exists(store.TempPath), Is.False);
        }

        [Test]
        public void Store_SecondWrite_KeepsPreviousAsBackup()
        {
            store.Write(SaveEnvelope.Wrap("one"));
            store.Write(SaveEnvelope.Wrap("two"));
            Assert.That(Read(out string payload), Is.EqualTo(SaveSource.Main));
            Assert.That(payload, Is.EqualTo("two"));
            Assert.That(SaveEnvelope.TryUnwrap(File.ReadAllText(store.BackupPath), out string backup));
            Assert.That(backup, Is.EqualTo("one"));
        }

        [Test]
        public void Store_CorruptMain_FallsBackToBackup()
        {
            store.Write(SaveEnvelope.Wrap("one"));
            store.Write(SaveEnvelope.Wrap("two"));
            File.WriteAllText(store.MainPath, "garbage");
            Assert.That(Read(out string payload), Is.EqualTo(SaveSource.Backup));
            Assert.That(payload, Is.EqualTo("one"));
        }

        [Test]
        public void Store_MissingMain_FallsBackToBackup()
        {
            store.Write(SaveEnvelope.Wrap("one"));
            store.Write(SaveEnvelope.Wrap("two"));
            File.Delete(store.MainPath);
            Assert.That(Read(out string payload), Is.EqualTo(SaveSource.Backup));
            Assert.That(payload, Is.EqualTo("one"));
        }

        [Test]
        public void Store_BothCorrupt_IsUnreadable()
        {
            store.Write(SaveEnvelope.Wrap("one"));
            store.Write(SaveEnvelope.Wrap("two"));
            File.WriteAllText(store.MainPath, "");
            File.WriteAllText(store.BackupPath, "BZ1|x|y");
            Assert.That(Read(out string payload), Is.EqualTo(SaveSource.Unreadable));
            Assert.That(payload, Is.Null);
        }

        [Test]
        public void Store_LeftoverTempFile_DoesNotBreakWrites()
        {
            store.Write(SaveEnvelope.Wrap("one"));
            File.WriteAllText(store.TempPath, "half written");
            store.Write(SaveEnvelope.Wrap("two"));
            Assert.That(Read(out string payload), Is.EqualTo(SaveSource.Main));
            Assert.That(payload, Is.EqualTo("two"));
        }

        [Test]
        public void Store_Delete_RemovesAllFiles()
        {
            store.Write(SaveEnvelope.Wrap("one"));
            store.Write(SaveEnvelope.Wrap("two"));
            store.Delete();
            Assert.That(Read(out _), Is.EqualTo(SaveSource.None));
        }

        // ---- SaveData + JsonUtility ----

        [Test]
        public void SaveData_RoundTripsThroughJsonUtility()
        {
            var data = new SaveData
            {
                honey = BigNumberData.From(BigNumber.Create(1.25, 42)),
                royalJelly = BigNumberData.From(7),
                beesPerTier = new[] { 4, 2, 1 },
                slotBloom = new[] { 0.5f, 1f },
                slotActive = new[] { true, false },
                slotNectar = new[] { 3.5, 0 },
                gardenIndex = 2,
                clock = new ClockStamp { lastSeenUtc = 1_700_000_000.5, lastMonotonicSeconds = 123.25 },
            };
            data.stats.flowersBloomed = 9;

            SaveData read = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            Assert.That(read.honey.ToBigNumber(), Is.EqualTo(BigNumber.Create(1.25, 42)));
            Assert.That(read.royalJelly.ToBigNumber().ToDouble(), Is.EqualTo(7));
            Assert.That(read.beesPerTier, Is.EqualTo(new[] { 4, 2, 1 }));
            Assert.That(read.slotBloom, Is.EqualTo(new[] { 0.5f, 1f }));
            Assert.That(read.slotActive, Is.EqualTo(new[] { true, false }));
            Assert.That(read.gardenIndex, Is.EqualTo(2));
            Assert.That(read.clock.lastSeenUtc, Is.EqualTo(1_700_000_000.5));
            Assert.That(read.stats.flowersBloomed, Is.EqualTo(9));
            Assert.That(read.saveVersion, Is.EqualTo(SaveMigration.CurrentVersion));
        }

        [Test]
        public void BigNumberData_DamagedMantissa_ReadsAsZero()
        {
            Assert.That(new BigNumberData { m = double.NaN, e = 3 }.ToBigNumber().IsZero);
            Assert.That(new BigNumberData { m = double.PositiveInfinity }.ToBigNumber().IsZero);
            Assert.That(new BigNumberData { m = -5, e = 2 }.ToNonNegative().IsZero);
        }

        [Test]
        public void BigNumberData_UnnormalisedMantissa_IsNormalised()
        {
            BigNumber value = new BigNumberData { m = 250, e = 1 }.ToBigNumber();
            Assert.That(value.ToDouble(), Is.EqualTo(2500));
        }

        // ---- SaveMigration ----

        [Test]
        public void Migration_VersionZero_UpgradesAndFillsArrays()
        {
            SaveData data = JsonUtility.FromJson<SaveData>("{\"honey\":{\"m\":1.5,\"e\":3}}");
            Assert.That(data.saveVersion, Is.EqualTo(SaveMigration.CurrentVersion), "Field initialiser survives a missing field.");
            data.saveVersion = 0;
            data.beesPerTier = null;
            data.stats = null;

            Assert.That(SaveMigration.Upgrade(data, 0));
            Assert.That(data.saveVersion, Is.EqualTo(SaveMigration.CurrentVersion));
            Assert.That(data.beesPerTier, Is.Empty);
            Assert.That(data.slotBloom, Is.Not.Null);
            Assert.That(data.stats, Is.Not.Null);
            Assert.That(data.honey.ToBigNumber().ToDouble(), Is.EqualTo(1500));
        }

        [Test]
        public void Migration_VersionOne_AddsEmptyEntitlements()
        {
            // A version 1 save has no entitlements field.
            SaveData data = JsonUtility.FromJson<SaveData>("{\"saveVersion\":1,\"gardenIndex\":3}");
            data.entitlements = null;

            Assert.That(SaveMigration.Upgrade(data, 1));
            Assert.That(data.saveVersion, Is.EqualTo(SaveMigration.CurrentVersion));
            Assert.That(data.entitlements, Is.Not.Null);
            Assert.That(data.entitlements.removeAds || data.entitlements.permanentHoney2x || data.entitlements.starterPackBought, Is.False);
            Assert.That(data.gardenIndex, Is.EqualTo(3));
        }

        [Test]
        public void Migration_CurrentVersion_ChangesNothing()
        {
            var data = new SaveData();
            Assert.That(SaveMigration.Upgrade(data, SaveMigration.CurrentVersion), Is.False);
        }

        [Test]
        public void Migration_NewerVersion_IsLeftAlone()
        {
            var data = new SaveData { saveVersion = SaveMigration.CurrentVersion + 3 };
            Assert.That(SaveMigration.Upgrade(data, data.saveVersion), Is.False);
            Assert.That(data.saveVersion, Is.EqualTo(SaveMigration.CurrentVersion + 3));
            Assert.That(data.abilities, Is.Not.Null);
        }

        [Test]
        public void Migration_VersionTwo_DropsTheOldAbilityList()
        {
            // A version 2 save wrote the unused index-based list; the id-keyed field is missing.
            SaveData data = JsonUtility.FromJson<SaveData>("{\"saveVersion\":2,\"abilityLevels\":[],\"movesMade\":4}");
            data.abilities = null;

            Assert.That(SaveMigration.Upgrade(data, 2));
            Assert.That(data.saveVersion, Is.EqualTo(SaveMigration.CurrentVersion));
            Assert.That(data.abilities, Is.Empty);
            Assert.That(data.movesMade, Is.EqualTo(4));
        }

        [Test]
        public void Migration_VersionThree_StartsTheShakeCountAtZero()
        {
            SaveData data = JsonUtility.FromJson<SaveData>("{\"saveVersion\":3,\"stats\":{\"flowersBloomed\":12}}");

            Assert.That(SaveMigration.Upgrade(data, 3));
            Assert.That(data.saveVersion, Is.EqualTo(SaveMigration.CurrentVersion));
            Assert.That(data.stats.flowersShaken, Is.Zero);
            Assert.That(data.stats.flowersBloomed, Is.EqualTo(12));
        }

        [Test]
        public void Migration_VersionFour_TurnsSoundOnAndStartsTheGoals()
        {
            // A version 4 save has no settings object; whatever JsonUtility fills in, the step sets the defaults.
            SaveData data = JsonUtility.FromJson<SaveData>("{\"saveVersion\":4,\"settings\":{\"musicOn\":false,\"soundOn\":false,\"vibration\":0},\"goalIndex\":7}");

            Assert.That(SaveMigration.Upgrade(data, 4));
            Assert.That(data.saveVersion, Is.EqualTo(SaveMigration.CurrentVersion));
            Assert.That(data.settings.musicOn, Is.True);
            Assert.That(data.settings.soundOn, Is.True);
            Assert.That(data.settings.vibration, Is.EqualTo(VibrationMode.All));
            Assert.That(data.goalIndex, Is.Zero);
            Assert.That(data.welcomeGiftGiven, Is.False);
        }

        [Test]
        public void Settings_GoalsAndGift_RoundTripThroughJsonUtility()
        {
            var data = new SaveData
            {
                goalIndex = 6,
                welcomeGiftGiven = true,
                settings = new PlayerSettings { musicOn = false, soundOn = true, vibration = VibrationMode.Important },
            };
            SaveData read = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            Assert.That(read.goalIndex, Is.EqualTo(6));
            Assert.That(read.welcomeGiftGiven, Is.True);
            Assert.That(read.settings.musicOn, Is.False);
            Assert.That(read.settings.soundOn, Is.True);
            Assert.That(read.settings.vibration, Is.EqualTo(VibrationMode.Important));
        }

        [Test]
        public void Dewdrops_RoundTripThroughJsonUtility()
        {
            var data = new SaveData { dewdrops = 125, dewEarned = 400, goalIndex = 21, goalBase = 812 };
            SaveData read = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            Assert.That(read.dewdrops, Is.EqualTo(125));
            Assert.That(read.dewEarned, Is.EqualTo(400));
            Assert.That(read.goalIndex, Is.EqualTo(21));
            Assert.That(read.goalBase, Is.EqualTo(812));
        }

        [Test]
        public void Migration_VersionFive_StartsWithoutDewdrops()
        {
            SaveData data = JsonUtility.FromJson<SaveData>("{\"saveVersion\":5,\"goalIndex\":4}");
            Assert.That(SaveMigration.Upgrade(data, 5));
            Assert.That(data.saveVersion, Is.EqualTo(SaveMigration.CurrentVersion));
            Assert.That(data.dewdrops, Is.Zero);
            Assert.That(data.dewEarned, Is.Zero);
            Assert.That(data.goalBase, Is.Zero);
            Assert.That(data.goalIndex, Is.EqualTo(4), "Version 5 progress is kept.");
        }

        [Test]
        public void Migration_VersionSix_StartsWithoutBoostsOrOrders()
        {
            SaveData data = JsonUtility.FromJson<SaveData>("{\"saveVersion\":6,\"dewdrops\":90,\"entitlements\":{\"removeAds\":true}}");
            Assert.That(SaveMigration.Upgrade(data, 6));
            Assert.That(data.saveVersion, Is.EqualTo(SaveMigration.CurrentVersion));
            Assert.That(data.dewBoosts, Is.Empty);
            Assert.That(data.entitlements.grantedOrders, Is.Empty);
            Assert.That(data.entitlements.removeAds, "Version 6 purchases are kept.");
            Assert.That(data.dewdrops, Is.EqualTo(90));
        }

        [Test]
        public void DewBoostsAndOrders_RoundTripThroughJsonUtility()
        {
            var data = new SaveData
            {
                dewBoosts = new[] { new DewBoostEntry(3f, 600), new DewBoostEntry(2f, 3600) },
                dewBoostAnchorUtc = 1_700_000_000,
            };
            data.entitlements.RecordOrder("GPA.1");
            SaveData read = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            Assert.That(read.dewBoosts.Length, Is.EqualTo(2));
            Assert.That(read.dewBoosts[0].multiplier, Is.EqualTo(3f));
            Assert.That(read.dewBoosts[1].secondsLeft, Is.EqualTo(3600));
            Assert.That(read.dewBoostAnchorUtc, Is.EqualTo(1_700_000_000));
            Assert.That(read.entitlements.HasGrantedOrder("GPA.1"));
        }

        [Test]
        public void Migration_VersionSeven_StartsTheStreakAndPouchFresh()
        {
            SaveData data = JsonUtility.FromJson<SaveData>("{\"saveVersion\":7,\"dewdrops\":40,\"goalIndex\":9}");
            Assert.That(SaveMigration.Upgrade(data, 7));
            Assert.That(data.saveVersion, Is.EqualTo(SaveMigration.CurrentVersion));
            Assert.That(data.daily.firstDay, Is.Zero, "The first day is noted once the new build knows the date.");
            Assert.That(data.daily.lastClaimDay, Is.Zero);
            Assert.That(data.daily.streak, Is.Zero);
            Assert.That(data.pouch.today, Is.Zero);
            Assert.That(data.pouch.since, Is.Zero);
            Assert.That(data.dewdrops, Is.EqualTo(40), "Version 7 progress is kept.");
            Assert.That(data.goalIndex, Is.EqualTo(9));
        }

        [Test]
        public void Migration_VersionEight_StartsWithoutRareBees()
        {
            SaveData data = JsonUtility.FromJson<SaveData>("{\"saveVersion\":8,\"dewdrops\":75,\"movesMade\":2}");
            Assert.That(SaveMigration.Upgrade(data, 8));
            Assert.That(data.saveVersion, Is.EqualTo(SaveMigration.CurrentVersion));
            Assert.That(data.rareBees, Is.Not.Null.And.Empty, "Nobody had a rare bee before version 9.");
            Assert.That(data.dewdrops, Is.EqualTo(75), "Version 8 progress is kept.");
            Assert.That(data.movesMade, Is.EqualTo(2));
        }

        [Test]
        public void RareBees_RoundTripThroughJsonUtility()
        {
            var data = new SaveData
            {
                rareBees = new[] { new AbilityLevel { id = "Bumblebee", level = 4 }, new AbilityLevel { id = "CarpenterBee", level = 1 } },
            };
            SaveData read = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            Assert.That(read.rareBees.Length, Is.EqualTo(2));
            Assert.That(read.rareBees[0].id, Is.EqualTo("Bumblebee"));
            Assert.That(read.rareBees[0].level, Is.EqualTo(4));
            Assert.That(read.rareBees[1].id, Is.EqualTo("CarpenterBee"));
        }

        [Test]
        public void Normalize_FillsMissingRareBees()
        {
            var data = new SaveData { rareBees = null };
            data.Normalize();
            Assert.That(data.rareBees, Is.Not.Null);
        }

        [Test]
        public void StreakAndPouch_RoundTripThroughJsonUtility()
        {
            var data = new SaveData
            {
                daily = new DailyStreak { firstDay = 20_360, lastClaimDay = 20_363, streak = 4 },
                pouch = new DewPouch { day = 20_363, today = 2, since = 3 },
            };
            SaveData read = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            Assert.That(read.daily.firstDay, Is.EqualTo(20_360));
            Assert.That(read.daily.lastClaimDay, Is.EqualTo(20_363));
            Assert.That(read.daily.streak, Is.EqualTo(4));
            Assert.That(read.pouch.day, Is.EqualTo(20_363));
            Assert.That(read.pouch.today, Is.EqualTo(2));
            Assert.That(read.pouch.since, Is.EqualTo(3));
        }

        [Test]
        public void Normalize_KeepsStreakAndPouchCountsSane()
        {
            var data = new SaveData
            {
                daily = new DailyStreak { streak = -2 },
                pouch = new DewPouch { today = -1, since = -7 },
            };
            data.Normalize();
            Assert.That(data.daily.streak, Is.Zero);
            Assert.That(data.pouch.today, Is.Zero);
            Assert.That(data.pouch.since, Is.Zero);
        }

        [Test]
        public void Normalize_FillsMissingBoostsAndOrders()
        {
            var data = new SaveData { dewBoosts = null };
            data.entitlements.grantedOrders = null;
            data.Normalize();
            Assert.That(data.dewBoosts, Is.Not.Null);
            Assert.That(data.entitlements.grantedOrders, Is.Not.Null);
        }

        [Test]
        public void Normalize_KeepsDewdropsSane()
        {
            var data = new SaveData { dewdrops = -5, dewEarned = 2, goalBase = -1 };
            data.Normalize();
            Assert.That(data.dewdrops, Is.Zero);
            Assert.That(data.goalBase, Is.Zero);
            data = new SaveData { dewdrops = 50, dewEarned = 10 };
            data.Normalize();
            Assert.That(data.dewEarned, Is.EqualTo(50), "Nobody holds more than they ever earned.");
        }

        [Test]
        public void Normalize_FillsMissingSettings_AndClampsTheGoalIndex()
        {
            var data = new SaveData { settings = null, goalIndex = -3 };
            data.Normalize();
            Assert.That(data.settings, Is.Not.Null);
            Assert.That(data.settings.soundOn, Is.True);
            Assert.That(data.goalIndex, Is.Zero);
        }

        [Test]
        public void AbilityLevels_RoundTripThroughJsonUtility()
        {
            var data = new SaveData
            {
                abilities = new[] { new AbilityLevel { id = "RoyalWings", level = 3 }, new AbilityLevel { id = "PollenTouch", level = 1 } },
            };
            SaveData read = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            Assert.That(read.abilities.Length, Is.EqualTo(2));
            Assert.That(read.abilities[0].id, Is.EqualTo("RoyalWings"));
            Assert.That(read.abilities[0].level, Is.EqualTo(3));
            Assert.That(read.abilities[1].id, Is.EqualTo("PollenTouch"));
        }
    }
}
