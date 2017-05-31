using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXQuadOutput : VFXContext
    {
        public VFXQuadOutput() : base(VFXContextType.kOutput, VFXDataType.kParticle, VFXDataType.kNone) {}
        public override string name { get { return "Quad Output"; } }

        public class InputProperties
        {
            public Texture2D texture;
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
            mapper.AddExpression(GetInputSlot(0).GetExpression(), "texture");
            return mapper;
        }
    }
}
