using UnityEngine.EventSystems;

namespace UnityEngine.Rendering
{
    class DebugUpdater : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RuntimeInit()
        {
            if (!Debug.isDebugBuild || FindObjectOfType<DebugUpdater>() != null)
                return;

            var go = new GameObject { name = "[Debug Updater]" };
            go.AddComponent<DebugUpdater>();

            var es = GameObject.FindObjectOfType<EventSystem>();
            if (es == null)
            {
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
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
            else
            {
                if (Input.touchCount == 3)
                {
                    foreach (var touch in Input.touches)
                    {
                        if (touch.phase == TouchPhase.Began)
                            debugManager.displayRuntimeUI = !debugManager.displayRuntimeUI;
                    }
                }
            }

            if (debugManager.displayRuntimeUI && debugManager.GetAction(DebugAction.ResetAll) != 0.0f)
            {
                debugManager.Reset();
            }
        }
    }
}
