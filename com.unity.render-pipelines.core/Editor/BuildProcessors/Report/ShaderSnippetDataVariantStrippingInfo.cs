using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.Rendering
{
    [Serializable]
    internal class ShaderSnippetDataVariantStrippingInfo : VariantCounter, IVariantStrippingInfo<Shader, ShaderSnippetData>, ISerializationCallbackReceiver
    {
        [Serializable]
        class Pass : VariantCounter
        {
            public Pass(ShaderSnippetData shaderVariant, int variantsIn, int variantsOut, double stripTimeMs, string renderPipelineTag)
            {
                RecordVariants(variantsIn, variantsOut);

                pass = shaderVariant.passName;
                passType = shaderVariant.passType.ToString();
                this.stripTimeMs = stripTimeMs;
                this.renderPipelineTag = renderPipelineTag;
            }

            public string renderPipelineTag;
            public string pass;
            public string passType;
            [NonSerialized]
            public double stripTimeMs;
        }

        [Serializable]
        class SubShader : VariantCounter
        {
            public List<Pass> passes = new List<Pass>();
        }

        private Dictionary<uint, SubShader> subShadersDictionary { get; } = new ();

        [SerializeField]
        private string shaderName;
        private Shader m_Shader;
        public void SetShader(Shader shader)
        {
            m_Shader = shader;
            shaderName = shader.name;
        }

        public void Add(ShaderSnippetData shaderVariant, int variantsIn, int variantsOut, double stripTimeMs)
        {
            RecordVariants(variantsIn, variantsOut);

            if (!subShadersDictionary.TryGetValue(shaderVariant.pass.SubshaderIndex, out var subShader))
            {
                subShader = new SubShader();
                subShadersDictionary.Add(shaderVariant.pass.SubshaderIndex, subShader);
            }

            subShader.RecordVariants(variantsIn, variantsOut);
            m_Shader.TryToGetRenderPipelineTag(shaderVariant, out string renderPipelineTag);
            subShader.passes.Add(new Pass(shaderVariant, variantsIn, variantsOut, stripTimeMs, renderPipelineTag));
        }

        public void AppendLog(StringBuilder sb, bool onlySrp)
        {
            sb.AppendLine($"{shaderName} - {strippedVariantsInfo}");
            foreach(var (index, subShader) in subShadersDictionary)
            {
                sb.AppendLine($"- SubShader {index} - {subShader.strippedVariantsInfo}");

                foreach (var pass in subShader.passes)
                {
                    if (onlySrp && !string.IsNullOrEmpty(pass.renderPipelineTag) || !onlySrp)
                        sb.AppendLine($" Pass ({pass.pass}) ({pass.passType}) - {pass.strippedVariantsInfo} - Time={pass.stripTimeMs}Ms");
                }

                sb.AppendLine();
            }
        }

        #region ISerializationCallbackReceiver
        [SerializeField]
        private SubShader[] subShaders;

        public void OnBeforeSerialize()
        {
            subShaders = subShadersDictionary.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
        }
        #endregion
    }
}
