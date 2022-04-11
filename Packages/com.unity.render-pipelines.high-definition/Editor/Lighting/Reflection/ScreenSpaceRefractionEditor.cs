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
            PropertyField(m_ScreenFadeDistance, EditorGUIUtility.TrTextContent("Screen Weight Distance"));
        }
    }
}
