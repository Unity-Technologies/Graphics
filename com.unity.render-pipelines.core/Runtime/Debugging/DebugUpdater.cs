namespace UnityEngine.Rendering
{
    class DebugUpdater : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RuntimeInit()
        {
            if (!Debug.isDebugBuild || !DebugManager.instance.enableRuntimeUI || FindObjectOfType<DebugUpdater>() != null)
                return;

            var go = new GameObject { name = "[Debug Updater]" };
            go.AddComponent<DebugUpdater>();
            DontDestroyOnLoad(go);
        }

        void Update()
        {
            DebugManager.instance.UpdateActions();

            if (DebugManager.instance.GetAction(DebugAction.EnableDebugMenu) != 0.0f)
                DebugManager.instance.displayRuntimeUI = !DebugManager.instance.displayRuntimeUI;

            if (DebugManager.instance.displayRuntimeUI && DebugManager.instance.GetAction(DebugAction.ResetAll) != 0.0f)
                DebugManager.instance.Reset();
        }
    }
}
