#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
    #define USE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.EnhancedTouch;
#endif
using System.Collections;
using UnityEngine.EventSystems;

namespace UnityEngine.Rendering
{
    class DebugUpdater : MonoBehaviour
    {
        ScreenOrientation m_Orientation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RuntimeInit()
        {
            if (!Debug.isDebugBuild || FindObjectOfType<DebugUpdater>() != null)
                return;

            var go = new GameObject { name = "[Debug Updater]" };
            var debugUpdater = go.AddComponent<DebugUpdater>();

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
                EnhancedTouchSupport.Enable();
#else
                go.AddComponent<StandaloneInputModule>();
#endif
            }

            debugUpdater.m_Orientation = Screen.orientation;

            DontDestroyOnLoad(go);
        }

        void Update()
        {
            DebugManager debugManager = DebugManager.instance;

            debugManager.UpdateActions();

            if (debugManager.GetAction(DebugAction.EnableDebugMenu) != 0.0f ||
                debugManager.GetActionToggleDebugMenuWithTouch())
            {
                debugManager.displayRuntimeUI = !debugManager.displayRuntimeUI;
            }

            if (debugManager.displayRuntimeUI)
            {
                if (debugManager.GetAction(DebugAction.ResetAll) != 0.0f)
                    debugManager.Reset();

                if (debugManager.GetActionReleaseScrollTarget())
                    debugManager.SetScrollTarget(null); // Allow mouse wheel scroll without causing auto-scroll
            }

            if (m_Orientation != Screen.orientation)
            {
                StartCoroutine(RefreshRuntimeUINextFrame());
                m_Orientation = Screen.orientation;
            }
        }

        static IEnumerator RefreshRuntimeUINextFrame()
        {
            yield return null; // Defer runtime UI refresh to next frame to allow canvas to update first.
            DebugManager.instance.ReDrawOnScreenDebug();
        }
    }
}
