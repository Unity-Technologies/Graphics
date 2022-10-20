using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ContactShadows))]
    class ContactShadowsEditor : VolumeComponentWithQualityEditor
    {
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_Length;
        SerializedDataParameter m_DistanceScaleFactor;
        SerializedDataParameter m_MaxDistance;
        SerializedDataParameter m_MinDistance;
        SerializedDataParameter m_FadeDistance;
        SerializedDataParameter m_FadeInDistance;
        SerializedDataParameter m_SampleCount;
        SerializedDataParameter m_Opacity;
        SerializedDataParameter m_Bias;
        SerializedDataParameter m_Thickness;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ContactShadows>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_Length = Unpack(o.Find(x => x.length));
            m_DistanceScaleFactor = Unpack(o.Find(x => x.distanceScaleFactor));
            m_MaxDistance = Unpack(o.Find(x => x.maxDistance));
            m_MinDistance = Unpack(o.Find(x => x.minDistance));
            m_FadeDistance = Unpack(o.Find(x => x.fadeDistance));
            m_FadeInDistance = Unpack(o.Find(x => x.fadeInDistance));
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_Opacity = Unpack(o.Find(x => x.opacity));
            m_Bias = Unpack(o.Find(x => x.rayBias));
            m_Thickness = Unpack(o.Find(x => x.thicknessScale));

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Enable, EditorGUIUtility.TrTextContent("State", "When enabled, HDRP processes Contact Shadows for this Volume."));

            if (!m_Enable.value.hasMultipleDifferentValues)
            {
                PropertyField(m_Length, EditorGUIUtility.TrTextContent("Length", "Controls the length of the rays HDRP uses to calculate Contact Shadows. Uses meters."));
                PropertyField(m_DistanceScaleFactor, EditorGUIUtility.TrTextContent("Distance Scale Factor", "Dampens the scale up effect HDRP process with distance from the Camera."));
                m_MinDistance.value.floatValue = Mathf.Clamp(m_MinDistance.value.floatValue, 0.0f, m_MaxDistance.value.floatValue);
                PropertyField(m_MinDistance, EditorGUIUtility.TrTextContent("Min Distance", "Sets the distance from the camera at which HDRP begins to fade in Contact Shadows. Uses meters."));
                PropertyField(m_MaxDistance, EditorGUIUtility.TrTextContent("Max Distance", "Sets the distance from the Camera at which HDRP begins to fade out Contact Shadows. Uses meters."));
                m_FadeInDistance.value.floatValue = Mathf.Clamp(m_FadeInDistance.value.floatValue, 0.0f, m_MaxDistance.value.floatValue);
                PropertyField(m_FadeInDistance, EditorGUIUtility.TrTextContent("Fade In Distance", "Sets the distance over which HDRP fades Contact Shadows in when past the Min Distance. Uses meters."));
                PropertyField(m_FadeDistance, EditorGUIUtility.TrTextContent("Fade Out Distance", "Sets the distance over which HDRP fades Contact Shadows out when at the Max Distance. Uses meters."));
                PropertyField(m_Opacity, EditorGUIUtility.TrTextContent("Opacity", "Controls the opacity of the Contact Shadow."));
                PropertyField(m_Bias, EditorGUIUtility.TrTextContent("Bias", "Controls the bias applied to the screen space ray cast to get contact shadows. Increasing this value can help with self-intersection issues, but can lead to detached contact shadows."));
                PropertyField(m_Thickness, EditorGUIUtility.TrTextContent("Thickness", "Controls the thickness of the objects found along the ray, essentially thickening the contact shadows."));

                base.OnInspectorGUI();

                using (new IndentLevelScope())
                using (new QualityScope(this))
                {
                    PropertyField(m_SampleCount, EditorGUIUtility.TrTextContent("Sample Count", "Controls the number of samples HDRP uses for ray casting."));
                }
            }
        }

        public override QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob settings = null)
        {
            if (settings == null)
                settings = new QualitySettingsBlob();

            settings.Save<int>(m_SampleCount);

            return settings;
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            settings.TryLoad<int>(ref m_SampleCount);
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            CopySetting(ref m_SampleCount, settings.lightingQualitySettings.ContactShadowSampleCount[level]);
        }
    }
}
