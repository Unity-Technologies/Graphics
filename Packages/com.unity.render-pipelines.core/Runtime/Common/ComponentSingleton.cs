namespace UnityEngine.Rendering
{
    // Use this class to get a static instance of a component
    // Mainly used to have a default instance

    /// <summary>
    /// Singleton of a Component class.
    /// </summary>
    /// <typeparam name="TType">Component type.</typeparam>
    public static class ComponentSingleton<TType>
        where TType : Component
    {
        static TType s_Instance = null;
        /// <summary>
        /// Instance of the required component type.
        /// </summary>
        public static TType instance
        {
            get
            {
                if (s_Instance == null)
                {
                    GameObject go = new GameObject("Default " + typeof(TType).Name) { hideFlags = HideFlags.HideAndDontSave };

#if !UNITY_EDITOR
                    GameObject.DontDestroyOnLoad(go);
#endif

                    go.SetActive(false);
                    s_Instance = go.AddComponent<TType>();
                }

                return s_Instance;
            }
        }

        /// <summary>
        /// Release the component singleton.
        /// </summary>
        public static void Release()
        {
            if (s_Instance != null)
            {
                var go = s_Instance.gameObject;
                CoreUtils.Destroy(go);
                s_Instance = null;
            }
        }
    }
}
