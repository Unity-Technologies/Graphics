using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP
{
    [VolumeComponentEditor(typeof(MotionBlur))]
    sealed class MotionBlurEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Mode;
        SerializedDataParameter m_Quality;
        SerializedDataParameter m_Intensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<MotionBlur>(serializedObject);

            m_Mode = Unpack(o.Find(x => x.mode));
            m_Quality = Unpack(o.Find(x => x.quality));
            m_Intensity = Unpack(o.Find(x => x.intensity));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode);

            if (m_Mode.value.intValue == (int)MotionBlurMode.CameraOnly)
            {
                PropertyField(m_Quality);
                PropertyField(m_Intensity);
            }
            else
            {
                EditorGUILayout.HelpBox("Object motion blur is not supported on the Lightweight Render Pipeline yet.", MessageType.Info);
            }
        }
    }
}
