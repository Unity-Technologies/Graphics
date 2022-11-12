#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
#define USE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.EnhancedTouch;
#endif
using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine.EventSystems;

namespace UnityEngine.Rendering
{
    [CoreRPHelpURL("Rendering-Debugger")]
    class DebugUpdater : MonoBehaviour
    {
        static DebugUpdater s_Instance = null;

        ScreenOrientation m_Orientation;
        bool m_RuntimeUiWasVisibleLastFrame = false;

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
            s_Instance.m_Orientation = Screen.orientation;

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

        internal static void HandleInternalEventSystemComponents(bool uiEnabled)
        {
            if (s_Instance == null)
                return;

            if (uiEnabled)
                s_Instance.EnsureExactlyOneEventSystem();
            else
                s_Instance.DestroyDebugEventSystem();
        }

        void EnsureExactlyOneEventSystem()
        {
            var eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            var debugEventSystem = GetComponent<EventSystem>();

            if (eventSystems.Length > 1 && debugEventSystem != null)
            {
                Debug.Log($"More than one EventSystem detected in scene. Destroying EventSystem owned by DebugUpdater.");
                DestroyDebugEventSystem();
            }
            else if (eventSystems.Length == 0)
            {
                Debug.Log($"No EventSystem available. Creating a new EventSystem to enable Rendering Debugger runtime UI.");
                CreateDebugEventSystem();
            }
            else
            {
                StartCoroutine(DoAfterInputModuleUpdated(CheckInputModuleExists));
            }
        }

        IEnumerator DoAfterInputModuleUpdated(Action action)
        {
            // EventSystem.current.currentInputModule is not updated immediately when EventSystem.current changes. It happens
            // with a delay in EventSystem.Update(), so wait a couple of frames to ensure that has happened.
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            action.Invoke();
        }

        void CheckInputModuleExists()
        {
            if (EventSystem.current != null && EventSystem.current.currentInputModule == null)
            {
                Debug.LogWarning("Found a game object with EventSystem component but no corresponding BaseInputModule component - Debug UI input might not work correctly.");
            }
        }

#if USE_INPUT_SYSTEM
        void AssignDefaultActions()
        {
            if (EventSystem.current != null && EventSystem.current.currentInputModule is InputSystemUIInputModule inputSystemModule)
            {
                // FIXME: In order to activate default input actions in player builds (required for touch input to work),
                // we need to call InputSystemUIInputModule.AssignDefaultActions() which was added in com.unity.inputsystem@1.1.0-pre.5.
                // However, there is a problem in InputSystem package version ordering, where it sorts this version as an
                // older version than it should be. Hence we cannot write a version define to conditionally compile this function call.
                // Instead, we use reflection to see if the function is there and can be invoked.
                //
                // Once com.unity.inputsystem@1.1.0 is available, create an INPUTSYSTEM_1_1_0_OR_GREATER version define and use it
                // to conditionally call AssignDefaultActions().
                System.Reflection.MethodInfo assignDefaultActionsMethod = inputSystemModule.GetType().GetMethod("AssignDefaultActions");
                if (assignDefaultActionsMethod != null)
                {
                    assignDefaultActionsMethod.Invoke(inputSystemModule, null);
                }
            }

            CheckInputModuleExists();
        }
#endif

        void CreateDebugEventSystem()
        {
            gameObject.AddComponent<EventSystem>();
#if USE_INPUT_SYSTEM
            gameObject.AddComponent<InputSystemUIInputModule>();
            StartCoroutine(DoAfterInputModuleUpdated(AssignDefaultActions));
#else
            gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        void DestroyDebugEventSystem()
        {
            var eventSystem = GetComponent<EventSystem>();
#if USE_INPUT_SYSTEM
            var inputModule = GetComponent<InputSystemUIInputModule>();
            if (inputModule)
            {
                CoreUtils.Destroy(inputModule);
                StartCoroutine(DoAfterInputModuleUpdated(AssignDefaultActions));
            }
#else
            CoreUtils.Destroy(GetComponent<StandaloneInputModule>());
            CoreUtils.Destroy(GetComponent<BaseInput>());
#endif
            CoreUtils.Destroy(eventSystem);
        }

        void Update()
        {
            DebugManager debugManager = DebugManager.instance;

            // Runtime UI visibility can change i.e. due to scene unload - allow component cleanup in this case.
            if (m_RuntimeUiWasVisibleLastFrame != debugManager.displayRuntimeUI)
            {
                HandleInternalEventSystemComponents(debugManager.displayRuntimeUI);
            }

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

            m_RuntimeUiWasVisibleLastFrame = debugManager.displayRuntimeUI;
        }

        static IEnumerator RefreshRuntimeUINextFrame()
        {
            yield return null; // Defer runtime UI refresh to next frame to allow canvas to update first.
            DebugManager.instance.ReDrawOnScreenDebug();
        }
    }
}
