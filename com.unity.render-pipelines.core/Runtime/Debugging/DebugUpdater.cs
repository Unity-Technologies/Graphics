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
            if (!Debug.isDebugBuild || !DebugManager.instance.enableRuntimeUI || FindObjectOfType<DebugUpdater>() != null)
                return;

            EnableRuntime();
        }

        internal static void SetEnabled(bool enabled)
        {
            if (enabled)
                EnableRuntime();
            else
                DisableRuntime();
        }

        static void EnableRuntime()
        {
            var go = new GameObject { name = "[Debug Updater]" };
            var debugUpdater = go.AddComponent<DebugUpdater>();

            var es = FindObjectOfType<EventSystem>();
            if (es == null)
            {
                go.AddComponent<EventSystem>();
#if USE_INPUT_SYSTEM
                // FIXME: InputSystemUIInputModule has a quirk where the default actions fail to get initialized if the
                // component is initialized while the GameObject is active. So we deactivate it temporarily.
                // See https://fogbugz.unity3d.com/f/cases/1323566/
                go.SetActive(false);
                var uiModule = go.AddComponent<InputSystemUIInputModule>();

                // FIXME: In order to activate default input actions in player builds (required for touch input to work),
                // we need to call InputSystemUIInputModule.AssignDefaultActions() which was added in com.unity.inputsystem@1.1.0-pre.5.
                // However, there is a problem in InputSystem package version ordering, where it sorts this version as an
                // older version than it should be. Hence we cannot write a version define to conditionally compile this function call.
                // Instead, we use reflection to see if the function is there and can be invoked.
                //
                // Once com.unity.inputsystem@1.1.0 is available, create an INPUTSYSTEM_1_1_0_OR_GREATER version define and use it
                // to conditionally call AssignDefaultActions().
                System.Reflection.MethodInfo assignDefaultActionsMethod = uiModule.GetType().GetMethod("AssignDefaultActions");
                if (assignDefaultActionsMethod != null)
                {
                    assignDefaultActionsMethod.Invoke(uiModule, null);
                }

                go.SetActive(true);
#else
                go.AddComponent<StandaloneInputModule>();
#endif
            }
            else
            {
#if USE_INPUT_SYSTEM
                if (es.GetComponent<InputSystemUIInputModule>() == null)
                    Debug.LogWarning("Found a game object with EventSystem component but no corresponding InputSystemUIInputModule component - Debug UI input may not work correctly.");
#else
                if (es.GetComponent<StandaloneInputModule>() == null)
                    Debug.LogWarning("Found a game object with EventSystem component but no corresponding StandaloneInputModule component - Debug UI input may not work correctly.");
#endif
            }

#if USE_INPUT_SYSTEM
            EnhancedTouchSupport.Enable();
#endif
            debugUpdater.m_Orientation = Screen.orientation;

            DontDestroyOnLoad(go);

            DebugManager.instance.EnableInputActions();
        }

        static void DisableRuntime()
        {
            DebugManager debugManager = DebugManager.instance;
            debugManager.displayRuntimeUI = false;
            debugManager.displayPersistentRuntimeUI = false;

            var debugUpdater = FindObjectOfType<DebugUpdater>();
            if (debugUpdater != null)
            {
                CoreUtils.Destroy(debugUpdater.gameObject);
            }
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
