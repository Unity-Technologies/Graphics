using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorAdd : VFXOperatorBinaryFloatOperationZero
    {
        override public string name { get { return "Add"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionAdd(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorSubtract : VFXOperatorBinaryFloatOperationZero
    {
        override public string name { get { return "Subtract"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSubtract(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo]
    class VFXOperatorMul : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Mul"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMul(inputExpression[0], inputExpression[1]) };
        }
    }
}