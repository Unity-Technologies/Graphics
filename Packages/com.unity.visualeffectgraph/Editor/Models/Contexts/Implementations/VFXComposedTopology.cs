using System;

using UnityEngine;
using StripTilingMode = UnityEditor.VFX.VFXAbstractParticleOutput.StripTilingMode;

namespace UnityEditor.VFX
{
    [Serializable]
    class ParticleTopologyPlanarPrimitive : ParticleTopology
    {
        public ParticleTopologyPlanarPrimitive() : this(VFXPrimitiveType.Quad)
        {
        }

        public ParticleTopologyPlanarPrimitive(VFXPrimitiveType primitiveType)
        {
            this.primitiveType = primitiveType;
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies what primitive type to use for this output. Triangle outputs have fewer vertices, octagons can be used to conform the geometry closer to the texture to avoid overdraw, and quads are a good middle ground.")]
        protected VFXPrimitiveType primitiveType;

        public override TraitDescription GetDescription(VFXAbstractComposedParticleOutput parent)
        {
            var desc = base.GetDescription(parent);
            desc.name = primitiveType.ToString();
            if (primitiveType == VFXPrimitiveType.Octagon)
            {
                desc.properties.AddRange(PropertiesFromType(typeof(VFXPlanarPrimitiveHelper.OctagonInputProperties)));
                desc.propertiesGpuExpressions.Add((ExpressionFromSlot)nameof(VFXPlanarPrimitiveHelper.OctagonInputProperties.cropFactor));
            }

            desc.additionalDefines.Add(VFXPlanarPrimitiveHelper.GetShaderDefine(primitiveType));
            desc.taskType = VFXPlanarPrimitiveHelper.GetTaskType(primitiveType);
            desc.supportMotionVectorPerVertex = VFXAbstractParticleOutput.SupportsMotionVectorPerVertex(desc.taskType, parent.HasStrips(false), parent.isRayTraced, out desc.motionVectorPerVertexCount);

            return desc;
        }
    }

    [Serializable]
    sealed class ParticleTopologyMesh : ParticleTopology, IVFXMultiMeshOutput
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Range(1, 4), Tooltip("Specifies the number of different meshes (up to 4). Mesh per particle can be specified with the meshIndex attribute."), SerializeField]
        uint MeshCount = 1;
        uint actualMeshCount = 1;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, screen space LOD is used to determine with meshIndex to use per particle."), SerializeField]
        bool lod = false;

        public override TraitDescription GetDescription(VFXAbstractComposedParticleOutput parent)
        {
            var desc = base.GetDescription(parent);
            desc.name = "Mesh";
            desc.taskType = VFXTaskType.ParticleMeshOutput;
            desc.supportMotionVectorPerVertex = false;
            desc.motionVectorPerVertexCount = 0;
            desc.hiddenSettings.Add("enableRayTracing");

            if (parent.HasStrips(true))
                actualMeshCount = 1;
            else
                actualMeshCount = MeshCount;

            //Can't use parent.outputUpdateFeatures at this stage it would cause a StackOverflow
            if (actualMeshCount > 1)
                desc.features |= VFXOutputUpdate.Features.MultiMesh;
            if (lod)
                desc.features |= VFXOutputUpdate.Features.LOD;
            if (parent.HasFrustumCulling())
                desc.features |= VFXOutputUpdate.Features.FrustumCulling;

            desc.properties.AddRange(VFXMultiMeshHelper.GetInputProperties(MeshCount, desc.features));
            foreach (var cpuExpression in VFXMultiMeshHelper.GetCPUExpressionNames(MeshCount))
                desc.propertiesCpuExpressions.Add((ExpressionFromSlot)cpuExpression);

            return desc;
        }

        public uint meshCount => actualMeshCount;
    }

    [Serializable]
    sealed class ParticleTopologyQuadStrip : ParticleTopologyPlanarPrimitive
    {
        [VFXSetting, SerializeField, Tooltip("Specifies the way the UVs are interpolated along the strip. They can either be stretched or repeated per segment.")]
        StripTilingMode tilingMode = StripTilingMode.Stretch;

        [VFXSetting, SerializeField, Tooltip("When enabled, uvs for the strips are swapped.")]
        bool swapUV = false;

        public class CustomUVInputProperties
        {
            [Tooltip("Specifies the texture coordinate value (u or v depending on swap UV being enabled) used along the strip.")]
            public float texCoord = 0.0f;
        }

        public override TraitDescription GetDescription(VFXAbstractComposedParticleOutput parent)
        {
            var desc = base.GetDescription(parent);
            desc.hiddenSettings.Add(nameof(primitiveType));

            if (tilingMode == StripTilingMode.Custom)
            {
                desc.properties.AddRange(PropertiesFromType(typeof(CustomUVInputProperties)));
                desc.propertiesGpuExpressions.Add((ExpressionFromSlot)nameof(CustomUVInputProperties.texCoord));
            }

            if (tilingMode == StripTilingMode.Stretch)
                desc.additionalDefines.Add("VFX_STRIPS_UV_STRECHED");
            else if (tilingMode == StripTilingMode.RepeatPerSegment)
                desc.additionalDefines.Add("VFX_STRIPS_UV_PER_SEGMENT");
            if (swapUV)
                desc.additionalDefines.Add("VFX_STRIPS_SWAP_UV");

            return desc;
        }
    }
}
