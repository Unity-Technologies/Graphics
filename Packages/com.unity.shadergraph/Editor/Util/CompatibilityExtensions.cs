using System;
using UnityEngine;

using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    static class CompatibilityExtensions
    {
        public static void OnToggleChanged(this Toggle toggle, EventCallback<ChangeEvent<bool>> callback)
        {
            toggle.RegisterValueChangedCallback(callback);
        }
    }

    static class TrickleDownEnum
    {
        public static readonly TrickleDown NoTrickleDown = TrickleDown.NoTrickleDown;
        public static readonly TrickleDown TrickleDown = TrickleDown.TrickleDown;
    }
}
