using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.Internal
{
    /// <summary>
    /// Contains a database of built-in resource GUIds. These are used to load built-in resource files.
    /// </summary>
    public static class ResourceGuid
    {
        /// <summary>
        /// GUId for the <c>ScriptableRendererFeature</c> template file.
        /// </summary>
        public static readonly string rendererTemplate = "51493ed8d97d3c24b94c6cffe834630b";
    }
}

namespace UnityEditor.Rendering.Universal
{
    static partial class EditorUtils
    {
        // Each group is separate in the menu by a menu bar
        public const int lwrpAssetCreateMenuPriorityGroup1 = CoreUtils.assetCreateMenuPriority1;
        public const int lwrpAssetCreateMenuPriorityGroup2 = CoreUtils.assetCreateMenuPriority1 + 50;
        public const int lwrpAssetCreateMenuPriorityGroup3 = lwrpAssetCreateMenuPriorityGroup2 + 50;

        internal enum Unit { Metric, Percent }

        internal class Styles
        {
            //Measurements
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
