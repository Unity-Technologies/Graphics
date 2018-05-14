using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    class BranchDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
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

        public sealed override string name { get { return "Branch (deprecated) "; } }

        protected sealed override IEnumerable<VFXExpression> ApplyPatchInputExpression(IEnumerable<VFXExpression> inputExpression)
        {
            yield return inputExpression.First();
            var branches = VFXOperatorUtility.UpcastAllFloatN(inputExpression.Skip(1));
            foreach (var b in branches)
                yield return b;
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBranch(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Branch));
        }
    }
}
