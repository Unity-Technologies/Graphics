using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class SceneSettingsManager
    {
        static private SceneSettingsManager s_Instance = null;
        static public SceneSettingsManager instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new SceneSettingsManager();

                return s_Instance;
            }
        }

        private List<SceneSettings> m_SceneSettingsList = new List<SceneSettings>();

        void OnEnable()
        {
        }

        void OnDisable()
        {
        }

        public SceneSettings GetCurrentSceneSetting()
        {
            if (m_SceneSettingsList.Count == 0)
                return null;
            else
                return m_SceneSettingsList[m_SceneSettingsList.Count - 1];
        }

        // This can be needed in the editor in case the current setting is being changed. In this case we need to reapply it.
        public void UpdateCurrentSceneSetting()
        {
            if (m_SceneSettingsList.Count != 0)
                ApplySettings(GetCurrentSceneSetting());
        }

        public void AddSceneSettings(SceneSettings settings)
        {
            m_SceneSettingsList.Add(settings);
            ApplySettings(settings);
        }

        public void RemoveSceneSettings(SceneSettings settings)
        {
            m_SceneSettingsList.Remove(settings);

            // Always reapply the settings at the top of the list
            // (this way if the setting being removed was the active one we switch to the next one)
            ApplySettings(GetCurrentSceneSetting());
        }

        private void ApplySettings(SceneSettings settings)
        {
            if (settings)
            {
                CommonSettingsSingleton.overrideSettings = settings.commonSettings;
                SkySettingsSingleton.overrideSettings = settings.skySettings;
                ScreenSpaceAmbientOcclusionSettingsSingleton.overrideSettings = settings.ssaoSettings;
            }
            else
            {
                CommonSettingsSingleton.overrideSettings = null;
                SkySettingsSingleton.overrideSettings = null;
                ScreenSpaceAmbientOcclusionSettingsSingleton.overrideSettings = null;
            }
        }
    }
}
