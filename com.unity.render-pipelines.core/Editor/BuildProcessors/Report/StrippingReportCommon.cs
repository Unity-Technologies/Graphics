using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    static class VariantCounterExtension
    {
        public static string ToStrippedVariantInfo([DisallowNull] this IVariantCounter variantCounter)
        {
            return $"Total={variantCounter.variantsIn}/{variantCounter.variantsOut}({variantCounter.variantsOut / (float)variantCounter.variantsIn * 100f:0.00}%)";
        }
    }
    interface IVariantCounter
    {
        uint variantsIn { get; set; }
        uint variantsOut { get; set; }
    }

    interface IStrippedShader : IVariantCounter
    {
        string shaderName { get; }
        void AddVariant(IStrippedVariant variant);
        string Log(ShaderVariantLogLevel shaderVariantLogLevel);
    }

    interface IStrippedVariant : IVariantCounter
    {
        bool isSRPVariant { get; }
        void AppendLog(StringBuilder sb);
    }

    [Serializable]
    abstract class StrippedShaderBase : IVariantCounter
    {
        [SerializeField]
        protected string shader;
        public string shaderName => shader;

        [SerializeField] private uint inVariants;
        public uint variantsIn { get => inVariants; set => inVariants = value; }

        [SerializeField] private uint outVariants;
        public uint variantsOut { get => outVariants; set => outVariants = value; }

        protected List<IStrippedVariant> m_StrippedVariants = new List<IStrippedVariant>();

        public void AddVariant(IStrippedVariant variant)
        {
            variantsIn += variant.variantsIn;
            variantsOut += variant.variantsOut;
            m_StrippedVariants.Add(variant);
        }

        public string Log(ShaderVariantLogLevel shaderVariantLogLevel)
        {
            if (shaderVariantLogLevel is ShaderVariantLogLevel.OnlySRPShaders &&  !m_StrippedVariants.Any(i => i.isSRPVariant))
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"STRIPPING {shader} {this.ToStrippedVariantInfo()}");
            foreach (var strippedVariant in m_StrippedVariants)
            {
                strippedVariant.AppendLog(sb);
            }

            return sb.ToString();
        }
    }
}
