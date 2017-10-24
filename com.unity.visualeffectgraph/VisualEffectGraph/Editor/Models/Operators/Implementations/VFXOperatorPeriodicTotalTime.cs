using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Time")]
    class VFXOperatorPeriodicTotalTime : VFXOperator
    {
        public class InputProperties
        {
            [Min(0.001f)]
            public float Period = 5.0f;
        }
        public class OutputProperties
        {
            public float Absolute;
            public float Relative;
        }
        public override string name
        {
            get
            {
                return "Periodic Total Time";
            }
        }
        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] output = new VFXExpression[] {
                VFXOperatorUtility.Fmod(VFXBuiltInExpression.TotalTime, inputExpression[0]),
                VFXOperatorUtility.Fmod(VFXBuiltInExpression.TotalTime, inputExpression[0]) / inputExpression[0],
            };
            return output;
        }
    }
}
