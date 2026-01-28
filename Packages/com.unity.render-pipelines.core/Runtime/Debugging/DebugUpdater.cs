#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif
#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
#define USE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

using System;
using System.Collections;
using System.Diagnostics;

namespace UnityEngine.Rendering
{
    [CoreRPHelpURL("Rendering-Debugger")]
    [AddComponentMenu("")] // Hide from Add Component menu
    class DebugUpdater : MonoBehaviour
    {
        static DebugUpdater s_Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RuntimeInit()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (DebugManager.instance.enableRuntimeUI)
                EnableRuntime();
#endif
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
            if (s_Instance != null)
                return;

            var go = new GameObject { name = "[Debug Updater]" };
            s_Instance = go.AddComponent<DebugUpdater>();

            DontDestroyOnLoad(go);

            DebugManager.instance.EnableInputActions();

#if USE_INPUT_SYSTEM
            EnhancedTouchSupport.Enable();
#endif
        }

        static void DisableRuntime()
        {
            DebugManager debugManager = DebugManager.instance;
            debugManager.displayRuntimeUI = false;
            debugManager.displayPersistentRuntimeUI = false;

            if (s_Instance != null)
            {
                CoreUtils.Destroy(s_Instance.gameObject);
                s_Instance = null;
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

#if ENABLE_RENDERING_DEBUGGER_UI
            if (debugManager.displayRuntimeUI)
            {
                if (debugManager.m_RuntimeDebugWindow.IsPopupOpen())
                    return;

                if (debugManager.GetAction(DebugAction.ResetAll) != 0.0f)
                    debugManager.Reset();

                if (debugManager.GetAction(DebugAction.MakePersistent) != 0.0f)
                    debugManager.TogglePersistent();

                if (debugManager.GetAction(DebugAction.NextDebugPanel) != 0.0f)
                    debugManager.m_RuntimeDebugWindow.SelectNextPanel();

                if (debugManager.GetAction(DebugAction.PreviousDebugPanel) != 0.0f)
                    debugManager.m_RuntimeDebugWindow.SelectPreviousPanel();

                float moveHorizontal = debugManager.GetAction(DebugAction.MoveHorizontal);
                if (moveHorizontal != 0.0f)
                    debugManager.m_RuntimeDebugWindow.ChangeSelectedValue(moveHorizontal);
            }
#endif
        }
    }
}
