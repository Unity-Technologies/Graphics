#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class SkySettingsSingleton : Singleton<SkySettingsSingleton>
    {
        private SkySettings settings { get; set; }

        public static SkySettings overrideSettings
        {
            get { return instance.settings; }
            set { instance.settings = value; }
        }
    }
}
