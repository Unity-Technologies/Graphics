using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ScreenSpaceRefraction))]
    class ScreenSpaceRefractionEditor : VolumeComponentEditor
    {
        protected SerializedDataParameter m_ScreenFadeDistance;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceRefraction>(serializedObject);

            m_ScreenFadeDistance = Unpack(o.Find(x => x.screenFadeDistance));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_ScreenFadeDistance, k_ScreenFadeDistance);
        }

        static public readonly UnityEngine.GUIContent k_ScreenFadeDistance = EditorGUIUtility.TrTextContent("Screen Weight Distance", "Controls the distance at which HDRP fades out the refraction effect when the destination of the ray is near the boundaries of the screen. Increase this value to increase the distance from the screen edge at which HDRP fades out the refraction effect for a ray destination.");
    }
}
