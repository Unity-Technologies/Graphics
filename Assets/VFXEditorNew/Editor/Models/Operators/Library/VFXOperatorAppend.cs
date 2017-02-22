using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorAppendVector : VFXOperator
    {
        override public string name { get { return "AppendVector"; } }

        public class Properties
        {
            public float a = 0.0f;
            public float b = 0.0f;
        }



        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var a = inputExpression[0];
            var b = inputExpression[1];
            var allComponent = VFXOperatorUtility.ExtractComponents(a).Concat(VFXOperatorUtility.ExtractComponents(b)).ToArray();
            return new[] { new VFXExpressionCombine(allComponent) };
        }
    }
}