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
    public static class CameraCaptureBridge
    {
#if USE_REFLECTION
        static FieldInfo  m_Enabled;
        static MethodInfo m_GetActions;
#endif

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
                useCameraCaptureCallbacksFieldName, BindingFlags.Public | BindingFlags.Static);
            if (useCameraCaptureCallbacksField == null)
                return;

            const string captureClassName = "UnityEditor.Recorder.Input.CameraCapture";
            var captureType = Type.GetType(captureClassName + ", " + editorDllName);
            if (captureType == null)
                return;

            const string getActionsMethodName = "GetActions";
            var getActionsMethod = captureType.GetMethod(
                getActionsMethodName, BindingFlags.Public | BindingFlags.Static);
            if (getActionsMethod == null)
                return;

            m_Enabled = useCameraCaptureCallbacksField;
            m_GetActions = getActionsMethod;
#endif
        }

        static public bool enabled
        {
            get
            {
                return
#if USE_REFLECTION
                    m_Enabled == null ? false : (bool)m_Enabled.GetValue(null)
#elif UNITY_EDITOR
                    UnityEditor.Recorder.Options.useCameraCaptureCallbacks
#else
                    false
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
            }
        }

        static public IEnumerator<Action<RenderTargetIdentifier, CommandBuffer> > GetCaptureActions(Camera camera)
        {
            return
#if USE_REFLECTION
                m_GetActions == null ? null :
                    (m_GetActions.Invoke(null, new object [] { camera } ) as
                     IEnumerator<Action<RenderTargetIdentifier, CommandBuffer> >)
#elif UNITY_EDITOR
                UnityEditor.Recorder.Input.CameraCapture.GetActions(camera)
#else
                null
#endif
                ;
        }
    }
}
