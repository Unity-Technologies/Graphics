using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorSin : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Sin"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSin(inputExpression[0]) };
        }
    }

    [VFXInfo]
    class VFXOperatorCos : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Cos"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionCos(inputExpression[0]) };
        }
    }

    [VFXInfo]
    class VFXOperatorOneMinus : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "OneMinus"; } }

        private static readonly Dictionary<int, VFXExpression> s_constOneExpression = new Dictionary<int, VFXExpression>
        {
            { 1, new VFXValueFloat(1.0f, true) },
            { 2, new VFXValueFloat2(Vector2.one, true) },
            { 3, new VFXValueFloat3(Vector3.one, true) },
            { 4, new VFXValueFloat4(Vector4.one, true) },
        };

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var input = inputExpression[0];
            var one = s_constOneExpression[VFXExpression.TypeToSize(input.ValueType)];
            return new[] { new VFXExpressionSubtract(one, input) };
        }

    }

}