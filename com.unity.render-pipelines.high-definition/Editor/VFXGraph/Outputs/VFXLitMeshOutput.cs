using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXLitMeshOutput : VFXAbstractParticleHDRPLitOutput, IVFXMultiMeshOutput
    {
        public override string name { get { return "Output Particle Lit Mesh"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleLitMesh"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleMeshOutput; } }
        public override bool supportsUV { get { return shaderGraph == null; } }
        public override bool implementsMotionVector { get { return true; } }

        public override CullMode defaultCullMode { get { return CullMode.Back; } }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Range(1, 4), SerializeField]
        private uint MeshCount = 1;
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        private bool lod = false;
        public uint meshCount => MeshCount;

        public override VFXOutputUpdate.Features outputUpdateFeatures
        {
            get
            {
                VFXOutputUpdate.Features features = base.outputUpdateFeatures;
                if (!(HasSorting() || HasStrips(true))) // TODO make it compatible with sorting and strips
                {
                    if (MeshCount > 1)
                        features |= VFXOutputUpdate.Features.MultiMesh;
                    if (lod)
                        features |= VFXOutputUpdate.Features.LOD;
                }
                return features;
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                if (colorMode != ColorMode.None)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
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

                if (usesFlipbook)
                    yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var property in base.inputProperties)
                    yield return property;

                foreach (var property in VFXMultiMeshHelper.GetInputProperties(MeshCount, lod))
                    yield return property;
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
