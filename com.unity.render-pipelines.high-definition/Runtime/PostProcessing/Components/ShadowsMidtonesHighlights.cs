using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Shadows, Midtones, Highlights effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Shadows, Midtones, Highlights")]
    public sealed class ShadowsMidtonesHighlights : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Controls the darkest portions of the render.
        /// </summary>
        [Tooltip("Controls the darkest portions of the render.")]
        public Vector4Parameter shadows = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Controls the power function that handles mid-range tones.
        /// </summary>
        [Tooltip("Controls the power function that handles mid-range tones.")]
        public Vector4Parameter midtones = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Controls the lightest portions of the render.
        /// </summary>
        [Tooltip("Controls the lightest portions of the render.")]
        public Vector4Parameter highlights = new Vector4Parameter(new Vector4(1f, 1f, 1f, 0f));

        /// <summary>
        /// Sets the start point of the transition between shadows and midtones.
        /// </summary>
        [Tooltip("Sets the start point of the transition between shadows and midtones.")]
        public MinFloatParameter shadowsStart = new MinFloatParameter(0f, 0f);

        /// <summary>
        /// Sets the end point of the transition between shadows and midtones.
        /// </summary>
        [Tooltip("Sets the end point of the transition between shadows and midtones.")]
        public MinFloatParameter shadowsEnd = new MinFloatParameter(0.3f, 0f);

        /// <summary>
        /// Sets the start point of the transition between midtones and highlights.
        /// </summary>
        [Tooltip("Sets the start point of the transition between midtones and highlights.")]
        public MinFloatParameter highlightsStart = new MinFloatParameter(0.55f, 0f);

        /// <summary>
        /// Sets the end point of the transition between midtones and highlights.
        /// </summary>
        [Tooltip("Sets the end point of the transition between midtones and highlights.")]
        public MinFloatParameter highlightsEnd = new MinFloatParameter(1f, 0f);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            var defaultState = new Vector4(1f, 1f, 1f, 0f);
            return shadows != defaultState
                || midtones != defaultState
                || highlights != defaultState;
        }

        ShadowsMidtonesHighlights() => displayName = "Shadows, Midtones, Highlights";
    }
}
