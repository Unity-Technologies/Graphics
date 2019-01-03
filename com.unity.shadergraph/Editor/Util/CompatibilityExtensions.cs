using System;
using UnityEngine;

using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    static class CompatibilityExtensions
    {
#if !UNITY_2018_3_OR_NEWER
        public static void MarkDirtyRepaint(this VisualElement element)
        {
            element.Dirty(ChangeType.Repaint);
        }
#endif

#if !UNITY_2018_3_OR_NEWER
        public static void CaptureMouse(this VisualElement element)
        {
            element.TakeMouseCapture();
        }

        public static void ReleaseMouse(this VisualElement element)
        {
            element.ReleaseMouseCapture();
        }
#endif

        public static void OnToggleChanged(this Toggle toggle, EventCallback<ChangeEvent<bool>> callback)
        {
#if UNITY_2018_3_OR_NEWER
            toggle.RegisterValueChangedCallback(callback);
#else
            toggle.OnToggle(() => callback(ChangeEvent<bool>.GetPooled(!toggle.value, toggle.value)));
#endif
        }
    }

    static class TrickleDownEnum
    {
#if UNITY_2018_3_OR_NEWER
        public static readonly TrickleDown NoTrickleDown = TrickleDown.NoTrickleDown;
        public static readonly TrickleDown TrickleDown = TrickleDown.TrickleDown;
#else
        public static readonly Capture NoTrickleDown = Capture.NoCapture;
        public static readonly Capture TrickleDown = Capture.Capture;
#endif
    }
}
