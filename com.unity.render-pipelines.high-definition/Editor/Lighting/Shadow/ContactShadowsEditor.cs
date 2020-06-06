using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ContactShadows))]
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

        public override void OnEnable()
        {
            base.OnEnable();

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
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Enable, EditorGUIUtility.TrTextContent("Enable", "When enabled, HDRP processes Contact Shadows for this Volume."));

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
                base.OnInspectorGUI();
                GUI.enabled = useCustomValue;
                PropertyField(m_SampleCount, EditorGUIUtility.TrTextContent("Sample Count", "Controls the number of samples HDRP uses for ray casting."));
                GUI.enabled = true;
            }
        }
    }
}
