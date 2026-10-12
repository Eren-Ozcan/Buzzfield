using System;
using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class PriceLabelTests
    {
        /// <summary>A font like the game's: printable ASCII only.</summary>
        static readonly Func<char, bool> Ascii = c => c >= 32 && c <= 126;

        /// <summary>A font that also has the common European signs.</summary>
        static readonly Func<char, bool> WithEuro = c => Ascii(c) || c == '€' || c == '£';

        [TestCase("$0.99", "USD", "$0.99")]
        [TestCase("US$0.99", "USD", "US$0.99")]
        [TestCase("R$ 5,90", "BRL", "R$ 5,90")]
        [TestCase("CHF 1.00", "CHF", "CHF 1.00")]
        public void DrawablePrice_StaysAsTheStoreWroteIt(string price, string code, string expected) =>
            Assert.That(PriceLabel.Drawable(price, code, 1m, Ascii), Is.EqualTo(expected));

        [TestCase("₺29,99", "TRY", "29,99 TRY")]
        [TestCase("29,99 ₺", "TRY", "29,99 TRY")]
        [TestCase("₹89.00", "INR", "89.00 INR")]
        [TestCase("1 234,56 ₽", "RUB", "1 234,56 RUB")]
        [TestCase("29,99 zł", "PLN", "29,99 PLN")]
        [TestCase("¥160", "JPY", "160 JPY")]
        [TestCase("₩1,200", "KRW", "1,200 KRW")]
        public void MissingSign_GivesWayToTheCurrencyCode(string price, string code, string expected) =>
            Assert.That(PriceLabel.Drawable(price, code, 1m, Ascii), Is.EqualTo(expected));

        [Test]
        public void SignTheFontHas_IsKept_AndItsSpaceTurnsPlain()
        {
            Assert.That(PriceLabel.Drawable("1,99 €", "EUR", 1.99m, WithEuro), Is.EqualTo("1,99 €"));
            Assert.That(PriceLabel.Drawable("1,99 €", "EUR", 1.99m, WithEuro), Is.EqualTo("1,99 €"));
            Assert.That(PriceLabel.Drawable("£0.99", "GBP", 0.99m, WithEuro), Is.EqualTo("£0.99"));
        }

        [Test]
        public void DirectionMarks_AreDropped()
        {
            Assert.That(PriceLabel.Drawable("‏3.99 US$", "USD", 3.99m, Ascii), Is.EqualTo("3.99 US$"));
        }

        [Test]
        public void DigitsOfAnotherScript_UseTheAmount()
        {
            // Arabic-Indic digits and the Saudi riyal sign: neither is in the font.
            Assert.That(PriceLabel.Drawable("٣٫٩٩ ر.س.‏", "SAR", 3.99m, Ascii), Is.EqualTo("3.99 SAR"));
            Assert.That(PriceLabel.Drawable("١٦٠ ₹", "INR", 160m, Ascii), Is.EqualTo("160 INR"));
            Assert.That(PriceLabel.Drawable("١٬٢٣٤ ₹", "INR", 1234.5m, Ascii), Is.EqualTo("1,234.50 INR"));
        }

        [Test]
        public void NoCurrencyCode_KeepsTheNumber()
        {
            Assert.That(PriceLabel.Drawable("₺29,99", null, 29.99m, Ascii), Is.EqualTo("29,99"));
            Assert.That(PriceLabel.Drawable("₺29,99", "", 29.99m, Ascii), Is.EqualTo("29,99"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void NoPrice_StaysEmpty(string price) =>
            Assert.That(PriceLabel.Drawable(price, "TRY", 0m, Ascii), Is.EqualTo(price));
    }
}
