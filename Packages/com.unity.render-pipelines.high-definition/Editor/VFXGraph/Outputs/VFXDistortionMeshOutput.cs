using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace UnityEditor.VFX.HDRP
{
    [VFXInfo(name = "Output Particle|HDRP Distortion|Mesh", category = "#4Output Advanced")]
    class VFXDistortionMeshOutput : VFXAbstractDistortionOutput
    {
        public override string name => "Output Particle".AppendLabel("HDRP Distortion", false) + "\nMesh";
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleDistortionMesh"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleMeshOutput; } }
        public override bool supportsUV { get { return true; } }
        public override CullMode defaultCullMode { get { return CullMode.Back; } }

        public class InputProperties
        {
            [Tooltip("Specifies the mesh used to render the particle.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
            [Tooltip("Defines a bitmask to control which submeshes are rendered."), BitField]
            public uint subMeshMask = 0xffffffff;
        }


        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var mapper = base.GetExpressionMapper(target);

            switch (target)
            {
                case VFXDeviceTarget.CPU:
                {
                    mapper.AddExpression(inputSlots.First(s => s.name == "mesh").GetExpression(), "mesh", -1);
                    mapper.AddExpression(inputSlots.First(s => s.name == "subMeshMask").GetExpression(), "subMeshMask", -1);
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
