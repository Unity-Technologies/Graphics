#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
    #define USE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
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
#if USE_INPUT_SYSTEM
                // FIXME: InputSystemUIInputModule has a quirk where the default actions fail to get initialized if the
                // component is initialized while the GameObject is active. So we deactivate it temporarily.
                // See https://fogbugz.unity3d.com/f/cases/1323566/
                go.SetActive(false);
                go.AddComponent<InputSystemUIInputModule>();
                go.SetActive(true);
#else
                go.AddComponent<StandaloneInputModule>();
#endif
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
                        // Gesture: 3-finger double-tap
                        if (touch.phase == TouchPhase.Began && touch.tapCount == 2)
                            debugManager.displayRuntimeUI = !debugManager.displayRuntimeUI;
                    }
                }
            }

            if (debugManager.displayRuntimeUI && debugManager.GetAction(DebugAction.ResetAll) != 0.0f)
            {
                debugManager.Reset();
            }

            if (Input.mouseScrollDelta != Vector2.zero)
                debugManager.SetScrollTarget(null); // Allow mouse wheel scroll without causing auto-scroll
        }
    }
}
