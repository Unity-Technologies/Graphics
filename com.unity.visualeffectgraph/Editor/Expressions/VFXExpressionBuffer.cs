using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VFX;
namespace UnityEditor.VFX
{
    class VFXExpressionBufferCount : VFXExpression
    {
        public VFXExpressionBufferCount() : this(VFXValue<GraphicsBuffer>.Default)
        {
        }

        public VFXExpressionBufferCount(VFXExpression graphicsBuffer) : base(Flags.InvalidOnGPU, graphicsBuffer)
        {
        }

        public override VFXExpressionOperation operation => VFXExpressionOperation.BufferCount;

        protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var graphicsBuffer = constParents[0].Get<GraphicsBuffer>();
            return VFXValue.Constant<uint>(graphicsBuffer != null ? (uint)graphicsBuffer.count : 0u);
        }
    }

    class VFXExpressionBufferStride : VFXExpression
    {
        public VFXExpressionBufferStride() : this(VFXValue<GraphicsBuffer>.Default)
        {
        }

        public VFXExpressionBufferStride(VFXExpression graphicsBuffer) : base(Flags.InvalidOnGPU, graphicsBuffer)
        {
        }

        public override VFXExpressionOperation operation => VFXExpressionOperation.BufferStride;

        protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var graphicsBuffer = constParents[0].Get<GraphicsBuffer>();
            return VFXValue.Constant<uint>(graphicsBuffer != null ? (uint)graphicsBuffer.stride : 0u);
        }
    }

#pragma warning disable 0659
    class VFXExpressionSampleBuffer : VFXExpression
    {
        public VFXExpressionSampleBuffer() : this(null, VFXValueType.None, string.Empty, VFXValue<GraphicsBuffer>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        private Type m_SampledType;
        private VFXValueType m_FieldType;
        private string m_FieldPath;

        public Type GetSampledType()
        {
            return m_SampledType;
        }

        public VFXExpressionSampleBuffer(Type sampledType, VFXValueType fieldType, string path, VFXExpression graphicsBuffer, VFXExpression index, VFXExpression stride, VFXExpression count) : base(Flags.InvalidOnCPU, graphicsBuffer, index, stride, count)
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
            var buffer = parents[0];
            var index = parents[1];
            var stride = parents[2];
            var count = parents[3];

            return string.Format("SampleStructuredBuffer({0}, {1}, {2}, {3}){4}", buffer, index, stride, count, string.IsNullOrEmpty(m_FieldPath) ? "" : "." + m_FieldPath);
        }
    }
}
