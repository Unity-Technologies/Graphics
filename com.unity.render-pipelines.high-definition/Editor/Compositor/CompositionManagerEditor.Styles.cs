using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    static internal class CompositorStyle
    {
        internal static readonly int k_ThumbnailSize = 32;
        internal static readonly int k_IconSize = 28;
        internal static readonly int k_ListItemPading = 4;
        internal static readonly int k_ListItemStackPading = 20;
        internal static readonly float k_SingleLineHeight = EditorGUIUtility.singleLineHeight;
        internal static readonly float k_Spacing = k_SingleLineHeight * 1.1f;
    }
}
