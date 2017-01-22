using System.Linq;
using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class SkyParametersSingleton : Singleton<SkyParametersSingleton>
    {
        private SkyParameters settings { get; set; }

        public static SkyParameters overrideSettings
        {
            get { return instance.settings; }
        }

        protected override void SceneManagerOnActiveSceneChanged(Scene from, Scene to)
        {
            Refresh();
        }

        public static void Refresh()
        {
            instance.settings = null;

            //TODO: Slow, and linq, make good and fast
            var overrideSettings = FindObjectsOfType<SkyParameters>().Where(x => x.isActiveAndEnabled && x.gameObject.scene == SceneManager.GetActiveScene());
            if (overrideSettings.Any())
                instance.settings = overrideSettings.FirstOrDefault();
        }
    }
}
