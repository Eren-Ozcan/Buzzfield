using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class NumberFormatTests
    {
        [TestCase(0.0, "0")]
        [TestCase(7.0, "7")]
        [TestCase(7.9, "7")]
        [TestCase(999.0, "999")]
        [TestCase(999.99, "999")]
        [TestCase(1000.0, "1K")]
        [TestCase(1200.0, "1.2K")]
        [TestCase(1234.0, "1.23K")]
        [TestCase(1999.0, "1.99K")]
        [TestCase(12345.0, "12.3K")]
        [TestCase(999999.0, "999K")]
        [TestCase(3.4e6, "3.4M")]
        [TestCase(5.6e9, "5.6B")]
        [TestCase(7.8e12, "7.8T")]
        [TestCase(1e15, "1aa")]
        [TestCase(2.5e18, "2.5ab")]
        [TestCase(1e90, "1az")]
        [TestCase(1e93, "1ba")]
        public void Abbreviate(double value, string expected)
        {
            Assert.That(NumberFormat.Abbreviate(value), Is.EqualTo(expected));
        }

        [Test]
        public void Abbreviate_LastSuffixIsZz()
        {
            BigNumber last = BigNumber.Create(1, 3L * NumberFormat.MaxTier);
            Assert.That(NumberFormat.Abbreviate(last), Is.EqualTo("1zz"));
        }

        [Test]
        public void Abbreviate_BeyondLastSuffix_KeepsWholeDigits()
        {
            BigNumber beyond = BigNumber.Create(1, 3L * NumberFormat.MaxTier + 4);
            Assert.That(NumberFormat.Abbreviate(beyond), Is.EqualTo("10000zz"));
        }

        [Test]
        public void Abbreviate_Negative()
        {
            Assert.That(NumberFormat.Abbreviate(-1500.0), Is.EqualTo("-1.5K"));
        }

        [TestCase(0, "")]
        [TestCase(1, "K")]
        [TestCase(4, "T")]
        [TestCase(5, "aa")]
        [TestCase(6, "ab")]
        [TestCase(30, "az")]
        [TestCase(31, "ba")]
        public void Suffix(int tier, string expected)
        {
            Assert.That(NumberFormat.Suffix(tier), Is.EqualTo(expected));
        }

        [TestCase(0.0, "0/s")]
        [TestCase(0.45, "0.4/s")]
        [TestCase(2.5, "2.5/s")]
        [TestCase(9.99, "9.9/s")]
        [TestCase(12.7, "12/s")]
        [TestCase(1500.0, "1.5K/s")]
        public void PerSecond(double value, string expected)
        {
            Assert.That(NumberFormat.PerSecond(value), Is.EqualTo(expected));
        }
    }
}
