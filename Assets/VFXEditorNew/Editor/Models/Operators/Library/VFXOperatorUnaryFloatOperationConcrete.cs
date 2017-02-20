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
}