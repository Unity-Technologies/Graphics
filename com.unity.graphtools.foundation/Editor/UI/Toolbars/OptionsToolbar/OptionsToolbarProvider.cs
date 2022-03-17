#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Default implementation of <see cref="IToolbarProvider"/> for the option toolbar.
    /// </summary>
    public class OptionsToolbarProvider : IOverlayToolbarProvider
    {
        /// <inheritdoc />
        public virtual IEnumerable<string> GetElementIds()
        {
            return new[] { OptionDropDownMenu.id };
        }
    }
}
#endif
