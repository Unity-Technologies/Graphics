#if PPV2_EXISTS
using System.Data;
using UnityEngine;
using UnityEngine.Rendering;
using BIRPRendering = UnityEngine.Rendering.PostProcessing;

namespace BIRPToURPConversionExtensions
{
    public static class PropertyConversions
    {
        public static void Convert(this BIRPRendering.FloatParameter birpSource, FloatParameter target, float scale = 1f, bool enabledState = true)
        {
            if (target == null) return;

            target.value = enabledState ? birpSource.value * scale : 0f;
            target.overrideState = birpSource.overrideState;
        }

        public static void Convert(this BIRPRendering.FloatParameter birpSource, MinFloatParameter target, float scale = 1f, bool enabledState = true)
        {
            if (target == null) return;

            target.value = enabledState ? birpSource.value * scale : 0f;
            target.overrideState = birpSource.overrideState;
        }

        public static void Convert(this BIRPRendering.FloatParameter birpSource, ClampedFloatParameter target, float scale = 1f, bool enabledState = true)
        {
            if (target == null) return;

            target.value = enabledState ? birpSource.value * scale : 0f;
            target.overrideState = birpSource.overrideState;
        }

        public static void Convert(this BIRPRendering.Vector2Parameter birpSource, Vector2Parameter target)
        {
            if (target == null) return;

            target.value = birpSource.value;
            target.overrideState = birpSource.overrideState;
        }

        public static void Convert(this BIRPRendering.Vector4Parameter birpSource, Vector4Parameter target, bool enabledState = true)
        {
            if (target == null) return;

            target.value = enabledState ? birpSource.value : new Vector4(1f, 1f, 1f, 0f);
            target.overrideState = birpSource.overrideState;
        }

        public static void Convert(this BIRPRendering.ColorParameter birpSource, ColorParameter target, bool enabledState, Color disabledColor)
        {
            if (target == null) return;

            target.value = enabledState ? birpSource.value : disabledColor;
            target.overrideState = birpSource.overrideState;
        }

        public static void Convert(this BIRPRendering.ColorParameter birpSource, ColorParameter target)
        {
            if (target == null) return;

            target.value = birpSource.value;
            target.overrideState = birpSource.overrideState;
        }

        public static void Convert(this BIRPRendering.TextureParameter birpSource, TextureParameter target)
        {
            if (target == null) return;

            target.value = birpSource.value;
            target.overrideState = birpSource.overrideState;
        }

        public static void Convert(this BIRPRendering.BoolParameter birpSource, BoolParameter target, bool invertValue = false)
        {
            if (target == null) return;

            target.value = invertValue ? !birpSource.value :  birpSource.value;
            target.overrideState = birpSource.overrideState;
        }

        public static void Convert(this BIRPRendering.SplineParameter birpSource, TextureCurveParameter target, bool enabledState = true)
        {
            if (target == null) return;

            target.value = new TextureCurve(birpSource.value.curve, zeroValue: 0f, loop: false, Vector2.up);

            target.overrideState = birpSource.overrideState;
        }
    }
}
#endif
