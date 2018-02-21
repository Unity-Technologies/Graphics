namespace UnityEngine.Experimental.Rendering
{
    public class DebugUpdater : MonoBehaviour
    {
        void Update()
        {
            DebugManager.instance.UpdateActions();

            if (DebugManager.instance.GetAction(DebugAction.EnableDebugMenu) != 0.0f)
                DebugManager.instance.displayRuntimeUI = !DebugManager.instance.displayRuntimeUI;
        }
    }
}
