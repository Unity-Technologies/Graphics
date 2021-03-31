using System;
using UnityEngine;
using UnityEngine.VFX;
namespace UnityEditor.VFX
{
    //*WIP*
    #pragma warning disable 0659
    class VFXExpressionSampleBuffer : VFXExpression
    {
        public VFXExpressionSampleBuffer() : this(null, VFXValueType.None, string.Empty, VFXValue<GraphicsBuffer>.Default, VFXValue<uint>.Default)
        {
        }

        //TODOPAUL : Check code convention
        private Type m_SampledType;
        private VFXValueType m_FieldType;
        private string m_FieldPath;

        public Type GetSampledType()
        {
            return m_SampledType;
        }

        public VFXExpressionSampleBuffer(Type sampledType, VFXValueType fieldType, string path, VFXExpression graphicsBuffer, VFXExpression index) : base(Flags.InvalidOnCPU, new VFXExpression[] { graphicsBuffer, index })
        {
            m_SampledType = sampledType;
            m_FieldType = fieldType;
            m_FieldPath = path;
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
                return false;

            var other = obj as VFXExpressionSampleBuffer;
            if (other == null)
                return false;

            return m_SampledType.Equals(other.m_SampledType) && m_FieldPath.Equals(other.m_FieldPath) && m_FieldType.Equals(other.m_FieldType);
        }

        protected override int GetInnerHashCode()
        {
            int hash = base.GetInnerHashCode();
            hash = (hash * 397) ^ m_SampledType.GetHashCode();
            hash = (hash * 397) ^ m_FieldPath.GetHashCode();
            hash = (hash * 397) ^ m_FieldType.GetHashCode();
            return hash;
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var newExpression = (VFXExpressionSampleBuffer)base.Reduce(reducedParents);
            newExpression.m_SampledType = m_SampledType;
            newExpression.m_FieldPath = m_FieldPath;
            newExpression.m_FieldType = m_FieldType;
            return newExpression;
        }

        sealed public override VFXValueType valueType { get { return m_FieldType; } }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("{0}[(int){1}].{2};", parents[0], parents[1], m_FieldPath);
        }
    }
}
