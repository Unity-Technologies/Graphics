namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A base class for volume components that use quality settings and HDRP scalable settings to adjust their behavior based on performance tiers.
    /// </summary>
    /// <remarks>
    /// This class is designed for volume components that need to change their parameters based on the selected quality level and scalable settings
    /// </remarks>
    public abstract class VolumeComponentWithQuality : VolumeComponent
    {
        /// <summary>
        /// Specifies the quality level to be used for performance-relevant parameters. The quality level will adjust
        /// the component's behavior based on the selected setting, which helps to optimize performance across different
        /// hardware configurations.
        /// </summary>
        /// <remarks>
        /// This parameter allows the user to specify the quality tier (e.g., Low, Medium, High) for specific components
        /// that are performance-sensitive. By modifying this parameter, you can tailor the visual fidelity of the component
        /// to meet performance requirements.
        /// </remarks>
        [Tooltip("Specifies the quality level to be used for performance relevant parameters.")]
        [InspectorName("Tier")]
        public ScalableSettingLevelParameter quality = new ScalableSettingLevelParameter((int)ScalableSettingLevelParameter.Level.Medium, false);

        /// <summary>
        /// Retrieves the post-processing quality settings from the current pipeline, if available.
        /// </summary>
        /// <remarks>
        /// This method checks the active render pipeline and returns the post-processing quality settings, which determine
        /// how post-processing effects are applied depending on the selected quality level.
        /// </remarks>
        /// <returns>The <see cref="GlobalPostProcessingQualitySettings"/> object, or null if unavailable.</returns>
        static internal GlobalPostProcessingQualitySettings GetPostProcessingQualitySettings()
        {
            var pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            if (pipeline != null)
            {
                return pipeline.currentPlatformRenderPipelineSettings.postProcessQualitySettings;
            }
            // This shouldn't happen ever.
            return null;
        }

        /// <summary>
        /// Retrieves the lighting quality settings from the current pipeline, if available.
        /// </summary>
        /// <remarks>
        /// This method retrieves the lighting quality settings from the active render pipeline. These settings control
        /// how lighting is processed and rendered depending on the quality level.
        /// </remarks>
        /// <returns>The <see cref="GlobalLightingQualitySettings"/> object, or null if unavailable.</returns>
        static internal GlobalLightingQualitySettings GetLightingQualitySettings()
        {
            var pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            if (pipeline != null)
            {
                return pipeline.currentPlatformRenderPipelineSettings.lightingQualitySettings;
            }
            // This shouldn't happen ever.
            return null;
        }

        /// <summary>
        /// Determines if the component is using parameters from the quality settings.
        /// </summary>
        /// <remarks>
        /// This method checks whether the component uses the current quality settings or whether it is overridden by
        /// a custom setting. If the component uses the default quality settings, it will return true.
        /// </remarks>
        /// <returns>True if the component uses the quality settings; otherwise, false.</returns>
        /// <example><code>
        /// // Example of usage:
        /// if (UsesQualitySettings())
        /// {
        ///     // Adjust parameters based on quality settings
        /// }
        /// </code></example>
        protected bool UsesQualitySettings()
        {
            return !quality.levelAndOverride.useOverride && (HDRenderPipeline)RenderPipelineManager.currentPipeline != null;
        }
    }

}
