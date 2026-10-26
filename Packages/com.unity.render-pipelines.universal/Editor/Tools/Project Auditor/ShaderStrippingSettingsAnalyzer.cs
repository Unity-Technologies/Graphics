using System.Collections.Generic;
using System.ComponentModel;
using Unity.ProjectAuditor.Editor;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    [Category("Global Settings")]
    class ShaderStrippingSettingsAnalyzer : IRenderingSettingsAnalyzer
    {
        internal const string URP0203 = nameof(URP0203);

        public Descriptor Descriptor { get; } = new Descriptor(
            URP0203,
            "URP: Shader stripping settings could be improved",
            Areas.BuildSize | Areas.BuildTime,
            "One or more URP shader stripping options are disabled in the Additional Shader Stripping Settings (Project Settings > Graphics > URP). Disabling these options keeps shader variants in the build that the project is unlikely to use. This increases build size and shader compilation time.",
            "Enable <b>Strip Unused Variants</b>, <b>Strip Unused Post Processing Variants And Resources</b>, <b>Strip Screen Coord Override Variants</b>, and <b>Strip 2D Unused Variants</b> in Project Settings > Graphics > URP."
        )
        {
            Fixer = FixShaderStripping,
        };

        public IEnumerable<RenderingSettingsIssue> EnumerateIssues()
        {
            // Shader stripping settings only matter when URP is in use. A missing Global Settings asset is
            // reported separately by URP0201, so there is nothing to add here in that case.
            if (UniversalRenderPipelineGlobalSettings.instance == null)
                yield break;
            if (!URPProjectAuditorUtilities.IsURPActiveInProject())
                yield break;

            if (!EditorGraphicsSettings.TryGetRenderPipelineSettingsForPipeline<URPShaderStrippingSetting, UniversalRenderPipeline>(out var settings)
                || !settings.stripUnusedVariants
                || !settings.stripUnusedPostProcessingVariants
                || !settings.stripScreenCoordOverrideVariants
                || !settings.strip2DUnusedVariants)
            {
                yield return new RenderingSettingsIssue(URP0203);
            }
        }

        public static bool FixShaderStripping(ReportItem issue, AnalysisParams context)
        {
            var globalSettings = UniversalRenderPipelineGlobalSettings.instance;
            if (globalSettings == null)
                return false;

            // The container can be absent from older or newly created Global Settings assets. Populate adds
            // any missing IRenderPipelineGraphicsSettings (initialized to their defaults) so we can enable them.
            if (!EditorGraphicsSettings.TryGetRenderPipelineSettingsForPipeline<URPShaderStrippingSetting, UniversalRenderPipeline>(out var settings))
            {
                EditorGraphicsSettings.PopulateRenderPipelineGraphicsSettings(globalSettings);
                if (!EditorGraphicsSettings.TryGetRenderPipelineSettingsForPipeline<URPShaderStrippingSetting, UniversalRenderPipeline>(out settings))
                    return false;
            }

            settings.stripUnusedVariants = true;
            settings.stripUnusedPostProcessingVariants = true;
            settings.stripScreenCoordOverrideVariants = true;
            settings.strip2DUnusedVariants = true;

            EditorUtility.SetDirty(globalSettings);
            return true;
        }
    }
}
