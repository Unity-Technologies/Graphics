using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "BuiltIn")]
    class VFXTime : VFXOperator
    {
        public override string libraryName => "VFX Time";
        public override string name => libraryName;

        public class OutputProperties
        {
            public float deltaTime;
            public float unscaledDeltaTime;
            public float totalTime;
            public uint frameIndex;
            public float playRate;
            public float fixedTimeStep;
            public float maxDeltaTime;
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[]
            {
                VFXBuiltInExpression.DeltaTime,
                VFXBuiltInExpression.UnscaledDeltaTime,
                VFXBuiltInExpression.TotalTime,
                VFXBuiltInExpression.FrameIndex,
                VFXBuiltInExpression.PlayRate,
                VFXBuiltInExpression.ManagerFixedTimeStep,
                VFXBuiltInExpression.ManagerMaxDeltaTime,
            };
        }
    }
}
