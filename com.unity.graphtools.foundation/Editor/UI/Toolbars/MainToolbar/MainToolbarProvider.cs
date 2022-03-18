using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Default implementation of <see cref="IToolbarProvider"/> for the main toolbar.
    /// </summary>
    public class MainToolbarProvider : IToolbarProvider, IOverlayToolbarProvider
    {
        /// <inheritdoc />
        public virtual bool ShowButton(string buttonName)
        {
            return buttonName != MainToolbar.BuildAllButton;
        }

        /// <inheritdoc />
        public virtual IEnumerable<string> GetElementIds()
        {
            // ReSharper disable once RedundantExplicitArrayCreation : type is needed because of #if.
            return new string[]
            {
#if UNITY_2022_2_OR_NEWER
                NewGraphButton.id, SaveAllButton.id
#endif
            };
        }
    }
}
