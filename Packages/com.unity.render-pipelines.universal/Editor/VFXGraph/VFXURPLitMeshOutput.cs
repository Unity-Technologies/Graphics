#if HAS_VFX_GRAPH

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.URP
{
    [VFXHelpURL("Context-OutputPrimitive")]
    [VFXInfo(name = "Output Particle|URP Lit|Mesh", category = "#2Output Basic")]
    class VFXURPLitMeshOutput : VFXAbstractParticleURPLitOutput, IVFXMultiMeshOutput
    {
        public override string name => "Output Particle".AppendLabel("URP Lit").AppendLabel("Mesh");
        public override string codeGeneratorTemplate => RenderPipeTemplate("VFXParticleLitMesh");
        public override VFXTaskType taskType => VFXTaskType.ParticleMeshOutput;
        public override bool supportsUV => GetOrRefreshShaderGraphObject() == null;
        public override bool implementsMotionVector => true;

        public override CullMode defaultCullMode => CullMode.Back;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Range(1, 4), Tooltip("Specifies the number of different meshes (up to 4). Mesh per particle can be specified with the meshIndex attribute."), SerializeField]
        private uint MeshCount = 1;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Tooltip("When enabled, screen space LOD is used to determine with meshIndex to use per particle."), SerializeField]
        private bool lod = false;
        public uint meshCount => HasStrips(true) ? 1 : MeshCount;

        public override VFXOutputUpdate.Features outputUpdateFeatures
        {
            get
            {
                VFXOutputUpdate.Features features = base.outputUpdateFeatures;
                if (!HasStrips(true)) // TODO make it compatible with strips
                {
                    if (MeshCount > 1)
                        features |= VFXOutputUpdate.Features.MultiMesh;
                    if (lod)
                        features |= VFXOutputUpdate.Features.LOD;
                }
                if (HasSorting() && VFXOutputUpdate.HasFeature(features, VFXOutputUpdate.Features.IndirectDraw))
                    features |= VFXOutputUpdate.Features.Sort;
                return features;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var property in base.inputProperties)
                    yield return property;

                foreach (var property in VFXMultiMeshHelper.GetInputProperties(MeshCount, outputUpdateFeatures))
                    yield return property;
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var s in base.filteredOutSettings)
                    yield return s;

                // TODO Add a experimental bool to setting attribute
                if (!VFXViewPreference.displayExperimentalOperator)
                {
                    yield return "MeshCount";
                    yield return "lod";
                }
            }
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var mapper = base.GetExpressionMapper(target);

            switch (target)
            {
                case VFXDeviceTarget.CPU:
                {
                    foreach (var name in VFXMultiMeshHelper.GetCPUExpressionNames(MeshCount))
                        mapper.AddExpression(inputSlots.First(s => s.name == name).GetExpression(), name, -1);
                    break;
                }
                default:
                {
                    break;
                }
            }

            return mapper;
        }
    }
}
#endif
