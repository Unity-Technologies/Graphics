using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.HDRP
{
    [VFXInfo(name = "Output Particle|HDRP Lit|Mesh", category = "#2Output Basic")]
    class VFXLitMeshOutput : VFXAbstractParticleHDRPLitOutput, IVFXMultiMeshOutput
    {
        public override string name => "Output Particle".AppendLabel("HDRP Lit") + "\nMesh";
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleLitMesh"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleMeshOutput; } }
        public override bool supportsUV { get { return GetOrRefreshShaderGraphObject() == null; } }
        public override bool implementsMotionVector { get { return true; } }

        public override CullMode defaultCullMode { get { return CullMode.Back; } }

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
                    if (HasSorting() && VFXOutputUpdate.HasFeature(features, VFXOutputUpdate.Features.IndirectDraw) || needsOwnSort)
                    {
                        if (VFXSortingUtility.IsPerCamera(sortMode))
                            features |= VFXOutputUpdate.Features.CameraSort;
                        else
                            features |= VFXOutputUpdate.Features.Sort;
                    }
                }
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

                yield return nameof(enableRayTracing);

                // TODO Add a experimental bool to setting attribute
                if (!VFXViewPreference.displayExperimentalOperator)
                {
                    yield return nameof(MeshCount);
                    yield return nameof(lod);
                }
            }
        }

        protected override IEnumerable<string> untransferableSettings
        {
            get
            {
                foreach (var setting in base.untransferableSettings)
                {
                    yield return setting;
                }
                yield return nameof(enableRayTracing);
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

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);
            var dataParticle = GetData() as VFXDataParticle;
            if (dataParticle != null && dataParticle.boundsMode != BoundsSettingMode.Manual)
                report.RegisterError("WarningBoundsComputation", VFXErrorType.Warning, $"Bounds computation have no sense of what the scale of the output mesh is," +
                    $" so the resulted computed bounds can be too small or big" +
                    $" Please use padding to mitigate this discrepancy.", this);
        }
    }
}
