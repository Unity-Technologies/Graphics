using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXOutputUpdate : VFXContext
    {
        [Flags]
        public enum Features
        {
            None = 0,
            MotionVector = 1 << 0,
            IndirectDraw = 1 << 1,
            Culling = 1 << 2 | IndirectDraw,
            MultiMesh = 1 << 3 | Culling,
            LOD = 1 << 4 | Culling,
            Sort = 1 << 5 | IndirectDraw,
            FrustumCulling = 1 << 6 | IndirectDraw,
        }

        public VFXOutputUpdate() : base(VFXContextType.Filter, VFXDataType.Particle, VFXDataType.Particle) { }
        public override string name => "OutputUpdate";

        private VFXAbstractParticleOutput m_Output;
        public VFXAbstractParticleOutput output => m_Output;
        public override VFXDataType ownedType => output != null ? output.ownedType : base.ownedType;

        public void SetOutput(VFXAbstractParticleOutput output)
        {
            if (m_Output != null)
                throw new InvalidOperationException("Unexpected SetOutput called twice, supposed to be call only once after construction");

            features = output.outputUpdateFeatures;

            if (features == VFXOutputUpdate.Features.None)
                throw new ArgumentException("This output does not need an output update pass");

            m_Output = output;
        }

        private Features features = Features.None;

        public static bool HasFeature(Features flags, Features feature)
        {
            return (flags & feature) == feature;
        }

        public static bool IsPerCamera(Features flags)
        {
            return HasFeature(flags, Features.MotionVector)
                || HasFeature(flags, Features.LOD)
                || HasFeature(flags, Features.Sort)
                || HasFeature(flags, Features.FrustumCulling);
        }

        public bool HasFeature(Features feature)
        {
            return HasFeature(this.features, feature);
        }

        public bool isPerCamera => IsPerCamera(features);

        // Set by compiler
        public int bufferIndex = -1;
        public int sortedBufferIndex = -1;
        public uint bufferCount
        {
            get
            {
                if (HasFeature(Features.MultiMesh))
                    return ((IVFXMultiMeshOutput)m_Output).meshCount;
                if (HasFeature(Features.IndirectDraw))
                    return 1;
                return 0;
            }
        }

        public override string codeGeneratorTemplate
        {
            get
            {
                return VisualEffectGraphPackageInfo.assetPackagePath + "/Shaders/VFXOutputUpdate";
            }
        }
        public override bool codeGeneratorCompute { get { return true; } }
        public override bool doesIncludeCommonCompute { get { return false; } }

        public override VFXTaskType taskType { get { return isPerCamera ? VFXTaskType.PerCameraUpdate : VFXTaskType.Update; } }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (!m_Output)
                throw new NullReferenceException("Unexpected call of GetExpressionMapper with a null output");
            if (features == Features.None)
                throw new InvalidOperationException("This additional update context has no feature set");


            if (target == VFXDeviceTarget.GPU)
            {
                var expressionMapper = m_Output.GetExpressionMapper(target);

                var exp = GetExpressionsFromSlots(m_Output);

                if (HasFeature(Features.LOD))
                {
                    var lodExp = exp.FirstOrDefault(e => e.name == VFXMultiMeshHelper.lodName);
                    var ratioExp = lodExp.exp * VFXValue.Constant(new Vector4(0.01f, 0.01f, 0.01f, 0.01f));
                    expressionMapper.AddExpression(ratioExp, VFXMultiMeshHelper.lodName, -1);
                }

                if (HasFeature(Features.LOD) || HasFeature(Features.FrustumCulling))
                {
                    var radiusScaleExp = exp.FirstOrDefault(e => e.name == "radiusScale").exp;
                    if (radiusScaleExp == null) // Not found, assume it's 1
                        radiusScaleExp = VFXValue.Constant(1.0f);
                    expressionMapper.AddExpression(radiusScaleExp, "radiusScale", -1);
                }

                if (HasFeature(Features.MotionVector))
                {
                    var currentFrameIndex = expressionMapper.FromNameAndId("currentFrameIndex", -1);
                    if (currentFrameIndex == null)
                        Debug.LogError("CurrentFrameIndex isn't reachable in encapsulatedOutput for motionVector");
                }

                //Since it's a compute shader without renderer associated, these entries aren't automatically sent
                expressionMapper.AddExpression(VFXBuiltInExpression.LocalToWorld, "unity_ObjectToWorld", -1);
                expressionMapper.AddExpression(VFXBuiltInExpression.WorldToLocal, "unity_WorldToObject", -1);

                return expressionMapper;
            }
            else
                return new VFXExpressionMapper();
        }

        protected override IEnumerable<VFXBlock> implicitPostBlock
        {
            get
            {
                foreach (var inBase in base.implicitPostBlock)
                    yield return inBase;

                if (m_Output != null)
                {
                    foreach (var block in m_Output.activeChildrenWithImplicit)
                    {
                        yield return block;
                    }
                }
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);

                if (HasFeature(Features.MultiMesh))
                    yield return new VFXAttributeInfo(VFXAttribute.MeshIndex, VFXAttributeMode.Read);

                if (HasFeature(Features.MotionVector) && output is VFXLineOutput)
                    yield return new VFXAttributeInfo(VFXAttribute.TargetPosition, VFXAttributeMode.Read);
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                yield return "INDIRECT_BUFFER_COUNT " + bufferCount;

                if (HasFeature(Features.MotionVector))
                {
                    yield return "VFX_FEATURE_MOTION_VECTORS";
                    if (output.SupportsMotionVectorPerVertex(out uint vertsCount))
                        yield return "VFX_FEATURE_MOTION_VECTORS_VERTS " + vertsCount;
                }
                if (HasFeature(Features.LOD))
                    yield return "VFX_FEATURE_LOD";
                if (HasFeature(Features.Sort))
                    yield return "VFX_FEATURE_SORT";
                if (HasFeature(Features.FrustumCulling))
                    yield return "VFX_FEATURE_FRUSTUM_CULL";
                if (output.HasStrips(false))
                    yield return "HAS_STRIPS";
            }
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                if (HasFeature(Features.MotionVector))
                {
                    string motionVectorVerts = null;

                    if (output.HasStrips(false))
                    {
                        switch (output.taskType)
                        {
                            case VFXTaskType.ParticleQuadOutput:
                                motionVectorVerts = @"float3 verts[] =
{
    mul(elementToVFX, float4(0.0f, -0.5f, 0.0f, 1.0f)).xyz,
    mul(elementToVFX, float4(0.0f,  0.5f, 0.0f, 1.0f)).xyz
};";
                                break;
                            case VFXTaskType.ParticleLineOutput:
                                motionVectorVerts = @"float3 verts[] =
{
    attributes.position
};";
                                break;
                        }
                    }
                    else if (output is VFXLineOutput)
                    {
                        bool useTargetOffset = (bool)output.GetSettingValue("useTargetOffset");
                        string targetPosition = useTargetOffset ? "mul(elementToVFX, float4(targetOffset, 1)).xyz" : "attributes.targetPosition";
                        switch (output.taskType)
                        {
                            case VFXTaskType.ParticleQuadOutput:
                                motionVectorVerts = @"float3 verts[] =
{
    attributes.position,
    attributes.position,
    " + targetPosition + @",
    " + targetPosition + @"
};";
                                break;
                            case VFXTaskType.ParticleLineOutput:
                                motionVectorVerts = @"float3 verts[] =
{
    attributes.position,
    " + targetPosition + @"
};";
                                break;
                        }
                    }
                    else
                    {
                        switch (output.taskType)
                        {
                            case VFXTaskType.ParticleQuadOutput:
                                motionVectorVerts = @"float3 verts[] =
{
    mul(elementToVFX, float4(-0.5f, -0.5f, 0.0f, 1.0f)).xyz,
    mul(elementToVFX, float4( 0.5f, -0.5f, 0.0f, 1.0f)).xyz,
    mul(elementToVFX, float4(-0.5f,  0.5f, 0.0f, 1.0f)).xyz,
    mul(elementToVFX, float4( 0.5f,  0.5f, 0.0f, 1.0f)).xyz
};";
                                break;
                            case VFXTaskType.ParticleTriangleOutput:
                                motionVectorVerts = @"float3 verts[] =
{
    mul(elementToVFX, float4(-0.5f, -0.288675129413604736328125f, 0.0f, 1.0f)).xyz,
    mul(elementToVFX, float4( 0.0f,  0.57735025882720947265625f, 0.0f, 1.0f)).xyz,
    mul(elementToVFX, float4( 0.5f, -0.288675129413604736328125f, 0.0f, 1.0f)).xyz
};";
                                break;
                            case VFXTaskType.ParticlePointOutput:
                                motionVectorVerts = @"float3 verts[] = { elementToVFX._m03_m13_m23 };";
                                break;
                        }
                    }

                    if (motionVectorVerts != null)
                    {
                        var motionVectorVertsWriter = new VFXShaderWriter();
                        motionVectorVertsWriter.Write(motionVectorVerts);
                        yield return new KeyValuePair<string, VFXShaderWriter>("${VFXMotionVectorVerts}", motionVectorVertsWriter);
                    }
                }
            }
        }
    }
}
