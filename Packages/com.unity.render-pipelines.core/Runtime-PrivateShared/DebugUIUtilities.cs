using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Unity.RenderPipelines.Core.Runtime.Shared
{
    internal static class DebugUIUtilities
    {
        internal static DebugUI.Foldout GetFoldoutByName(string name, IEnumerable<DebugUI.Widget> list)
        {
            if (list == null) return null;
            foreach (var x in list)
            {
                if (x is DebugUI.Foldout foldout && foldout.displayName == name)
                    return foldout;
            }
            return null;
        }
    }
}
