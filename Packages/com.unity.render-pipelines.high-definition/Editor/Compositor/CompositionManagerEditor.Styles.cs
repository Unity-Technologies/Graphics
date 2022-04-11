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
        internal static readonly int k_ThumbnailDivider = 5;   // the horizontal space in between the thumbnails
        internal static readonly int k_ThumbnailSpacing = 10;  // the horizontal space after the thumbnail
        internal static readonly int k_IconSpacing = 5;        // the horizontal space after an icon
        internal static readonly int k_CheckboxSpacing = 20;   // the horizontal space for a checkbox

        internal static readonly int k_IconVerticalOffset = 5;  // used to center the icons vertically
        internal static readonly int k_LabelVerticalOffset = 6; // used to center the labels vertically


        internal static readonly int k_IconSize = 28;
        internal static readonly int k_HeaderFontSize = 14;
        internal static readonly int k_ListItemPading = 4;
        internal static readonly int k_ListItemStackPading = 20;
        internal static readonly float k_SingleLineHeight = EditorGUIUtility.singleLineHeight;
        internal static readonly float k_Spacing = k_SingleLineHeight + EditorGUIUtility.standardVerticalSpacing;
    }
}
