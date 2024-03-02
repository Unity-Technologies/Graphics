using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using static UnityEditor.VFX.VFXSortingUtility;

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
            Sort = 1 << 5 | Culling,
            CameraSort = 1 << 6 | Sort,
            FrustumCulling = 1 << 7 | IndirectDraw,
            FillRaytracingAABB = 1 << 8,
            VolumetricFog = 1 << 9 | IndirectDraw,
        }

        public VFXOutputUpdate() : base(VFXContextType.Filter, VFXDataType.Particle, VFXDataType.Particle) { }
        public override string name => "OutputUpdate";

        protected VFXAbstractParticleOutput m_Output;
        public VFXAbstractParticleOutput output => m_Output;
        public override VFXDataType ownedType => output != null ? output.ownedType : base.ownedType;

        public virtual void SetOutput(VFXAbstractParticleOutput output)
        {
            if (m_Output != null)
                throw new InvalidOperationException("Unexpected SetOutput called twice, supposed to be call only once after construction");

            features = output.outputUpdateFeatures;
            sortCriterion = output.GetSortCriterion();

            if (features == VFXOutputUpdate.Features.None)
                throw new ArgumentException("This output does not need an output update pass");

            m_Output = output;
        }

        protected Features features = Features.None;

        private SortCriteria sortCriterion = SortCriteria.DistanceToCamera;

        public static bool HasFeature(Features flags, Features feature)
        {
            return (flags & feature) == feature;
        }

        public static bool IsPerCamera(Features flags)
        {
            return HasFeature(flags, Features.MotionVector)
                || HasFeature(flags, Features.LOD)
                || HasFeature(flags, Features.CameraSort)
                || HasFeature(flags, Features.FrustumCulling)
                || HasFeature(flags, Features.VolumetricFog);
        }

        public bool HasFeature(Features feature)
        {
            return HasFeature(this.features, feature);
        }

        public virtual bool isPerCamera => IsPerCamera(features);

        public virtual uint bufferCount
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
                var expressionMapper = VFXExpressionMapper.FromBlocks(m_Output.activeFlattenedChildrenWithImplicit);

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
                    expressionMapper.AddExpression(VFXBuiltInExpression.FrameIndex, "currentFrameIndex", -1);
                }

                var localSpace = ((VFXDataParticle)GetData()).space == VFXSpace.Local;
                if (localSpace)
                {
                    //Since it's a compute shader without renderer associated, these entries aren't automatically sent
                    expressionMapper.AddExpression(VFXBuiltInExpression.LocalToWorld, "localToWorld", -1);
                    expressionMapper.AddExpression(VFXBuiltInExpression.WorldToLocal, "worldToLocal", -1);
                }

                if (m_Output.HasCustomSortingCriterion())
                {
                    var sortKeyExp = m_Output.inputSlots.First(s => s.name == "sortKey").GetExpression();
                    expressionMapper.AddExpression(sortKeyExp, "sortKey", -1);
                }


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

                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                foreach (var attribute in VFXAttributesManager.AffectingAABBAttributes)
                    yield return new VFXAttributeInfo(attribute, VFXAttributeMode.Read);

                if (HasFeature(Features.MultiMesh))
                    yield return new VFXAttributeInfo(VFXAttribute.MeshIndex, VFXAttributeMode.Read);

                if (HasFeature(Features.MotionVector) && output is VFXLineOutput)
                    yield return new VFXAttributeInfo(VFXAttribute.TargetPosition, VFXAttributeMode.Read);

                if (sortCriterion == SortCriteria.YoungestInFront)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Age, VFXAttributeMode.Read);
                }
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                // Output Update need to handle local to world matrix for each instance
                yield return "HAVE_VFX_MODIFICATION";

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
                {
                    yield return "VFX_FEATURE_SORT";
                    yield return "SORTING_SIGN " + (output.revertSorting ? -1 : 1);
                    foreach (string additionalDef in GetSortingAdditionalDefines(m_Output.GetSortCriterion()))
                    {
                        yield return additionalDef;
                    }
                }
                if (HasFeature(Features.FrustumCulling))
                    yield return "VFX_FEATURE_FRUSTUM_CULL";
                if (output.HasStrips(false))
                    yield return "HAS_STRIPS";
                if (HasFeature(Features.FillRaytracingAABB))
                {
                    foreach (var define in output.rayTracingDefines)
                        yield return define;
                }
            }
        }

        public override IEnumerable<VFXMapping> additionalMappings
        {
            get
            {
                if (HasFeature(Features.FillRaytracingAABB))
                    yield return new VFXMapping("contextComputesAabb", 1);
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

        public override VFXContextCompiledData PrepareCompiledData()
        {
            var compiledData = base.PrepareCompiledData();
            var outputUpdateTask = compiledData.tasks.Last();

            if (HasFeature(Features.IndirectDraw))
            {
                string bufferName = VFXDataParticle.k_IndirectBufferName;
                outputUpdateTask.bufferMappings.Add(new VFXTask.BufferMapping(bufferName, "outputBuffer") { useBufferCountIndexInName = true });
                compiledData.AllocateIndirectBuffer(IsPerCamera(features), HasFeature(Features.Sort) ? 8u : 4u, bufferName, bufferCount);
            }

            if (HasFeature(Features.Sort))
            {
                // We don't need to bind the sorting buffer here because the VFX sort is done after in a separate pass
                // but because this pass doesn't use the task system, we need to declare it in the output update just before the sort.
                outputUpdateTask.bufferMappings.Add(VFXDataParticle.k_SortedIndirectBufferName);

                compiledData.buffers.Add(new VFXContextBufferDescriptor
                {
                    bufferSizeMode = VFXContextBufferSizeMode.FixedSizePlusScaleWithCapacity,
                    capacityScaleMultiplier = 1,
                    size = 1,
                    baseName = VFXDataParticle.k_SortedIndirectBufferName,
                    isPerCamera = IsPerCamera(features),
                    stride = 4u,
                    bufferTarget = GraphicsBuffer.Target.Structured,
                    bufferCount = bufferCount
                });
            }

            return compiledData;
        }
    }
}
