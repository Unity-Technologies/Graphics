using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionDecodeDepth : VFXExpression
    {
        public VFXExpressionDecodeDepth() : this(VFXValue<float>.Default)
        {
        }

        public VFXExpressionDecodeDepth(VFXExpression depth)
            : base(Flags.InvalidOnCPU, new VFXExpression[] { depth })
        {
        }

        public sealed override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        public sealed override VFXValueType valueType { get { return VFXValueType.Float; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("DecodeDepth({0})", parents[0]);
        }
    }
}
