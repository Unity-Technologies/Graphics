namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class SkySettingsSingleton : Singleton<SkySettingsSingleton>
    {
        SkySettings m_Settings { get; set; }

        public static SkySettings overrideSettings
        {
            get { return instance.m_Settings; }
            set { instance.m_Settings = value; }
        }
    }
}
