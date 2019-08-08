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

        // Motion Blur
        public SerializedProperty MotionBlurSampleCount;

        public SerializedPostProcessingQualitySettings(SerializedProperty root)
        {
            this.root = root;

            // DoF
            NearBlurSampleCount     = root.Find((GlobalPostProcessingQualitySettings s) => s.NearBlurSampleCount);
            NearBlurMaxRadius       = root.Find((GlobalPostProcessingQualitySettings s) => s.NearBlurMaxRadius);
            FarBlurSampleCount      = root.Find((GlobalPostProcessingQualitySettings s) => s.FarBlurSampleCount);
            FarBlurMaxRadius        = root.Find((GlobalPostProcessingQualitySettings s) => s.FarBlurMaxRadius);
            DoFResolution           = root.Find((GlobalPostProcessingQualitySettings s) => s.Resolution);
            DoFHighFilteringQuality = root.Find((GlobalPostProcessingQualitySettings s) => s.HighQualityFiltering);

            // Motion Blur
            MotionBlurSampleCount   = root.Find((GlobalPostProcessingQualitySettings s) => s.MotionBlurSampleCount);
        }
    }
}
