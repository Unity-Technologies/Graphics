using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
public class ShaderStrippingReport
    {
        public static ShaderStrippingReport instance { get; private set; }

        internal static void InitializeReport()
        {
            instance = new ShaderStrippingReport();
        }

        internal static void DumpReport()
        {
            instance.Log();
            instance.Export();
            instance = null;
        }

        ShaderVariantLogLevel logStrippedVariants { get; }
        bool exportStrippedVariants { get; }

        private ShaderStrippingReport()
        {
            // Obtain logging and export information if the Global settings are configured as IShaderVariantSettings
            if (RenderPipelineManager.currentPipeline != null &&
                RenderPipelineManager.currentPipeline.defaultSettings is IShaderVariantSettings shaderVariantSettings)
            {
                logStrippedVariants = shaderVariantSettings.shaderVariantLogLevel;
                exportStrippedVariants = shaderVariantSettings.exportShaderVariants;
            }
        }

        private readonly ShaderStrippingInfo<StrippedShader> shaderStrippingInfo = new ShaderStrippingInfo<StrippedShader>();
        private readonly ShaderStrippingInfo<StrippedComputeShader> computeShaderStrippingInfo = new ();

        private IStrippedShader GetStrippedShader<TShader>([DisallowNull] TShader shader, uint variantsIn, uint variantsOut)
            where TShader : UnityEngine.Object
        {
            if (typeof(TShader) == typeof(Shader))
            {
                if (!shaderStrippingInfo.TryGetStrippedShader(shader.name, out IStrippedShader strippedShader))
                {
                    strippedShader = new StrippedShader((Shader)Convert.ChangeType(shader, typeof(Shader)));
                    shaderStrippingInfo.Add(shader.name, strippedShader);
                }

                shaderStrippingInfo.totalVariantsIn += variantsIn;
                shaderStrippingInfo.totalVariantsOut += variantsOut;

                return strippedShader;
            }

            if (typeof(TShader) == typeof(ComputeShader))
            {
                if (!computeShaderStrippingInfo.TryGetStrippedShader(shader.name, out IStrippedShader strippedShader))
                {
                    strippedShader = new StrippedComputeShader((ComputeShader)Convert.ChangeType(shader, typeof(ComputeShader)));
                    computeShaderStrippingInfo.Add(shader.name, strippedShader);
                }

                computeShaderStrippingInfo.totalVariantsIn += variantsIn;
                computeShaderStrippingInfo.totalVariantsOut += variantsOut;

                return strippedShader;
            }

            throw new NotImplementedException(typeof(TShader).FullName);
        }

        private IStrippedVariant CreateStrippedVariant<TShader, TShaderVariant>([DisallowNull]TShader shader, TShaderVariant shaderVariant, uint variantsIn, uint variantsOut, double stripTimeMs)
        {
            IStrippedVariant strippedVariant = null;
            if (typeof(TShaderVariant) == typeof(ShaderSnippetData))
            {
                var snippetData = (ShaderSnippetData)Convert.ChangeType(shaderVariant, typeof(ShaderSnippetData));
                strippedVariant = new ShaderSnippetDataVariant(
                    snippetData,
                    variantsIn,
                    variantsOut,
                    ((Shader)Convert.ChangeType(shader, typeof(Shader))).GetRenderPipelineTag(snippetData),
                    stripTimeMs);
            }
            else if (typeof(TShaderVariant) == typeof(string))
            {
                strippedVariant = new KernelVariant(
                    (string)Convert.ChangeType(shaderVariant, typeof(string)),
                    variantsIn,
                    variantsOut,
                    stripTimeMs);
            }

            return strippedVariant;
        }

        public void OnShaderProcessed<TShader, TShaderVariant>(
            [DisallowNull] TShader shader,
            TShaderVariant shaderVariant,
            uint variantsIn,
            uint variantsOut,
            double stripTimeMs)
        where TShader : UnityEngine.Object
        {
            IStrippedShader strippedShader = GetStrippedShader(shader, variantsIn, variantsOut);
            IStrippedVariant strippedVariant = CreateStrippedVariant(shader, shaderVariant, variantsIn, variantsOut, stripTimeMs);
            strippedShader.AddVariant(strippedVariant);
        }

        void Log()
        {
            if (logStrippedVariants == ShaderVariantLogLevel.Disabled)
                return;

            bool onlySrp = logStrippedVariants == ShaderVariantLogLevel.OnlySRPShaders;

            if (shaderStrippingInfo.totalVariantsIn > 0)
                shaderStrippingInfo.Log("SHADERS", onlySrp);

            if (computeShaderStrippingInfo.totalVariantsIn > 0)
                computeShaderStrippingInfo.Log("COMPUTE SHADERS", onlySrp);
        }

        private void Export()
        {
            if (!exportStrippedVariants)
                return;

            shaderStrippingInfo.Export("Temp/shader-stripping.json");
            computeShaderStrippingInfo.Export("Temp/compute-shader-stripping.json");
        }
    }
}
