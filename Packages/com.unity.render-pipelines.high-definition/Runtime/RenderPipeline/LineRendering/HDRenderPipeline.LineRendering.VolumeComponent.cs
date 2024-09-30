using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for High Quality Line Rendering.
    /// </summary>
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Serializable, VolumeComponentMenu("Rendering/High Quality Lines")]
    public class HighQualityLineRenderingVolumeComponent : VolumeComponent
    {
        /// <summary>
        /// A <see cref="VolumeParameter"/> that holds a Line Rendering Composition Mode value.
        /// </summary>
        [Serializable]
        public sealed class LinesCompositionModeParameter : VolumeParameter<LineRendering.CompositionMode>
        {
            /// <summary>
            /// Creates a new LinesSortingQualityParameter instance.
            /// </summary>
            /// <param name="value">The initial value to store in the parameter.</param>
            /// <param name="overrideState">The initial override state for the parameter.</param>
            public LinesCompositionModeParameter(LineRendering.CompositionMode value, bool overrideState = false) : base(value, overrideState) { }
        }

        /// <summary>
        /// A <see cref="VolumeParameter"/> that holds a Line Rendering Sorting Quality value.
        /// </summary>
        [Serializable]
        public sealed class LinesSortingQualityParameter : VolumeParameter<LineRendering.SortingQuality>
        {
            /// <summary>
            /// Creates a new LinesSortingQualityParameter instance.
            /// </summary>
            /// <param name="value">The initial value to store in the parameter.</param>
            /// <param name="overrideState">The initial override state for the parameter.</param>
            public LinesSortingQualityParameter(LineRendering.SortingQuality value, bool overrideState = false) : base(value, overrideState) { }
        }

        /// <summary>
        ///
        /// </summary>
        public BoolParameter enable = new BoolParameter(false);

        /// <summary>
        /// Determines when in the render pipeline that lines will be composed into the main frame.
        /// </summary>
        [Tooltip("Determines when in the render pipeline that lines will be composed into the main frame.")]
        public LinesCompositionModeParameter compositionMode = new LinesCompositionModeParameter(LineRendering.CompositionMode.BeforeColorPyramid);

        /// <summary>
        /// Sets the number of clusters along the z-axis for high quality line rendering, improves transparent sorting.
        /// </summary>
        [Tooltip("Sets the number of clusters along the z-axis for high quality line rendering, improves transparent sorting.")]
        public ClampedIntParameter clusterCount = new ClampedIntParameter(24, 1, 128);

        /// <summary>
        /// Sets the number of segments that are sorted within a cluster.
        /// </summary>
        [Tooltip("Sets the number of segments that are sorted within a cluster.")]
        public LinesSortingQualityParameter sortingQuality = new LinesSortingQualityParameter(LineRendering.SortingQuality.Low);

        /// <summary>
        /// Threshold for determining what qualifies as an opaque tile for high quality line rendering.
        /// </summary>
        [Tooltip("Threshold for determining what qualifies as an opaque tile for high quality line rendering, lower values improve performance, but lose quality.")]
        public ClampedFloatParameter tileOpacityThreshold = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Depth and motion vectors are written only if the alpha value is above this threshold.
        /// </summary>
        [Tooltip("Threshold for determining when to write depth to output.")]
        public ClampedFloatParameter writeDepthAlphaThreshold = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        HighQualityLineRenderingVolumeComponent()
        {
            displayName = "High Quality Line Rendering";
        }
    }
}
