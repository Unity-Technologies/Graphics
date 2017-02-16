using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public class SceneSettings : MonoBehaviour
    {
        public CommonSettings commonSettings
        {
            get { return m_CommonSettings; }
        }

        public SkyParameters skyParameters
        {
            get { return m_SkyParameters; }
        }

        [SerializeField] private CommonSettings m_CommonSettings;
        [SerializeField] private SkyParameters  m_SkyParameters;

        // Use this for initialization
        void OnEnable()
        {
            SceneSettingsManager.instance.AddSceneSettings(this);
        }

        void OnDisable()
        {
            SceneSettingsManager.instance.RemoveSceneSettings(this);
        }

        void OnValidate()
        {
            // If the setting is already the one currently used we need to tell the manager to reapply it.
            if(SceneSettingsManager.instance.GetCurrentSceneSetting())
            {
                SceneSettingsManager.instance.UpdateCurrentSceneSetting();
            }
        }

    }
}
