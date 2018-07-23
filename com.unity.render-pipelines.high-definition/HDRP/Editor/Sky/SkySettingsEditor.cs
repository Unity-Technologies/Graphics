using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract class SkySettingsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_SkyExposure;
        SerializedDataParameter m_SkyMultiplier;
        SerializedDataParameter m_SkyRotation;
        SerializedDataParameter m_EnvUpdateMode;
        SerializedDataParameter m_EnvUpdatePeriod;
        protected SerializedDataParameter m_ShowProperties;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SkySettings>(serializedObject);

            m_ShowProperties = Unpack(o.Find(x => x.showProperties));
            m_SkyExposure = Unpack(o.Find(x => x.exposure));
            m_SkyMultiplier = Unpack(o.Find(x => x.multiplier));
            m_SkyRotation = Unpack(o.Find(x => x.rotation));
            m_EnvUpdateMode = Unpack(o.Find(x => x.updateMode));
            m_EnvUpdatePeriod = Unpack(o.Find(x => x.updatePeriod));
        }

        protected void CommonSkySettingsGUI()
        {
            if ((m_ShowProperties.value.intValue & (int)SkySettingsPropertyFlags.ShowMultiplierAndEV) != 0)
            {
                PropertyField(m_SkyExposure);
                PropertyField(m_SkyMultiplier);
            }

            if ((m_ShowProperties.value.intValue & (int)SkySettingsPropertyFlags.ShowRotation) != 0)
                PropertyField(m_SkyRotation);

            if ((m_ShowProperties.value.intValue & (int)SkySettingsPropertyFlags.ShowUpdateMode) != 0)
            {
                PropertyField(m_EnvUpdateMode);
                if (!m_EnvUpdateMode.value.hasMultipleDifferentValues && m_EnvUpdateMode.value.intValue == (int)EnvironementUpdateMode.Realtime)
                {
                    EditorGUI.indentLevel++;
                    PropertyField(m_EnvUpdatePeriod);
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}