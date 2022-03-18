using System;
using System.Collections.Generic;
#if UNITY_2022_2_OR_NEWER
using UnityEditor.Toolbars;
#endif

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Customize the content of an overlay toolbar.
    /// </summary>
    public interface IOverlayToolbarProvider
    {
        /// <summary>
        /// Returns the ids of the element to display in the toolbar. The ids are the one specified using the <see cref="EditorToolbarElementAttribute"/>.
        /// </summary>
        /// <returns>The ids of the element to display in the toolbar.</returns>
        IEnumerable<string> GetElementIds();
    }
}
