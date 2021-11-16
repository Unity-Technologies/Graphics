using System;
using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    [Obsolete("ForwardRendererDataEditor has been deprecated. Use UniversalRendererDataEditor instead (UnityUpgradable) -> UniversalRendererDataEditor", true)]
    public class ForwardRendererDataEditor : ScriptableRendererDataEditor
    {
        public override void OnInspectorGUI()
        {
            throw new NotSupportedException("ForwardRendererDataEditor has been deprecated. Use UniversalRendererDataEditor instead");
        }
    }

    public abstract partial class BaseShaderGUI
    {
        [Obsolete("DrawAdditionalFoldouts has been deprecated. Use FillAdditionalFoldouts instead, and materialScopesList.RegisterHeaderScope", false)]
        public virtual void DrawAdditionalFoldouts(Material material) { }
    }

    static partial class EditorUtils
    {
    }
}
