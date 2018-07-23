using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(HDRISky))]
    public class HDRISkyEditor
        : SkySettingsEditor
    {
        SerializedDataParameter m_hdriSky;

        public override void OnEnable()
        {
            base.OnEnable();

            // HDRI sky does not have control over sun display.
            m_CommonUIElementsMask = 0xFFFFFFFF & ~(uint)(SkySettingsUIElement.IncludeSunInBaking);

            var o = new PropertyFetcher<HDRISky>(serializedObject);
            m_hdriSky = Unpack(o.Find(x => x.hdriSky));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_hdriSky);

            EditorGUILayout.Space();
            base.CommonSkySettingsGUI();
        }
    }
}
