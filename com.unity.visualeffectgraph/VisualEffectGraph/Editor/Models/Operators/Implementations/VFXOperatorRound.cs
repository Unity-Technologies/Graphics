using System;
using UnityEditor.VFX;
namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorRound : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Round"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Round(inputExpression[0]) };
        }
    }
}
