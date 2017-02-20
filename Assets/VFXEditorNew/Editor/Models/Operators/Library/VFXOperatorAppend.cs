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

        private static IEnumerable<VFXExpression> ExtractComponents(VFXExpression expression)
        {
            if (expression.ValueType == VFXValueType.kFloat)
            {
                return new[] { expression };
            }

            var components = new List<VFXExpression>();
            for (int i = 0; i < VFXExpression.TypeToSize(expression.ValueType); ++i)
            {
                components.Add(new VFXExpressionExtractComponent(expression, i));
            }
            return components;
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var a = inputExpression[0];
            var b = inputExpression[1];
            var allComponent = ExtractComponents(a).Concat(ExtractComponents(b)).ToArray();
            return new[] { new VFXExpressionCombine(allComponent) };
        }
    }
}