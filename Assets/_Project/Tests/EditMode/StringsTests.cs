using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Buzzfield.Ads;
using Buzzfield.Core;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Buzzfield.Tests.EditMode
{
    /// <summary>
    /// Every user-visible string in <see cref="Strings"/>: used somewhere, not empty, with
    /// placeholders numbered from {0}, and drawn by the text font (its atlas is fixed, so a
    /// character it lacks shows as a box).
    /// </summary>
    public class StringsTests
    {
        const string TextFontPath = "Assets/_Project/UI/Fonts/Fredoka SDF.asset";
        const string CatalogPath = "Assets/_Project/ScriptableObjects/Settings/StoreCatalog.asset";

        /// <summary>Prices as stores write them in the big markets, with their ISO codes.</summary>
        static readonly (string price, string code, decimal amount)[] StorePrices =
        {
            ("$0.99", "USD", 0.99m), ("US$0.99", "USD", 0.99m), ("₺29,99", "TRY", 29.99m), ("TRY 29,99", "TRY", 29.99m),
            ("1,99 €", "EUR", 1.99m), ("€1.99", "EUR", 1.99m), ("£0.99", "GBP", 0.99m), ("¥160", "JPY", 160m),
            ("₹89.00", "INR", 89m), ("₩1,200", "KRW", 1200m), ("R$ 5,90", "BRL", 5.9m), ("1 234,56 ₽", "RUB", 1234.56m),
            ("29,99 zł", "PLN", 29.99m), ("₱35.00", "PHP", 35m), ("฿35.00", "THB", 35m), ("Rp 15.000", "IDR", 15000m),
            ("‏3.99 US$", "USD", 3.99m), ("٣٫٩٩ ر.س.‏", "SAR", 3.99m),
            ("29,99 CHF", "CHF", 29.99m), ("₫25.000", "VND", 25000m), ("₦500", "NGN", 500m),
        };

        static IEnumerable<FieldInfo> Fields() =>
            typeof(Strings).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(string) || f.FieldType == typeof(string[]));

        static IEnumerable<(string name, string text)> Texts()
        {
            foreach (FieldInfo field in Fields())
            {
                if (field.GetValue(null) is string text)
                    yield return (field.Name, text);
                else if (field.GetValue(null) is string[] texts)
                {
                    for (int i = 0; i < texts.Length; i++)
                        yield return ($"{field.Name}[{i}]", texts[i]);
                }
            }
        }

        [Test]
        public void EveryString_IsUsedInCode()
        {
            string scripts = Path.Combine(Application.dataPath, "_Project", "Scripts");
            string code = string.Concat(Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories)
                .Where(file => Path.GetFileName(file) != "Strings.cs")
                .Select(File.ReadAllText));
            List<string> unused = Fields().Select(f => f.Name)
                .Where(name => !Regex.IsMatch(code, $@"\bStrings\.{name}\b"))
                .ToList();
            Assert.That(unused, Is.Empty, "strings no screen shows; use them or remove them");
        }

        [Test]
        public void EveryString_HasText()
        {
            // A separator may be a single space, but nothing is blank.
            List<string> empty = Texts().Where(t => string.IsNullOrEmpty(t.text)).Select(t => t.name).ToList();
            Assert.That(empty, Is.Empty);
        }

        [Test]
        public void Placeholders_AreNumberedFromZero()
        {
            var broken = new List<string>();
            foreach ((string name, string text) in Texts())
            {
                // Braces only as placeholders: an odd one breaks string.Format.
                string stripped = Regex.Replace(text, @"\{\d+(:[^}]*)?\}", "");
                if (stripped.Contains("{") || stripped.Contains("}"))
                    broken.Add($"{name}: stray brace in \"{text}\"");
                int[] numbers = Regex.Matches(text, @"\{(\d+)").Cast<Match>().Select(m => int.Parse(m.Groups[1].Value)).Distinct().OrderBy(n => n).ToArray();
                if (!numbers.SequenceEqual(Enumerable.Range(0, numbers.Length)))
                    broken.Add($"{name}: placeholders {string.Join(",", numbers)} in \"{text}\"");
            }
            Assert.That(broken, Is.Empty);
        }

        [Test]
        public void EveryString_IsDrawnByTheTextFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TextFontPath);
            Assert.That(font, Is.Not.Null, TextFontPath);
            var missing = new List<string>();
            foreach ((string name, string text) in Texts())
            {
                string drawn = text.Replace("\n", "");
                if (!font.HasCharacters(drawn, out uint[] lacking, false, false))
                    missing.Add($"{name}: {string.Join(" ", lacking.Select(c => $"U+{c:X4}"))}");
            }
            Assert.That(missing, Is.Empty, "characters the text font atlas does not have");
        }

        /// <summary>
        /// A store price, after <see cref="PriceLabel"/>, is drawn by the shop's font: a sign the
        /// atlas lacks would show as a box on the buy button.
        /// </summary>
        [Test]
        public void StorePrices_AreDrawnByTheTextFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TextFontPath);
            var catalog = AssetDatabase.LoadAssetAtPath<StoreCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, CatalogPath);
            Func<char, bool> canDraw = c => font.HasCharacter(c, true);
            var prices = StorePrices.Concat(catalog.Products.Select(p => (p.fallbackPrice, (string)null, 0m)));
            var missing = new List<string>();
            foreach ((string price, string code, decimal amount) in prices)
            {
                string label = PriceLabel.Drawable(price, code, amount, canDraw);
                if (string.IsNullOrEmpty(label) || !label.All(canDraw))
                    missing.Add($"{price} ({code}) shows as \"{label}\"");
            }
            Assert.That(missing, Is.Empty);
        }
    }
}
