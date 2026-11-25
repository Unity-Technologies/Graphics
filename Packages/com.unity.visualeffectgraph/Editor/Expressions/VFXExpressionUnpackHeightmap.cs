using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionUnpackHeightmap : VFXExpression
    {
        public VFXExpressionUnpackHeightmap() : this(VFXValue<Vector4>.Default)
        {
        }

        public VFXExpressionUnpackHeightmap(VFXExpression heightmapValues)
            : base(Flags.InvalidOnCPU, new VFXExpression[] { heightmapValues })
        {
        }

        public sealed override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        public sealed override VFXValueType valueType { get { return VFXValueType.Float; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("UnpackHeightmap({0})", parents[0]);
        }
    }
}
