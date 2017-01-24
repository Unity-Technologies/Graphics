#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class SkyParametersSingleton : Singleton<SkyParametersSingleton>
    {
        private SkyParameters settings { get; set; }

        public static SkyParameters overrideSettings
        {
            get { return instance.settings; }
            set { instance.settings = value; }
        }
    }
}
