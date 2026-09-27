using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class HoneyFormulaTests
    {
        [Test]
        public void MultipliesAllFactors()
        {
            BigNumber honey = HoneyFormula.Honey(2, 1.5, 3, 2);
            Assert.That(honey.ToDouble(), Is.EqualTo(18).Within(1e-9));
        }

        [Test]
        public void HugeMultiplier_StaysBigNumber()
        {
            BigNumber honey = HoneyFormula.Honey(1, 1, BigNumber.Create(1, 400), 1);
            Assert.That(honey.Exponent, Is.EqualTo(400));
        }

        [TestCase(0.0, 1.0, 1.0)]
        [TestCase(-1.0, 1.0, 1.0)]
        [TestCase(1.0, 0.0, 1.0)]
        [TestCase(1.0, 1.0, 0.0)]
        public void NonPositiveInputs_GiveZero(double nectar, double flowerValue, double boost)
        {
            Assert.That(HoneyFormula.Honey(nectar, flowerValue, 1, boost).IsZero);
        }
    }
}
