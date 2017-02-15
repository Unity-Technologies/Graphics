using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public class SceneSettings : MonoBehaviour
    {
        public CommonSettings SceneCommonSettings
        {
            get { return m_CommonSettings; }
            set { m_CommonSettings = value; ApplySettings(); }
        }

        public SkyParameters SceneSkyParameters
        {
            get { return m_SkyParameters; }
            set { m_SkyParameters = value; ApplySettings(); }
        }

        [SerializeField]
        private CommonSettings m_CommonSettings;
        [SerializeField]
        private SkyParameters m_SkyParameters;

        // Use this for initialization
        void OnEnable()
        {
            ApplySettings();
        }

        void OnValidate()
        {
            ApplySettings();
        }

        private void ApplySettings()
        {
            if (m_CommonSettings != null)
                CommonSettingsSingleton.overrideSettings = m_CommonSettings;

            if (m_SkyParameters != null)
                SkyParametersSingleton.overrideSettings = m_SkyParameters;
        }
    }
}
