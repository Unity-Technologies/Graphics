using System;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.Rendering
{
    internal interface IStrippingReport<TShader, TShaderVariant>
        where TShader : UnityEngine.Object
    {
        public void OnShaderProcessed([NotNull] TShader shader, TShaderVariant shaderVariant, int variantsIn,
            int variantsOut, double stripTimeMs);

        public void DumpReport(string path);
    }

    internal static class StrippingReportFactory
    {
        public static IStrippingReport<TShader, TShaderVariant> CreateReport<TShader, TShaderVariant>()
            where TShader : UnityEngine.Object
        {
            if (typeof(TShader) == typeof(Shader))
            {
                return new StrippingReport<Shader, ShaderSnippetData, ShaderSnippetDataVariantStrippingInfo>() as IStrippingReport<TShader, TShaderVariant>;
            }

            if (typeof(TShader) == typeof(ComputeShader))
            {
                return new StrippingReport<ComputeShader, string, KernelVariantStrippingInfo>() as IStrippingReport<TShader, TShaderVariant>;
            }

            throw new Exception($"Type of report not supported for {typeof(TShader)}");
        }
    }
}
