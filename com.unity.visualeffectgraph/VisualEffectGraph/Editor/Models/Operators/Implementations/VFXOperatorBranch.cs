using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Flow")]
    class VFXOperatorBranch : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The predicate")]
            public bool predicate = true;
            [Tooltip("The true branch")]
            public FloatN True = 0.0f;
            [Tooltip("The false branch")]
            public FloatN False = 0.0f;
        }

        override public string name { get { return "Branch"; } }

        sealed override protected IEnumerable<VFXExpression> GetInputExpressions()
        {
            var inputExpression = base.GetInputExpressions();

            yield return inputExpression.First();
            var branches = VFXOperatorUtility.UnifyFloatLevel(inputExpression.Skip(1));
            foreach (var b in branches)
                yield return b;
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBranch(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
