namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ScreenSpaceAmbientOcclusionSettingsSingleton : Singleton<ScreenSpaceAmbientOcclusionSettingsSingleton>
    {
        private ScreenSpaceAmbientOcclusionSettings settings { get; set; }

        public static ScreenSpaceAmbientOcclusionSettings overrideSettings
        {
            get { return instance.settings; }
            set { instance.settings = value; }
        }
    }
}
