using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleDeformedMeshData : VFXExpression
    {
        public VFXExpressionSampleDeformedMeshData() : this(VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleDeformedMeshData(VFXExpression computeMeshIndex, VFXExpression vertexID)
            : base(VFXExpression.Flags.InvalidOnCPU, new VFXExpression[2] {computeMeshIndex, vertexID })
        {}

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }

        public sealed override string GetCodeString(string[] parents)
        {
#if ENABLE_HYBRID_RENDERER_V2 && ENABLE_COMPUTE_DEFORMATIONS
            return string.Format("_DeformedMeshData[asuint({0}) + asuint({1})].Position", parents[0], parents[1]);
#else
            return string.Format("float3(0.f, 0.f, 0.f)");
#endif
        }
    }
}
