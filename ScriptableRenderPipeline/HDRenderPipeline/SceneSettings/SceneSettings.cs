using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public class SceneSettings : MonoBehaviour
    {
        public CommonSettings commonSettings
        {
            set { m_CommonSettings = value; }
            get { return m_CommonSettings; }
        }

        public SkySettings skySettings
        {
            set { m_SkySettings = value; }
            get { return m_SkySettings; }
        }

        [SerializeField] private CommonSettings m_CommonSettings = null;
        [SerializeField] private SkySettings    m_SkySettings = null;

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
            if (SceneSettingsManager.instance.GetCurrentSceneSetting())
            {
                SceneSettingsManager.instance.UpdateCurrentSceneSetting();
            }
        }
    }
}
