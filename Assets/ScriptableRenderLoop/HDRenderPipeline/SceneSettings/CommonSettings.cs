using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class CommonSettings : MonoBehaviour
    {
        [Serializable]
        public class Settings
        {
            [SerializeField]
            string m_SkyRendererTypeName = ""; // Serialize a string because serialize a Type.

            [SerializeField]
            float m_ShadowMaxDistance = ShadowSettings.Default.maxShadowDistance;

            [SerializeField]
            int m_ShadowCascadeCount = ShadowSettings.Default.directionalLightCascadeCount;

            [SerializeField]
            float m_ShadowCascadeSplit0 = ShadowSettings.Default.directionalLightCascades.x;

            [SerializeField]
            float m_ShadowCascadeSplit1 = ShadowSettings.Default.directionalLightCascades.y;

            [SerializeField]
            float m_ShadowCascadeSplit2 = ShadowSettings.Default.directionalLightCascades.z;
            
            public float shadowMaxDistance { set { m_ShadowMaxDistance = value; OnValidate(); } get { return m_ShadowMaxDistance; } }
            public int shadowCascadeCount { set { m_ShadowCascadeCount = value; OnValidate(); } get { return m_ShadowCascadeCount; } }
            public float shadowCascadeSplit0 { set { m_ShadowCascadeSplit0 = value; OnValidate(); } get { return m_ShadowCascadeSplit0; } }
            public float shadowCascadeSplit1 { set { m_ShadowCascadeSplit1 = value; OnValidate(); } get { return m_ShadowCascadeSplit1; } }
            public float shadowCascadeSplit2 { set { m_ShadowCascadeSplit2 = value; OnValidate(); } get { return m_ShadowCascadeSplit2; } }

            public void OnValidate()
            {
                m_ShadowMaxDistance = Mathf.Max(0.0f, m_ShadowMaxDistance);
                m_ShadowCascadeCount = Math.Min(4, Math.Max(1, m_ShadowCascadeCount));
                m_ShadowCascadeSplit0 = Mathf.Min(1.0f, Mathf.Max(0.0f, m_ShadowCascadeSplit0));
                m_ShadowCascadeSplit1 = Mathf.Min(1.0f, Mathf.Max(0.0f, m_ShadowCascadeSplit1));
                m_ShadowCascadeSplit2 = Mathf.Min(1.0f, Mathf.Max(0.0f, m_ShadowCascadeSplit2));
            }
        }

        [SerializeField]
        private Settings m_Settings = new Settings();

        public Settings settings
        {
            get { return m_Settings; }
        }

        public void OnEnable()
        {
            CommonSettingsSingleton.Refresh();
        }

        public void OnDisable()
        {
            CommonSettingsSingleton.Refresh();
        }

        void OnValidate()
        {
            if (settings != null)
                settings.OnValidate();
        }

       /* public Type skyRendererType
        {
            set { settings.m_SkyRendererTypeName = value != null ? value.FullName : ""; OnSkyRendererChanged(); }
            get { return settings.m_SkyRendererTypeName == "" ? null : Assembly.GetAssembly(typeof(CommonSettings)).GetType(settings.m_SkyRendererTypeName); }
        }*/


    }


 /*       void OnSkyRendererChanged()
        {
            HDRenderPipeline renderPipeline = Utilities.GetHDRenderPipeline();
            if (renderPipeline == null)
            {
                return;
            }

            renderPipeline.InstantiateSkyRenderer(skyRendererType);

            List<SkyParameters> result = new List<SkyParameters>();
            gameObject.GetComponents<SkyParameters>(result);

            Type skyParamType = renderPipeline.GetSkyParameterType();

            // Disable all incompatible sky parameters and enable the compatible one
            bool found = false;
            foreach (SkyParameters param in result)
            {
                if (param.GetType() == skyParamType)
                {
                    // This is a workaround the fact that we can't control the order in which components are initialized.
                    // So it can happen that a given SkyParameter is OnEnabled before the CommonSettings and so fail the setup because the SkyRenderer is not yet initialized.
                    // So we disable it to for OnEnable to be called again.
                    param.enabled = false;

                    param.enabled = true;
                    found = true;
                }
                else
                {
                    param.enabled = false;
                }
            }

            // If it does not exist, create the parameters
            if (!found && skyParamType != null)
            {
                gameObject.AddComponent(skyParamType);
            }
        }
    }*/
}
