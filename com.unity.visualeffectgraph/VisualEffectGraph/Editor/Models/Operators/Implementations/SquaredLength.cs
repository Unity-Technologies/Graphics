using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.VFX;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class SquaredLength : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            [Tooltip("The vector to be used in the length calculation.")]
            public Vector3 x;
        }

        protected override sealed Type GetExpectedOutputTypeOfOperation(IEnumerable<Type> inputTypes)
        {
            var type = inputTypes.First(); //derive from VFXOperatorNumericUniformNew, First is suitable
            return VFXExpression.GetMatchingScalar(type);
        }

        protected sealed override string expectedOutputName
        {
            get
            {
                return "l";
            }
        }

        protected override sealed VFXPropertyAttribute[] expectedOutputAttributes
        {
            get
            {
                return VFXPropertyAttribute.Create(new TooltipAttribute("The squared length of x."));
            }
        }

        public sealed override string name
        {
            get
            {
                return "Squared Length";
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Dot(inputExpression[0], inputExpression[0]) };
        }
    }
}
