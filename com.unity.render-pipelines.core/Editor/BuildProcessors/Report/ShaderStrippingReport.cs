using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
            ShaderVariantLogLevel logStrippedVariants = ShaderVariantLogLevel.AllShaders;
            bool exportStrippedVariants = false;

            // Obtain logging and export information if the Global settings are configured as IShaderVariantSettings
            if (RenderPipelineManager.currentPipeline is {defaultSettings: IShaderVariantSettings shaderVariantSettings})
            {
                logStrippedVariants = shaderVariantSettings.shaderVariantLogLevel;
                exportStrippedVariants = shaderVariantSettings.exportShaderVariants;
            }

            if (logStrippedVariants is ShaderVariantLogLevel.AllShaders or ShaderVariantLogLevel.OnlyShaders or ShaderVariantLogLevel.OnlySRPShaders)
                instance.Dump<Shader>(logStrippedVariants, exportStrippedVariants);

            if (logStrippedVariants is ShaderVariantLogLevel.AllShaders or ShaderVariantLogLevel.OnlyComputeShaders)
                instance.Dump<ComputeShader>(logStrippedVariants, exportStrippedVariants);

            // Reset the report instance
            instance = null;
        }

        private readonly ShaderStrippingInfo<StrippedShader> shaderStrippingInfo = new ShaderStrippingInfo<StrippedShader>();
        private readonly ShaderStrippingInfo<StrippedComputeShader> computeShaderStrippingInfo = new ();

        private IStrippedShader GetStrippedShader<TShader>([DisallowNull] TShader shader, uint variantsIn, uint variantsOut)
            where TShader : UnityEngine.Object
        {
            IStrippedShader strippedShader = null;
            IVariantCounter variantCounter = null;

            if (typeof(TShader) == typeof(Shader))
            {
                variantCounter = shaderStrippingInfo;
                if (!shaderStrippingInfo.TryGetStrippedShader(shader.name, out strippedShader))
                {
                    strippedShader = new StrippedShader((Shader)Convert.ChangeType(shader, typeof(Shader)));
                    shaderStrippingInfo.Add(shader.name, strippedShader);
                }
            }

            if (typeof(TShader) == typeof(ComputeShader))
            {
                variantCounter = computeShaderStrippingInfo;
                if (!computeShaderStrippingInfo.TryGetStrippedShader(shader.name, out strippedShader))
                {
                    strippedShader = new StrippedComputeShader((ComputeShader)Convert.ChangeType(shader, typeof(ComputeShader)));
                    computeShaderStrippingInfo.Add(shader.name, strippedShader);
                }
            }

            variantCounter.variantsIn += variantsIn;
            variantCounter.variantsOut += variantsOut;

            return strippedShader;
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

        void Dump<TShader>(ShaderVariantLogLevel shaderVariantLogLevel, bool exportStrippedVariants)
        {
            IShaderStrippingOutput output = null;
            if (typeof(TShader) == typeof(Shader))
            {
                output = shaderStrippingInfo;
            }
            else if (typeof(TShader) == typeof(ComputeShader))
            {
                output = computeShaderStrippingInfo;
            }

            var shaderStrippingOutput = output.GetOutput(shaderVariantLogLevel, exportStrippedVariants);
            if (shaderStrippingOutput.logs.Any())
            {
                Debug.Log($"STRIPPING {typeof(TShader).Name} {instance.shaderStrippingInfo.ToStrippedVariantInfo()}");
                foreach (var log in shaderStrippingOutput.logs)
                {
                    Debug.Log(log);
                }
            }

            if (!exportStrippedVariants) return;

            try
            {
                File.WriteAllText($"Temp/{typeof(TShader).Name}-stripping.json", shaderStrippingOutput.exportAsJson);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
