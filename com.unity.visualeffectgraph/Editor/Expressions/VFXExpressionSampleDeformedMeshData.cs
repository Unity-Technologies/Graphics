using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleDeformedMeshPosition : VFXExpression
    {
        public VFXExpressionSampleDeformedMeshPosition() : this(VFXValue<Vector4>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleDeformedMeshPosition(VFXExpression deformationProperty, VFXExpression vertexID)
            : base(VFXExpression.Flags.InvalidOnCPU, new VFXExpression[2] {deformationProperty, vertexID })
        {}

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }

        public sealed override string GetCodeString(string[] parents)
        {
#if ENABLE_HYBRID_RENDERER_V2 && ENABLE_COMPUTE_DEFORMATIONS
            return string.Format("SampleDOTSDeformationMeshPosition(asint({0}), asuint({1}))", parents[0], parents[1]);
#else
            return string.Format("float3(0.f, 0.f, 0.f)");
#endif
        }
    }

    class VFXExpressionSampleDeformedMeshNormal : VFXExpression
    {
        public VFXExpressionSampleDeformedMeshNormal() : this(VFXValue<Vector4>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleDeformedMeshNormal(VFXExpression deformationParams, VFXExpression vertexID)
            : base(VFXExpression.Flags.InvalidOnCPU, new VFXExpression[2] {deformationParams, vertexID })
        {}

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }

        public sealed override string GetCodeString(string[] parents)
        {
#if ENABLE_HYBRID_RENDERER_V2 && ENABLE_COMPUTE_DEFORMATIONS
            return string.Format("SampleDOTSDeformationMeshNormal(asint({0}), asuint({1}))", parents[0], parents[1]);
#else
            return string.Format("float3(0.f, 0.f, 0.f)");
#endif
        }
    }

    class VFXExpressionSampleDeformedMeshTangent : VFXExpression
    {
        public VFXExpressionSampleDeformedMeshTangent() : this(VFXValue<Vector4>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionSampleDeformedMeshTangent(VFXExpression deformationParams, VFXExpression vertexID)
            : base(VFXExpression.Flags.InvalidOnCPU, new VFXExpression[2] {deformationParams, vertexID })
        {}

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }

        public sealed override string GetCodeString(string[] parents)
        {
#if ENABLE_HYBRID_RENDERER_V2 && ENABLE_COMPUTE_DEFORMATIONS
            return string.Format("SampleDOTSDeformationMeshTangent(asint({0}), asuint({1}))", parents[0], parents[1]);
#else
            return string.Format("float3(0.f, 0.f, 0.f)");
#endif
        }
    }
}
