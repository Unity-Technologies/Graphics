using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Contact Shadows effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Shadowing/Contact Shadows")]
    public class ContactShadows : VolumeComponentWithQuality
    {
        /// <summary>
        /// When enabled, HDRP processes Contact Shadows for this Volume.
        /// </summary>
        public BoolParameter                enable = new BoolParameter(false);
        /// <summary>
        /// Controls the length of the rays HDRP uses to calculate Contact Shadows. It is in meters, but it gets scaled by a factor depending on Distance Scale Factor
        /// and the depth of the point from where the contact shadow ray is traced. 
        /// </summary>
        public ClampedFloatParameter        length = new ClampedFloatParameter(0.15f, 0.0f, 1.0f);
        /// <summary>
        /// Controls the opacity of the contact shadows.
        /// </summary>
        public ClampedFloatParameter        opacity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        /// <summary>
        /// Scales the length of the contact shadow ray based on the linear depth value at the origin of the ray.
        /// </summary>
        public ClampedFloatParameter        distanceScaleFactor = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        /// <summary>
        /// The distance from the camera, in meters, at which HDRP begins to fade out Contact Shadows.
        /// </summary>
        public MinFloatParameter            maxDistance = new MinFloatParameter(50.0f, 0.0f);
        /// <summary>
        /// The distance, in meters, over which HDRP fades Contact Shadows out when past the Max Distance.
        /// </summary>
        public MinFloatParameter            fadeDistance = new MinFloatParameter(5.0f, 0.0f);
        /// <summary>
        /// Controls the number of samples HDRP takes along each contact shadow ray. Increasing this value can lead to higher quality.
        /// </summary>
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
