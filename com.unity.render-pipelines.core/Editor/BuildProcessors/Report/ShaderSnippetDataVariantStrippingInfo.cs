using System;
using System.Collections.Generic;
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

        private List<SubShader> subShadersList { get; } = new List<SubShader>();

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
            if (shaderVariant.pass.SubshaderIndex >= subShadersList.Count)
                subShadersList.Add(new SubShader());

            var subShader = subShadersList[(int)shaderVariant.pass.SubshaderIndex];
            subShader.RecordVariants(variantsIn, variantsOut);
            m_Shader.TryToGetRenderPipelineTag(shaderVariant, out string renderPipelineTag);
            subShader.passes.Add(new Pass(shaderVariant, variantsIn, variantsOut, stripTimeMs, renderPipelineTag));
        }

        public void AppendLog(StringBuilder sb, bool onlySrp)
        {
            sb.AppendLine($"{shaderName} - {strippedVariantsInfo}");
            for (int i = 0; i < subShadersList.Count; ++i)
            {
                var subShader = subShadersList[i];
                sb.AppendLine($"- SubShader {i} - {subShader.strippedVariantsInfo}");

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
            subShaders = subShadersList.ToArray();
        }

        public void OnAfterDeserialize()
        {
        }
        #endregion
    }
}
