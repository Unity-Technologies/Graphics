using UnityEngine.SceneManagement;

namespace UnityEngine.Experimental.Rendering
{
    [ExecuteInEditMode]
    public abstract class Singleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T theInstance { get; set; }
        
        protected static T instance
        {
            get
            {
                LoadAsset();
                return theInstance;
            }
        }

        protected virtual void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
        }
        
        protected virtual void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
        }

        protected abstract void SceneManagerOnActiveSceneChanged(Scene arg0, Scene arg1);

        static void LoadAsset()
        {
            if (!theInstance)
            {
                theInstance = CreateInstance<T>();
                theInstance.hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }
}
