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
