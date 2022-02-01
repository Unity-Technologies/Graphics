using System;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Editor script for a <c>ForwardRendererData</c> class.
    /// </summary>
    [Obsolete("ForwardRendererDataEditor has been deprecated. Use UniversalRendererDataEditor instead (UnityUpgradable) -> UniversalRendererDataEditor", true)]
    internal class ForwardRendererDataEditor : ScriptableRendererDataEditor
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
