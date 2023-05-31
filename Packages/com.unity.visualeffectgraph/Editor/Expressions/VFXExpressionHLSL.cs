using System.Collections.Generic;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
#pragma warning disable 0659
    class VFXExpressionPassThrough : VFXExpression
    {
        private int m_ParentIndex;
        private VFXValueType m_ValueType;

        public VFXExpressionPassThrough() : this(0, VFXValueType.None, new [] { VFXValue<int>.Default })
        {
        }

        public VFXExpressionPassThrough(int index, VFXValueType type, params VFXExpression[] parents) : base(Flags.InvalidOnCPU, parents)
        {
            m_ParentIndex = index;
            m_ValueType = type;
        }

        public override VFXExpressionOperation operation => VFXExpressionOperation.None;

        public override VFXValueType valueType => m_ValueType;

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return this;
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var newExpression = (VFXExpressionPassThrough)base.Reduce(reducedParents);
            newExpression.m_ParentIndex = m_ParentIndex;
            newExpression.m_ValueType = m_ValueType;
            return newExpression;
        }

        public override string GetCodeString(string[] parents)
        {
            return parents[m_ParentIndex];
        }
    }

    class VFXExpressionHLSL : VFXExpression, IHLSLCodeHolder
    {
        string m_FunctionName;
        VFXValueType m_ValueType;
        string m_HlslCode;
        List<int> m_TextureParentExpressionIndex;

        public VFXExpressionHLSL() : this(string.Empty, string.Empty, VFXValueType.None, new [] { VFXValue<int>.Default })
        {
        }

        public VFXExpressionHLSL(string functionName, string hlslCode, VFXValueType returnType, VFXExpression[] parents) : base(Flags.InvalidOnCPU, parents)
        {
            this.m_TextureParentExpressionIndex = new List<int>();
            this.m_FunctionName = functionName;
            this.m_ValueType = returnType;
            this.m_HlslCode = hlslCode;
        }

        public override VFXValueType valueType => m_ValueType;
        public override VFXExpressionOperation operation => VFXExpressionOperation.None;
        public ShaderInclude shaderFile => null;
        public string sourceCode { get; set; }
        public string customCode => m_HlslCode;
        public bool HasShaderFile() => false;
        public bool Equals(IHLSLCodeHolder other) => false;

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
                return false;

            if (obj is VFXExpressionHLSL hlslExpression)
            {
                return hlslExpression.m_FunctionName == m_FunctionName && hlslExpression.valueType == valueType && hlslExpression.m_HlslCode == m_HlslCode;
            }

            return false;
        }

        protected override int GetInnerHashCode()
        {
            var hash = base.GetInnerHashCode();
            hash = (hash * 397) ^ m_FunctionName.GetHashCode();
            hash = (hash * 397) ^ m_ValueType.GetHashCode();
            hash = (hash * 397) ^ m_HlslCode.GetHashCode();
            return hash;
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            m_TextureParentExpressionIndex.Clear();

            for (int i = 0; i < reducedParents.Length; i++)
            {
                if (IsTexture(reducedParents[i].valueType))
                {
                    m_TextureParentExpressionIndex.Add(i);
                }
            }

            var newExpression = (VFXExpressionHLSL)base.Reduce(reducedParents);
            newExpression.m_FunctionName = m_FunctionName;
            newExpression.m_ValueType = m_ValueType;
            newExpression.m_HlslCode = m_HlslCode;
            newExpression.m_TextureParentExpressionIndex = m_TextureParentExpressionIndex;
            return newExpression;
        }

        public sealed override string GetCodeString(string[] parentsExpressions)
        {
            foreach (var index in m_TextureParentExpressionIndex)
            {
                var expression = parentsExpressions[index];
                parentsExpressions[index] = $"VFX_SAMPLER({expression})";
            }
            return $"{m_FunctionName}({string.Join(", ", parentsExpressions)})";
        }
    }
}
