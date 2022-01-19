using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

#if PROFILE_BUILD
using UnityEngine.Profiling;
#endif

namespace UnityEditor.Rendering
{
    internal abstract class ShaderPreprocessor<TShader, TShaderVariant> : IPostprocessBuildWithReport
        where TShader : UnityEngine.Object
    {
        private readonly IStrippingReport<TShader, TShaderVariant> m_Report;

        IVariantStripper<TShader, TShaderVariant>[] strippers { get; }

        /// <summary>
        /// Constructor that fetch all the IVariantStripper defined on the assemblies
        /// </summary>
        protected ShaderPreprocessor()
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
                    System.Diagnostics.Debug.Assert(stripperInstance != null, nameof(stripperInstance) + " != null");
                    if (stripperInstance.isActive)
                        validStrippers.Add(stripperInstance);
                }
            }

            // Sort them by priority
            strippers = validStrippers
                .OrderByDescending(spp => spp.priority)
                .ToArray();

            Debug.Log($"Found {strippers.Length} `{typeof(TShader)} {string.Join(Environment.NewLine, strippers.Select(s => s.GetType().FullName))}");

            m_Report = StrippingReportFactory.CreateReport<TShader, TShaderVariant>();
        }

        #region IPostprocessBuildWithReport
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            var file = $"Temp/{typeof(TShader).Name}-strip.json";
            m_Report.DumpReport(file);
        }
        #endregion

        /// <summary>
        /// Strips the given <see cref="TShader" />
        /// </summary>
        /// <param name="shader">The <see cref="T" /> that might be stripped.</param>
        /// <param name="shaderVariant">The <see cref="TShaderVariant" /></param>
        /// <param name="compilerDataList">A list of <see cref="ShaderCompilerData" /></param>
        protected unsafe bool StripShaderVariants(
            [NotNull] TShader shader,
            TShaderVariant shaderVariant,
            IList<ShaderCompilerData> compilerDataList)
        {
#if PROFILE_BUILD
            Profiler.BeginSample(nameof(StripShaderVariants));
#endif
            if (shader == null || compilerDataList == null)
                return false;

            var beforeStrippingInputShaderVariantCount = compilerDataList.Count;

            // Early exit from the stripping
            if (beforeStrippingInputShaderVariantCount == 0)
                return true;

            var afterStrippingShaderVariantCount = beforeStrippingInputShaderVariantCount;

            double stripTimeMs = 0;
            using (TimedScope.FromPtr(&stripTimeMs))
            {
                // Go through all the shader variants
                for (var i = 0; i < afterStrippingShaderVariantCount;)
                {
                    // By default, all variants are stripped if there are not strippers using it
                	// Note that all strippers cumulate each other, so be aware of any conflict here
                    var canRemoveVariant = strippers
                        .Where(stripper => stripper.CanProcessVariant(shader, shaderVariant))
                        .All(stripper => stripper.CanRemoveVariant(shader, shaderVariant, compilerDataList[i]));

                    // Remove at swap back
                    if (canRemoveVariant)
                        compilerDataList[i] = compilerDataList[--afterStrippingShaderVariantCount];
                    else
                        ++i;
                }

                // Remove the shader variants that will be at the back
                compilerDataList.RemoveBack(beforeStrippingInputShaderVariantCount - afterStrippingShaderVariantCount);
            }

            var outputVariantCount = compilerDataList.Count;
            m_Report.OnShaderProcessed(shader, shaderVariant, beforeStrippingInputShaderVariantCount, outputVariantCount, stripTimeMs);

#if PROFILE_BUILD
            Profiler.EndSample();
#endif
            return true;
        }
    }
}
