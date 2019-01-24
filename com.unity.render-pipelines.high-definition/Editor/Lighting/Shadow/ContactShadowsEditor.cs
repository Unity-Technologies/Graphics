using UnityEditor;
using UnityEditor.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ContactShadows))]
    public class ContactShadowsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_Length;
        SerializedDataParameter m_DistanceScaleFactor;
        SerializedDataParameter m_MaxDistance;
        SerializedDataParameter m_FadeDistance;
        SerializedDataParameter m_SampleCount;
        SerializedDataParameter m_Opacity;


        public override void OnEnable()
        {
            var o = new PropertyFetcher<ContactShadows>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_Length = Unpack(o.Find(x => x.length));
            m_DistanceScaleFactor = Unpack(o.Find(x => x.distanceScaleFactor));
            m_MaxDistance = Unpack(o.Find(x => x.maxDistance));
            m_FadeDistance = Unpack(o.Find(x => x.fadeDistance));
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_Opacity = Unpack(o.Find(x => x.opacity));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Enable, EditorGUIUtility.TrTextContent("Enable"));

            if (!m_Enable.value.hasMultipleDifferentValues)
            {
                using (new EditorGUI.DisabledGroupScope(!m_Enable.value.boolValue))
                {
                    PropertyField(m_Length, EditorGUIUtility.TrTextContent("Length", "Length of rays used to gather contact shadows in world units."));
                    PropertyField(m_DistanceScaleFactor, EditorGUIUtility.TrTextContent("Distance Scale Factor", "Contact Shadows are scaled up with distance. Use this parameter to dampen this effect."));
                    PropertyField(m_MaxDistance, EditorGUIUtility.TrTextContent("Max Distance", "Distance from the camera in world units at which contact shadows are faded out to zero."));
                    PropertyField(m_FadeDistance, EditorGUIUtility.TrTextContent("Fade Distance", "Distance in world units over which the contact shadows fade out (see Max Distance)."));
                    PropertyField(m_SampleCount, EditorGUIUtility.TrTextContent("Sample Count", "Number of samples when ray casting."));
                    PropertyField(m_Opacity, EditorGUIUtility.TrTextContent("Opacity", "Opacity of the resulting contact shadow."));
                }
            }
        }
    }
}
