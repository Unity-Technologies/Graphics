using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract class SkySettingsEditor : VolumeComponentEditor
    {
        [System.Flags]
        protected enum SkySettingsUIElement
        {
            Exposure = 1 << 0,
            Multiplier = 1 << 1,
            Rotation = 1 << 2,
            UpdateMode = 1 << 3,
            IncludeSunInBaking = 1 << 4
        }

        SerializedDataParameter m_SkyExposure;
        SerializedDataParameter m_SkyMultiplier;
        SerializedDataParameter m_SkyRotation;
        SerializedDataParameter m_EnvUpdateMode;
        SerializedDataParameter m_EnvUpdatePeriod;
        SerializedDataParameter m_IncludeSunInBaking;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SkySettings>(serializedObject);

            m_SkyExposure = Unpack(o.Find(x => x.exposure));
            m_SkyMultiplier = Unpack(o.Find(x => x.multiplier));
            m_SkyRotation = Unpack(o.Find(x => x.rotation));
            m_EnvUpdateMode = Unpack(o.Find(x => x.updateMode));
            m_EnvUpdatePeriod = Unpack(o.Find(x => x.updatePeriod));
            m_IncludeSunInBaking = Unpack(o.Find(x => x.includeSunInBaking));

        }

        protected void CommonSkySettingsGUI(uint uiElementMask = 0xFFFFFFFF)
        {
            if ((uiElementMask & (uint)SkySettingsUIElement.Exposure) != 0)
                PropertyField(m_SkyExposure);
            if ((uiElementMask & (uint)SkySettingsUIElement.Multiplier) != 0)
                PropertyField(m_SkyMultiplier);
            if ((uiElementMask & (uint)SkySettingsUIElement.Rotation) != 0)
                PropertyField(m_SkyRotation);

            if ((uiElementMask & (uint)SkySettingsUIElement.UpdateMode) != 0)
            {
                PropertyField(m_EnvUpdateMode);
                if (!m_EnvUpdateMode.value.hasMultipleDifferentValues && m_EnvUpdateMode.value.intValue == (int)EnvironementUpdateMode.Realtime)
                {
                    EditorGUI.indentLevel++;
                    PropertyField(m_EnvUpdatePeriod);
                    EditorGUI.indentLevel--;
                }
            }
            if ((uiElementMask & (uint)SkySettingsUIElement.IncludeSunInBaking) != 0)
                PropertyField(m_IncludeSunInBaking);
        }
    }
}
