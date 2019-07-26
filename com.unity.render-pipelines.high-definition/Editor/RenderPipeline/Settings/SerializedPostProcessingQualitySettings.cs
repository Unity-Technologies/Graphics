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

        public SerializedPostProcessingQualitySettings(SerializedProperty root)
        {
            this.root = root;

            NearBlurSampleCount     = root.Find((GlobalPostProcessingQualitySettings s) => s.NearBlurSampleCount);
            NearBlurMaxRadius       = root.Find((GlobalPostProcessingQualitySettings s) => s.NearBlurMaxRadius);
            FarBlurSampleCount      = root.Find((GlobalPostProcessingQualitySettings s) => s.FarBlurSampleCount);
            FarBlurMaxRadius        = root.Find((GlobalPostProcessingQualitySettings s) => s.FarBlurMaxRadius);
            DoFResolution           = root.Find((GlobalPostProcessingQualitySettings s) => s.Resolution);
            DoFHighFilteringQuality = root.Find((GlobalPostProcessingQualitySettings s) => s.HighQualityFiltering);

        }
    }
}
