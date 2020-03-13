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
            [Tooltip("The visual effect DeltaTime relying on Update mode")]
            public float deltaTime;
            [Tooltip("The visual effect Delta Time before the play rate scale")]
            public float unscaledDeltaTime;
            [Tooltip("The visual effect time in second since component is enabled and visible")]
            public float totalTime;
            [Tooltip("Global visual effect manager frame index")]
            public uint frameIndex;
            [Tooltip("The multiplier applied to the delta time when it updates the VisualEffect")]
            public float playRate;
            [Tooltip("A VFXManager settings, the fixed interval in which the frame rate updates.")]
            public float fixedTimeStep;
            [Tooltip("A VFXManager settings, the maximum allowed delta time for an update interval.")]
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
