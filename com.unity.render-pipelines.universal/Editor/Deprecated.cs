using System;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    [Obsolete("ForwardRendererDataEditor has been deprecated. Use UniversalRendererDataEditor instead (UnityUpgradable) -> UniversalRendererDataEditor", true)]
    public class ForwardRendererDataEditor : ScriptableRendererDataEditor
    {
        protected override CachedScriptableRendererDataEditor Init(SerializedProperty property)
        {
            throw new NotSupportedException("ForwardRendererDataEditor has been deprecated. Use UniversalRendererDataEditor instead");
        }
        protected override void OnGUI(CachedScriptableRendererDataEditor cachedData, SerializedProperty property)
        {
            throw new NotSupportedException("ForwardRendererDataEditor has been deprecated. Use UniversalRendererDataEditor instead");
        }
    }

    static partial class EditorUtils
    {
    }
}
