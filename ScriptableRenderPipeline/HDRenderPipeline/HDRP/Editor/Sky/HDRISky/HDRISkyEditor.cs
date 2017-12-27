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
        SerializedDataParameter m_SkyHDRI;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<HDRISky>(serializedObject);
            m_SkyHDRI = Unpack(o.Find(x => x.skyHDRI));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_SkyHDRI);

            EditorGUILayout.Space();

            base.CommonSkySettingsGUI();
        }
    }
}
