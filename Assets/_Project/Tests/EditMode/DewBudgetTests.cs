using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Buzzfield.Tests.EditMode
{
    /// <summary>
    /// The daily Dewdrops budget of a player who never pays (docs/DEWDROPS.md sections 2 and 3),
    /// read from the real settings assets: about half an hour of play a day, every butterfly and
    /// pouch caught. Logs the table and holds the binding rules: about 55 a day, rewarded ads at
    /// most 35% of it, no Boosts tab offer more than 12 days away.
    /// </summary>
    public class DewBudgetTests
    {
        const string Data = "Assets/_Project/ScriptableObjects";
        const double ActiveSecondsPerDay = 30 * 60;
        /// <summary>Active play per garden, measured by the balance bot.</summary>
        const double SecondsPerGarden = 18 * 60;
        /// <summary>Goals after the front-loaded chain: about one endless goal a day.</summary>
        const double GoalDewPerDay = 10;

        static SerializedObject Load(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>($"{Data}/{path}.asset");
            Assert.That(asset, Is.Not.Null, path);
            return new SerializedObject(asset);
        }

        [Test]
        public void FreePlayerBudget_StaysInTheDesignedShape()
        {
            SerializedObject dew = Load("Settings/DewSettings");
            SerializedObject visitor = Load("Flowers/VisitorSettings");

            SerializedProperty rewards = dew.FindProperty("dailyRewards");
            Assert.That(rewards.arraySize, Is.EqualTo(7));
            double streakSum = 0;
            for (int i = 0; i < rewards.arraySize; i++)
                streakSum += rewards.GetArrayElementAtIndex(i).intValue;
            double streak = streakSum / rewards.arraySize;

            double gardens = dew.FindProperty("gardenCompleteReward").intValue * ActiveSecondsPerDay / SecondsPerGarden;

            double butterflySeconds = (visitor.FindProperty("minIntervalSeconds").floatValue + visitor.FindProperty("maxIntervalSeconds").floatValue) / 2
                + visitor.FindProperty("crossSeconds").floatValue;
            double pouches = System.Math.Min(visitor.FindProperty("pouchesPerDay").intValue,
                ActiveSecondsPerDay / butterflySeconds / visitor.FindProperty("pouchEvery").intValue);
            double pouchDew = pouches * (visitor.FindProperty("pouchMinDew").intValue + visitor.FindProperty("pouchMaxDew").intValue) / 2.0;

            double total = streak + gardens + GoalDewPerDay + pouchDew;
            double rewardedShare = pouchDew / total;

            int dearest = 0;
            SerializedProperty offers = dew.FindProperty("offers");
            for (int i = 0; i < offers.arraySize; i++)
                dearest = Mathf.Max(dearest, offers.GetArrayElementAtIndex(i).FindPropertyRelative("cost").intValue);

            TestContext.WriteLine("Source               Dewdrops/day");
            TestContext.WriteLine($"Daily streak         {streak,6:0.0}");
            TestContext.WriteLine($"Finished gardens     {gardens,6:0.0}");
            TestContext.WriteLine($"Goals                {GoalDewPerDay,6:0.0}");
            TestContext.WriteLine($"Butterfly pouches    {pouchDew,6:0.0}  ({pouches:0.0} pouches, rewarded)");
            TestContext.WriteLine($"Total                {total,6:0.0}  rewarded share {rewardedShare:P0}");
            TestContext.WriteLine($"Dearest Boosts offer {dearest} = {dearest / total:0.0} days");

            Assert.That(streak, Is.EqualTo(20).Within(0.01), "10, 10, 15, 15, 20, 20, 50 is 20 a day.");
            Assert.That(total, Is.InRange(45.0, 65.0), "About 55 Dewdrops a day.");
            Assert.That(rewardedShare, Is.LessThanOrEqualTo(0.35), "Rewarded ads stay under 35% of free Dewdrops.");
            Assert.That(dearest / total, Is.LessThanOrEqualTo(12), "No spend takes more than 12 days to save for.");
        }
    }
}
