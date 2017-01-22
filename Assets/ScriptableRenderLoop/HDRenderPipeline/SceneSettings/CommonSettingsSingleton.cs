using System.Linq;
using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class CommonSettingsSingleton : Singleton<CommonSettingsSingleton>
    {
        private CommonSettings.Settings settings { get; set; }

        public static CommonSettings.Settings overrideSettings
        {
            get { return instance.settings; }
        }

        protected override void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
        }

        protected override void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
        }

        protected override void SceneManagerOnActiveSceneChanged(Scene from, Scene to)
        {
            Refresh();
        }

        public static void Refresh()
        {
            instance.settings = null;

            //TODO: Slow, and linq, make good and fast
            var overrideSettings = FindObjectsOfType<CommonSettings>().Where(x => x.isActiveAndEnabled && x.gameObject.scene == SceneManager.GetActiveScene());
            if (overrideSettings.Any())
                instance.settings = overrideSettings.FirstOrDefault().settings;
        }

    }
}
