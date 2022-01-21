using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.Rendering
{
    [Serializable]
    class ShaderSnippetDataVariant : IStrippedVariant
    {
        [SerializeField] private uint inVariants;
        [SerializeField] private uint outVariants;
        [SerializeField] private string pass;
        [SerializeField] private string passType;
        [SerializeField] private string renderPipeline;

        public uint variantsIn => inVariants;
        public uint variantsOut => outVariants;
        public double stripTime { get; }
        public bool isSRPVariant => !string.IsNullOrEmpty(renderPipeline);

        public ShaderSnippetDataVariant(ShaderSnippetData shaderSnippetData, uint variantsIn, uint variantsOut, string renderPipeline, double stripTime)
        {
            pass = shaderSnippetData.passName;
            passType = shaderSnippetData.passType.ToString();
            inVariants = variantsIn;
            this.stripTime = stripTime;
            outVariants = variantsOut;
            this.renderPipeline = renderPipeline;
        }

        public void AppendLog(StringBuilder sb)
        {
            sb.AppendLine($" Pass ({pass}) ({passType}) - Render Pipeline={renderPipeline} - Total={variantsIn}/{variantsOut}({variantsOut / (float)variantsIn * 100f:0.00}%) - Time={stripTime}Ms");
        }
    }

    [Serializable]
    sealed class StrippedShader : StrippedShaderBase, IStrippedShader, ISerializationCallbackReceiver
    {
        public StrippedShader(Shader shader)
        {
            this.shader = shader.name;
            variantsIn = 0;
            variantsOut = 0;
        }

        [SerializeField] private ShaderSnippetDataVariant[] passes;

        public void OnBeforeSerialize()
        {
            passes = m_StrippedVariants.Cast<ShaderSnippetDataVariant>().ToArray();
        }

        public void OnAfterDeserialize()
        {
            throw new NotImplementedException();
        }
    }
}
