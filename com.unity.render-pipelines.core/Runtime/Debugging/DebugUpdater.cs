namespace UnityEngine.Experimental.Rendering
{
    public class DebugUpdater : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RuntimeInit()
        {
            if (FindObjectOfType<DebugUpdater>() != null)
                return;

            var go = new GameObject { name = "[Debug Updater]" };
            go.AddComponent<DebugUpdater>();
            DontDestroyOnLoad(go);
        }

        void Update()
        {
            DebugManager debugManager = DebugManager.instance;
            
            debugManager.UpdateActions();

            if (debugManager.GetAction(DebugAction.EnableDebugMenu) != 0.0f)
            {
                debugManager.displayRuntimeUI = !debugManager.displayRuntimeUI;
            }
            
            if (debugManager.displayRuntimeUI && debugManager.GetAction(DebugAction.ResetAll) != 0.0f)
            {
                debugManager.Reset();
            }
        }
    }
}
