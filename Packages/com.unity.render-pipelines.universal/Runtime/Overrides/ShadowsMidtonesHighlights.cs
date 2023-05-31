using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Shadows, Midtones, Highlights effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Shadows, Midtones, Highlights")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("Post-Processing-Shadows-Midtones-Highlights")]
    public sealed class ShadowsMidtonesHighlights : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Use this to control and apply a hue to the shadows.
        /// </summary>
        public Vector4Parameter shadows = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Use this to control and apply a hue to the midtones.
        /// </summary>
        public Vector4Parameter midtones = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Use this to control and apply a hue to the highlights.
        /// </summary>
        public Vector4Parameter highlights = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Start point of the transition between shadows and midtones.
        /// </summary>
        [Header("Shadow Limits")]
        [Tooltip("Start point of the transition between shadows and midtones.")]
        public MinFloatParameter shadowsStart = new MinFloatParameter(0f, 0f);

        /// <summary>
        /// End point of the transition between shadows and midtones.
        /// </summary>
        [Tooltip("End point of the transition between shadows and midtones.")]
        public MinFloatParameter shadowsEnd = new MinFloatParameter(0.3f, 0f);

        /// <summary>
        /// Start point of the transition between midtones and highlights
        /// </summary>
        [Header("Highlight Limits")]
        [Tooltip("Start point of the transition between midtones and highlights.")]
        public MinFloatParameter highlightsStart = new MinFloatParameter(0.55f, 0f);

        /// <summary>
        /// End point of the transition between midtones and highlights.
        /// </summary>
        [Tooltip("End point of the transition between midtones and highlights.")]
        public MinFloatParameter highlightsEnd = new MinFloatParameter(1f, 0f);

        /// <inheritdoc/>
        public bool IsActive()
        {
            var defaultState = new Vector4(1f, 1f, 1f, 0f);
            return shadows != defaultState
                || midtones != defaultState
                || highlights != defaultState;
        }

        /// <inheritdoc/>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => true;
    }
}
