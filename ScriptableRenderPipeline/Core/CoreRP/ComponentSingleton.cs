namespace UnityEngine.Experimental.Rendering
{
    // Use this class to get a static instance of a component
    // Mainly used to have a default instance
    public static class ComponentSingleton<TType>
        where TType : Component
    {
        static TType s_Instance = null;
        public static TType instance
        {
            get
            {
                return s_Instance ?? (s_Instance = new GameObject("Default " + typeof(TType))
                {
                    hideFlags = HideFlags.HideAndDontSave
                }.AddComponent<TType>());
            }
        }
    }
}
