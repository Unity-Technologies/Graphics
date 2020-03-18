using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Shadowing/Contact Shadows")]
    public class ContactShadows : VolumeComponentWithQuality
    {
        // Contact shadows
        public BoolParameter                enable = new BoolParameter(false);
        public ClampedFloatParameter        length = new ClampedFloatParameter(0.15f, 0.0f, 1.0f);
        public ClampedFloatParameter        opacity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter        distanceScaleFactor = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public MinFloatParameter            maxDistance = new MinFloatParameter(50.0f, 0.0f);
        public MinFloatParameter            fadeDistance = new MinFloatParameter(5.0f, 0.0f);
        public int sampleCount
        {
            get
            {
                if (!UsesQualitySettings())
                {
                    return m_SampleCount.value;
                }
                else
                {
                    int qualityLevel = (int)quality.value;
                    return GetLightingQualitySettings().ContactShadowSampleCount[qualityLevel];
                }
            }
            set { m_SampleCount.value = value; }
        }

        [SerializeField, FormerlySerializedAs("sampleCount")]
        private NoInterpClampedIntParameter m_SampleCount = new NoInterpClampedIntParameter(8, 4, 64);

        ContactShadows()
        {
            displayName = "Contact Shadows";
        }
    }
}
