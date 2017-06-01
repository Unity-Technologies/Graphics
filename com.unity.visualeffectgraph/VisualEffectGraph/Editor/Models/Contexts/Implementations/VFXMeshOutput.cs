using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXMeshOutput : VFXContext
    {
        public VFXMeshOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) { }
        public override string name { get { return "Mesh Particle Output"; } }

        public class InputProperties
        {
            public Mesh mesh;
        }

        public override VFXExpressionMapper GetGPUExpressions()
        {
            var mapper = new VFXExpressionMapper("uniform");
            for (int i = 0; i < GetNbChildren(); ++i)
                mapper.AddExpressionFromSlotContainer(GetChild(i), i);
            return mapper;
        }

        public override VFXExpressionMapper GetCPUExpressions()
        {
            var mapper = new VFXExpressionMapper("");
            mapper.AddExpression(GetInputSlot(0).GetExpression(), "mesh");
            return mapper;
        }
    }
}
