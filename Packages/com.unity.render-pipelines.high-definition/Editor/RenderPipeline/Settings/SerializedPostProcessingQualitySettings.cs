using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedPostProcessingQualitySettings
    {
        public SerializedProperty root;

        // DoF
        public SerializedProperty NearBlurSampleCount;
        public SerializedProperty NearBlurMaxRadius;
        public SerializedProperty FarBlurSampleCount;
        public SerializedProperty FarBlurMaxRadius;
        public SerializedProperty DoFResolution;
        public SerializedProperty DoFHighFilteringQuality;
        public SerializedProperty DoFPhysicallyBased;
        public SerializedProperty LimitManualRangeNearBlur;
        public SerializedProperty AdaptiveSamplingWeight;

        // Motion Blur
        public SerializedProperty MotionBlurSampleCount;

        // Bloom
        public SerializedProperty BloomRes;
        public SerializedProperty BloomHighFilteringQuality;
        public SerializedProperty BloomHighPrefilteringQuality;

        // Chromatic Aberration
        public SerializedProperty ChromaticAbMaxSamples;

        public SerializedPostProcessingQualitySettings(SerializedProperty root)
        {
            this.root = root;

            // DoF
            NearBlurSampleCount = root.Find((GlobalPostProcessingQualitySettings s) => s.NearBlurSampleCount);
            NearBlurMaxRadius = root.Find((GlobalPostProcessingQualitySettings s) => s.NearBlurMaxRadius);
            FarBlurSampleCount = root.Find((GlobalPostProcessingQualitySettings s) => s.FarBlurSampleCount);
            FarBlurMaxRadius = root.Find((GlobalPostProcessingQualitySettings s) => s.FarBlurMaxRadius);
            DoFResolution = root.Find((GlobalPostProcessingQualitySettings s) => s.DoFResolution);
            DoFHighFilteringQuality = root.Find((GlobalPostProcessingQualitySettings s) => s.DoFHighQualityFiltering);
            DoFPhysicallyBased = root.Find((GlobalPostProcessingQualitySettings s) => s.DoFPhysicallyBased);
            LimitManualRangeNearBlur = root.Find((GlobalPostProcessingQualitySettings s) => s.LimitManualRangeNearBlur);
            AdaptiveSamplingWeight = root.Find((GlobalPostProcessingQualitySettings s) => s.AdaptiveSamplingWeight);

            // Motion Blur
            MotionBlurSampleCount = root.Find((GlobalPostProcessingQualitySettings s) => s.MotionBlurSampleCount);

            // Bloom
            BloomRes = root.Find((GlobalPostProcessingQualitySettings s) => s.BloomRes);
            BloomHighFilteringQuality = root.Find((GlobalPostProcessingQualitySettings s) => s.BloomHighQualityFiltering);
            BloomHighPrefilteringQuality = root.Find((GlobalPostProcessingQualitySettings s) => s.BloomHighQualityPrefiltering);

            // Chromatic Aberration
            ChromaticAbMaxSamples = root.Find((GlobalPostProcessingQualitySettings s) => s.ChromaticAberrationMaxSamples);
        }
    }
}
