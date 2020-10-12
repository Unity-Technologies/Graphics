using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public abstract class SkySettingsEditor : VolumeComponentEditor
    {
        [System.Flags]
        protected enum SkySettingsUIElement
        {
            SkyIntensity = 1 << 0,
            Rotation = 1 << 1,
            UpdateMode = 1 << 2,
            //IncludeSunInBaking = 1 << 3, // TODO
        }
        protected uint m_CommonUIElementsMask = 0xFFFFFFFF;
        protected bool m_EnableLuxIntensityMode = false;

        GUIContent m_SkyIntensityModeLabel = new UnityEngine.GUIContent("Intensity Mode");
        GUIContent[] m_IntensityModes = { new GUIContent("Exposure"), new GUIContent("Multiplier"), new GUIContent("Lux") };
        int[] m_IntensityModeValues = { (int)SkyIntensityMode.Exposure, (int)SkyIntensityMode.Multiplier, (int)SkyIntensityMode.Lux };
        GUIContent[] m_IntensityModesNoLux = { new GUIContent("Exposure"), new GUIContent("Multiplier") };
        int[] m_IntensityModeValuesNoLux = { (int)SkyIntensityMode.Exposure, (int)SkyIntensityMode.Multiplier };

        // TODO SerializedDataParameter
        SerializedDataParameter m_IntensityMode;
        SerializedDataParameter m_SkyExposure;
        SerializedDataParameter m_SkyMultiplier;
        SerializedDataParameter m_DesiredLuxValue;
        SerializedDataParameter m_SkyRotation;
        SerializedDataParameter m_EnvUpdateMode;
        SerializedDataParameter m_EnvUpdatePeriod;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SkySettings>(serializedObject);
            // TODO SerializedDataParameter
            m_IntensityMode = Unpack(o.Find(x => x.skyIntensityMode));
            m_SkyExposure = Unpack(o.Find(x => x.exposure));
            m_SkyMultiplier = Unpack(o.Find(x => x.multiplier));
            m_DesiredLuxValue = Unpack(o.Find(x => x.desiredLuxValue));
            m_SkyRotation = Unpack(o.Find(x => x.rotation));
            m_EnvUpdateMode = Unpack(o.Find(x => x.updateMode));
            m_EnvUpdatePeriod = Unpack(o.Find(x => x.updatePeriod));
        }

        protected void CommonSkySettingsGUI()
        {
            if ((m_CommonUIElementsMask & (uint)SkySettingsUIElement.SkyIntensity) != 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawOverrideCheckbox(m_IntensityMode);
                    using (new EditorGUI.DisabledScope(!m_IntensityMode.overrideState.boolValue))
                    {
                        if (m_EnableLuxIntensityMode)
                        {
                            m_IntensityMode.value.intValue = EditorGUILayout.IntPopup(m_SkyIntensityModeLabel, (int)m_IntensityMode.value.intValue, m_IntensityModes, m_IntensityModeValues);
                        }
                        else
                        {
                            m_IntensityMode.value.intValue = EditorGUILayout.IntPopup(m_SkyIntensityModeLabel, (int)m_IntensityMode.value.intValue, m_IntensityModesNoLux, m_IntensityModeValuesNoLux);
                        }
                    }
                }

                EditorGUI.indentLevel++;
                switch (m_IntensityMode.value.GetEnumValue<SkyIntensityMode>())
                {
                    case SkyIntensityMode.Exposure:
                        PropertyField(m_SkyExposure);
                        break;
                    case SkyIntensityMode.Multiplier:
                        PropertyField(m_SkyMultiplier);
                        break;
                    case SkyIntensityMode.Lux:
                        PropertyField(m_DesiredLuxValue);
                        // TODO Helpbox
                        break;
                }
                EditorGUI.indentLevel--;
            }

            if ((m_CommonUIElementsMask & (uint)SkySettingsUIElement.UpdateMode) != 0)
            {
                PropertyField(m_SkyRotation);
            }

            if ((m_CommonUIElementsMask & (uint)SkySettingsUIElement.UpdateMode) != 0)
            {
                PropertyField(m_EnvUpdateMode);

                if (!m_EnvUpdateMode.value.hasMultipleDifferentValues && m_EnvUpdateMode.value.intValue == (int)EnvironmentUpdateMode.Realtime)
                {
                    EditorGUI.indentLevel++;
                    PropertyField(m_EnvUpdatePeriod);
                    EditorGUI.indentLevel--;
                }
            }

            // TODO Sun in baking
        }
    }
}
