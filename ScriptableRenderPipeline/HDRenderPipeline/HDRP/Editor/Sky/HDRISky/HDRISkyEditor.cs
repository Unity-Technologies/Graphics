using UnityEngine;
using UnityEditor;
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
