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
