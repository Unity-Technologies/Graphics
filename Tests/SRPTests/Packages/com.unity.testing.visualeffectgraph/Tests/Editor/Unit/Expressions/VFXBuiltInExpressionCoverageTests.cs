using NUnit.Framework;
using System;
using System.Linq;

namespace UnityEditor.VFX.Test.Unit.Expressions
{
    [TestFixture]
    class VFXBuiltInExpressionCoverageTests
    {
        static readonly string[] CheckAllBuiltinExpressionAreListed_ValueSource = VFXBuiltInExpression.All.Select(o => o.ToString()).ToArray();

        [Test]
        public void CheckBuiltinExpressionListed([ValueSource("CheckAllBuiltinExpressionAreListed_ValueSource")] string expressionName)
        {
            var operation = (UnityEngine.VFX.VFXExpressionOperation)Enum.Parse(typeof(UnityEngine.VFX.VFXExpressionOperation), expressionName);
            var referenceExpression = VFXBuiltInExpression.Find(operation);
            Assert.IsTrue(VFXDynamicBuiltInParameter.s_BuiltInInfo.Values.Any(o => o.expression == referenceExpression));
        }
    }
}
