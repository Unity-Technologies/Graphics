using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public static class EditorWindowExtensions
    {
        public static void CloseAllOverlays(this EditorWindow window)
        {
#if UNITY_2022_2_OR_NEWER
            foreach (var overlay in window.GetAllOverlays())
            {
                overlay.displayed = false;
            }
#endif
        }
    }
}
