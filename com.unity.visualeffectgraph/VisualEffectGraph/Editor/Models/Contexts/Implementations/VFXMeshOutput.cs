using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXMeshOutput : VFXContext
    {
        public VFXMeshOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) {}
        public override string name { get { return "Mesh Output"; } }

        public class InputProperties
        {
            public Mesh mesh;
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            switch (target)
            {
                case VFXDeviceTarget.GPU:
                {
                    var mapper = new VFXExpressionMapper();
                    for (int i = 0; i < GetNbChildren(); ++i)
                        mapper.AddExpressions(GetChild(i).parameters, i);
                    return mapper;
                }

                case VFXDeviceTarget.CPU:
                {
                    var mapper = new VFXExpressionMapper();
                    mapper.AddExpression(GetInputSlot(0).GetExpression(), "mesh", -1);
                    return mapper;
                }

                default:
                    return null;
            }
        }
    }
}
