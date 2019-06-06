using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Colors
{
    internal class NoColors : IColorProvider
    {
        public const string Title = "<None>";
        public string GetTitle() => Title;

        public bool AllowCustom() => false;
        public bool ClearOnDirty() => false;

        public void ApplyColor(IShaderNodeView nodeView)
        {
        }

        public void ClearColor(IShaderNodeView nodeView)
        {
        }
    }
}
