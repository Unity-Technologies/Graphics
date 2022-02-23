using System;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Editor script for a <c>ForwardRendererData</c> class.
    /// </summary>
    [Obsolete("ForwardRendererDataEditor has been deprecated. Use UniversalRendererDataEditor instead (UnityUpgradable) -> UniversalRendererDataEditor", true)]
    public class ForwardRendererDataEditor : ScriptableRendererDataEditor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            throw new NotSupportedException("ForwardRendererDataEditor has been deprecated. Use UniversalRendererDataEditor instead");
        }
    }

    static partial class EditorUtils
    {
    }
}
