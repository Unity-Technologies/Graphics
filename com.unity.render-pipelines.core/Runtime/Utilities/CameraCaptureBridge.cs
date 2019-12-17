#if UNITY_EDITOR
#define USE_REFLECTION
#endif

using System;
using System.Collections.Generic;
#if UNITY_EDITOR
#if USE_REFLECTION
using System.Reflection;

#else
using UnityEditor.Recorder;
#endif
#endif


namespace UnityEngine.Rendering
{
    /// <summary>
    /// Bridge class for camera captures.
    /// </summary>
    public static class CameraCaptureBridge
    {
#if USE_REFLECTION
        private static FieldInfo m_Enabled;
        private static MethodInfo m_GetActions;
#endif

        private static Dictionary<Camera, HashSet<Action<RenderTargetIdentifier, CommandBuffer>>> actionDict =
            new Dictionary<Camera, HashSet<Action<RenderTargetIdentifier, CommandBuffer>>>();

        private static bool _enabled;

        static CameraCaptureBridge()
        {
#if USE_REFLECTION
            const string optionsClassName = "UnityEditor.Recorder.Options";
            const string editorDllName = "Unity.Recorder.Editor";
            var optionsType = Type.GetType(optionsClassName + ", " + editorDllName);
            if (optionsType == null)
                return;

            const string useCameraCaptureCallbacksFieldName = "useCameraCaptureCallbacks";
            var useCameraCaptureCallbacksField = optionsType.GetField(
                useCameraCaptureCallbacksFieldName,
                BindingFlags.Public | BindingFlags.Static);
            if (useCameraCaptureCallbacksField == null)
                return;

            const string captureClassName = "UnityEditor.Recorder.Input.CameraCapture";
            var captureType = Type.GetType(captureClassName + ", " + editorDllName);
            if (captureType == null)
                return;

            const string getActionsMethodName = "GetActions";
            var getActionsMethod = captureType.GetMethod(
                getActionsMethodName,
                BindingFlags.Public | BindingFlags.Static);
            if (getActionsMethod == null)
                return;

            m_Enabled = useCameraCaptureCallbacksField;
            m_GetActions = getActionsMethod;
#endif
        }

        /// <summary>
        /// Enable camera capture.
        /// </summary>
        public static bool enabled
        {
            get
            {
                return
#if USE_REFLECTION
                    m_Enabled == null ? _enabled : (bool)m_Enabled.GetValue(null)
#elif UNITY_EDITOR
                    UnityEditor.Recorder.Options.useCameraCaptureCallbacks
#else
                    _enabled
#endif
                    ;
            }
            set
            {
#if USE_REFLECTION
                m_Enabled?.SetValue(null, value);
#elif UNITY_EDITOR
                UnityEditor.Recorder.Options.useCameraCaptureCallbacks = value;
#endif
                _enabled = value;
            }
        }

        /// <summary>
        /// Provides the set actions to the renderer to be triggered at the end of the render loop for camera capture
        /// </summary>
        /// <param name="camera">The camera to get actions for</param>
        /// <returns>Enumeration of actions</returns>
        public static IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> GetCaptureActions(Camera camera)
        {
#if USE_REFLECTION
            if (m_GetActions != null)
            {
                var recorderActions = (m_GetActions.Invoke(null, new object[] { camera }) as IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>>);
                if (recorderActions != null)
                    return recorderActions;
            }
#elif UNITY_EDITOR
            var recorderActions = UnityEditor.Recorder.Input.CameraCapture.GetActions(camera);
            if (recorderActions != null)
	            return recorderActions;
#endif

            if (!actionDict.TryGetValue(camera, out var actions))
                return null;

            return actions.GetEnumerator();
        }

        /// <summary>
        /// Adds actions for camera capture
        /// </summary>
        /// <param name="camera">The camera to add actions for</param>
        /// <param name="action">The action to add</param>
        public static void AddCaptureAction(Camera camera, Action<RenderTargetIdentifier, CommandBuffer> action)
        {
            actionDict.TryGetValue(camera, out var actions);
            if (actions == null)
            {
                actions = new HashSet<Action<RenderTargetIdentifier, CommandBuffer>>();
                actionDict.Add(camera, actions);
            }

            actions.Add(action);
        }

        /// <summary>
        /// Removes actions for camera capture
        /// </summary>
        /// <param name="camera">The camera to remove actions for</param>
        /// <param name="action">The action to remove</param>
        public static void RemoveCaptureAction(Camera camera, Action<RenderTargetIdentifier, CommandBuffer> action)
        {
            if (camera == null)
                return;

            if (actionDict.TryGetValue(camera, out var actions))
                actions.Remove(action);
        }
    }
}
