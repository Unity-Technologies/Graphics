using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    /// <summary>
    /// Implementation of <see cref="IToolbarProvider"/> for the main toolbar.
    /// </summary>
    public class MathBookMainToolbarProvider : MainToolbarProvider
    {
        /// <inheritdoc />
        public override IEnumerable<string> GetElementIds()
        {
            // Replace the save all button by our own.
            var elements = base.GetElementIds().ToList();
#if UNITY_2022_2_OR_NEWER
            var saveAllIndex = elements.IndexOf(SaveButton.id);
            elements[saveAllIndex] = MathBookSaveButton.id;
#endif
            return elements;
        }
    }
}
