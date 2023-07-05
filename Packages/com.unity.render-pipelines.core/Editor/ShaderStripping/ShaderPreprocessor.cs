using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    abstract class ShaderPreprocessor<TShader, TShaderVariant>
        where TShader : UnityEngine.Object
    {
        IVariantStripper<TShader, TShaderVariant>[] m_Strippers = null;
        protected virtual IVariantStripper<TShader, TShaderVariant>[] strippers
        {
            get => m_Strippers ??= FetchShaderStrippers();
            private set => m_Strippers = value;
        }

        Lazy<string> m_GlobalRenderPipeline = new (FetchGlobalRenderPipelineShaderTag);
        string globalRenderPipeline => m_GlobalRenderPipeline.Value;

        IVariantStripperScope<TShader, TShaderVariant>[] m_Scopes = null;
        protected virtual IVariantStripperScope<TShader, TShaderVariant>[] scopes => m_Scopes ??= strippers.OfType<IVariantStripperScope<TShader, TShaderVariant>>().ToArray();

        protected ShaderPreprocessor() { }
        protected ShaderPreprocessor(IVariantStripper<TShader, TShaderVariant>[] strippers)
        {
            this.strippers = strippers ?? throw new ArgumentNullException(nameof(strippers));
        }

        private static string FetchGlobalRenderPipelineShaderTag()
        {
            // We can not rely on Shader.globalRenderPipeline as if there wasn't a Camera.Render the variable won't be initialized.
            // Therefore, we fetch the RenderPipeline assets that are included by the Quality Settings and or Graphics Settings
            string globalRenderPipelineTag = string.Empty;
            using (ListPool<RenderPipelineAsset>.Get(out List<RenderPipelineAsset> rpAssets))
            {
                if (EditorUserBuildSettings.activeBuildTarget.TryGetRenderPipelineAssets<RenderPipelineAsset>(rpAssets))
                {
                    // As all the RP assets must be from the same pipeline we can simply obtain the shader tag from the first one
                    var asset = rpAssets.FirstOrDefault();
                    if (asset != null)
                        globalRenderPipelineTag = asset.renderPipelineShaderTag;
                }
            }

            return globalRenderPipelineTag;
        }

        bool TryGetShaderVariantRenderPipelineTag([DisallowNull] TShader shader, TShaderVariant shaderVariant, out string renderPipelineTag)
        {
            var inputShader = (Shader)Convert.ChangeType(shader, typeof(Shader));
            var snippetData = (ShaderSnippetData)Convert.ChangeType(shaderVariant, typeof(ShaderSnippetData));
            return inputShader.TryGetRenderPipelineTag(snippetData, out renderPipelineTag);
        }

        private static IVariantStripper<TShader, TShaderVariant>[] FetchShaderStrippers()
        {
            var validStrippers = new List<IVariantStripper<TShader, TShaderVariant>>();

            // Gather all the implementations of IVariantStripper and add them as the strippers
            foreach (var stripper in TypeCache.GetTypesDerivedFrom<IVariantStripper<TShader, TShaderVariant>>())
            {
                if (stripper.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) !=
                    null)
                {
                    var stripperInstance =
                        Activator.CreateInstance(stripper) as IVariantStripper<TShader, TShaderVariant>;
                    if (stripperInstance.active)
                        validStrippers.Add(stripperInstance);
                }
            }

            return validStrippers.ToArray();
        }

        bool CanRemoveVariant([DisallowNull] TShader shader, TShaderVariant shaderVariant, ShaderCompilerData shaderCompilerData)
        {
            return strippers
                .Where(s => s is not IVariantStripperSkipper<TShader, TShaderVariant> skipper || !skipper.SkipShader(shader, shaderVariant))
                .All(s => s.CanRemoveVariant(shader, shaderVariant, shaderCompilerData));
        }

        /// <summary>
        /// Strips the given <see cref="TShader"/>
        /// </summary>
        /// <param name="shader">The <see cref="T" /> that might be stripped.</param>
        /// <param name="shaderVariant">The <see cref="TShaderVariant" /></param>
        /// <param name="compilerDataList">A list of <see cref="ShaderCompilerData" /></param>
        [CollectionAccess(CollectionAccessType.ModifyExistingContent)]
        [MustUseReturnValue]
        protected bool TryStripShaderVariants([DisallowNull] TShader shader, TShaderVariant shaderVariant, IList<ShaderCompilerData> compilerDataList, [NotNullWhen(false)] out Exception error)
        {
            if (shader == null)
            {
                error = new ArgumentNullException(nameof(shader));
                return false;
            }

            if (compilerDataList == null)
            {
                error = new ArgumentNullException(nameof(compilerDataList));
                return false;
            }

            var beforeStrippingInputShaderVariantCount = compilerDataList.Count;
            var afterStrippingShaderVariantCount = beforeStrippingInputShaderVariantCount;

            string renderPipelineTag = string.Empty;
            if (typeof(TShader) == typeof(Shader) && typeof(TShaderVariant) == typeof(ShaderSnippetData))
            {
                if (TryGetShaderVariantRenderPipelineTag(shader, shaderVariant, out renderPipelineTag))
                {
                    if (!renderPipelineTag.Equals(globalRenderPipeline, StringComparison.OrdinalIgnoreCase))
                        afterStrippingShaderVariantCount = 0;
                }
            }

            bool strippersAvailable = strippers.Any();

            double stripTimeMs = 0.0;
            using (TimedScope.FromRef(ref stripTimeMs))
            {
                if (strippersAvailable)
                {
                    for (int i = 0; i < scopes.Length; ++i)
                        scopes[i].BeforeShaderStripping(shader);

                    // Go through all the shader variants
                    for (var i = 0; i < afterStrippingShaderVariantCount;)
                    {
                        // Remove at swap back
                        if (CanRemoveVariant(shader, shaderVariant, compilerDataList[i]))
                            compilerDataList[i] = compilerDataList[--afterStrippingShaderVariantCount];
                        else
                            ++i;
                    }
                }

                // Remove the shader variants that will be at the back
                if (!compilerDataList.TryRemoveElementsInRange(afterStrippingShaderVariantCount, compilerDataList.Count - afterStrippingShaderVariantCount, out error))
                    return false;

                if (strippersAvailable)
                {
                    for (int i = 0; i < scopes.Length; ++i)
                        scopes[i].AfterShaderStripping(shader);
                }
            }

            ShaderStripping.reporter.OnShaderProcessed(shader, shaderVariant, renderPipelineTag, (uint)beforeStrippingInputShaderVariantCount, (uint)compilerDataList.Count, stripTimeMs);
            ShaderStrippingWatcher.OnShaderProcessed(shader, shaderVariant, (uint)compilerDataList.Count, stripTimeMs);

            error = null;
            return true;
        }
    }

    class ShaderVariantStripper : ShaderPreprocessor<Shader, ShaderSnippetData>, IPreprocessShaders
    {
        public int callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> inputData)
        {
            if (!TryStripShaderVariants(shader, snippet, inputData, out var error))
                Debug.LogError(error);
        }
    }

    class ComputeShaderVariantStripper : ShaderPreprocessor<ComputeShader, string>, IPreprocessComputeShaders
    {
        public int callbackOrder => 0;

        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> inputData)
        {
            if (!TryStripShaderVariants(shader, kernelName, inputData, out var error))
                Debug.LogError(error);
        }
    }
}
