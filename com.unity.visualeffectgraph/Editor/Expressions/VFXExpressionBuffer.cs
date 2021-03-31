using System;
using UnityEngine;
using UnityEngine.VFX;
namespace UnityEditor.VFX
{
    //*WIP*
    #pragma warning disable 0659
    class VFXExpressionSampleBuffer : VFXExpression
    {
        public VFXExpressionSampleBuffer() : this(null, string.Empty, VFXValue<GraphicsBuffer>.Default, VFXValue<uint>.Default)
        {
        }

        //TODOPAUL : Check code convention
        private Type m_type;
        private string m_name;

        public Type GetSampledType()
        {
            return m_type;
        }

        public VFXExpressionSampleBuffer(Type type, string name, VFXExpression graphicsBuffer, VFXExpression index) : base(Flags.InvalidOnCPU, new VFXExpression[] { graphicsBuffer, index })
        {
            m_type = type;
            m_name = name;
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
                return false;

            var other = obj as VFXExpressionSampleBuffer;
            if (other == null)
                return false;

            return m_type.Equals(other.m_type) && m_name.Equals(other.m_name);
        }

        protected override int GetInnerHashCode()
        {
            int hash = base.GetInnerHashCode();
            hash = (hash * 397) ^ m_type.GetHashCode();
            hash = (hash * 397) ^ m_name.GetHashCode();
            return hash;
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var newExpression = (VFXExpressionSampleBuffer)base.Reduce(reducedParents);
            newExpression.m_type = m_type;
            newExpression.m_name = m_name;
            return newExpression;
        }

        //TODOPAUL : it can't be hardcoded
        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("{0}.Load((int){1}).{2};", parents[0], parents[1], m_name);
        }
    }
}
