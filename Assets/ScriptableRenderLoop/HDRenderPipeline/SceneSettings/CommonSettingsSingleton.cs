namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class CommonSettingsSingleton : Singleton<CommonSettingsSingleton>
    {
        private CommonSettings settings { get; set; }

        public static CommonSettings overrideSettings
        {
            get { return instance.settings; }
            set { instance.settings = value; }
        }
    }
}
