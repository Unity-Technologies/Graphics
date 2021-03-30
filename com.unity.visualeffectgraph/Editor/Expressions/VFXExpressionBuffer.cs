using System;
using UnityEngine;
using UnityEngine.VFX;
namespace UnityEditor.VFX
{
    //*WIP*
    class VFXExpressionSampleBuffer : VFXExpression
    {
        public VFXExpressionSampleBuffer() : this(null, string.Empty, VFXValue<GraphicsBuffer>.Default, VFXValue<uint>.Default)
        {
        }

        //TODOPAUL : Check code convention
        private Type m_type;
        private string m_name;

        public VFXExpressionSampleBuffer(Type type, string name, VFXExpression graphicsBuffer, VFXExpression index) : base(Flags.InvalidOnCPU, new VFXExpression[] { graphicsBuffer, index })
        {
            m_type = type;
            m_name = name;
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var newExpression = (VFXExpressionSampleBuffer)base.Reduce(reducedParents);
            newExpression.m_type = m_type;
            newExpression.m_name = m_name;
            return newExpression;
        }

        sealed public override VFXValueType valueType { get { return VFXValueType.Float4; } }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        public sealed override string GetCodeString(string[] parents)
        {
            //TEEEEEEMP & wrong but I don't care for now
            return string.Format("asfloat({0}.Load4(({1} * 2 + {2}) << 2));", parents[0], parents[1], m_name == "position" ? 0 : 1);
        }
    }
}
