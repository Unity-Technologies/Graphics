using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Vector")]
    class SampleBezier : VFXOperator
    {
        public class InputProperties
        {
            [Range(0.0f,1.0f), Tooltip("The progression to sample on the bezier [0..1]")]
            public float t;
            [Tooltip("The position of the start point")]
            public Position A = new Position() { position = new Vector3(0, 0, 0) };
            [Tooltip("The position of the first influence point")]
            public Position B = new Position() { position = new Vector3(0, 1, 0) };
            [Tooltip("The position of the second influence point")]
            public Position C = new Position() { position = new Vector3(1, 1, 0) };
            [Tooltip("The position of the end point")]
            public Position D = new Position() { position = new Vector3(1, 0, 0) };
        }

        public class OutputProperties
        {
            public Position Position;
            public Vector3 Tangent;
        }

        override public string name { get { return "Sample Bezier"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var t = VFXOperatorUtility.Saturate(inputExpression[0]);
            var posA = inputExpression[1];
            var posB = inputExpression[2];
            var posC = inputExpression[3];
            var posD = inputExpression[4];

            var vt = VFXOperatorUtility.CastFloat( t, VFXValueType.Float3);
            var vtc = VFXOperatorUtility.CastFloat(VFXOperatorUtility.OneExpression[1]-t, VFXValueType.Float3);
            var three = VFXOperatorUtility.CastFloat(VFXValue.Constant(3.0f), VFXValueType.Float3);
            var six = VFXOperatorUtility.CastFloat(VFXValue.Constant(6.0f), VFXValueType.Float3);

            // Position
            var A = posA * vtc * vtc * vtc;
            var B = three * posB * vtc * vtc * vt;
            var C = three * posC * vtc * vt * vt;
            var D = posD * vt * vt * vt;

            // Derivative
            var dA = three * vtc * vtc  * (posB - posA);
            var dB = six * vtc * vt * (posC - posB);
            var dC = three * vt * vt * (posD - posC);

            return new[] { A + B + C + D, dA + dB + dC };
        }
    }
}
