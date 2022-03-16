using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// An element used to interactively resizes its parent.
    /// </summary>
    public class OverlayResizer : VisualElement
    {
        public static readonly string ussClassName = "ge-overlay-resizer";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResizableElement"/> class.
        /// </summary>
        public OverlayResizer()
        {
            GraphElementHelper.LoadTemplateAndStylesheet(this, "OverlayResizer", ussClassName);

            VisualElement resizer;
            foreach (var value in new[] { ResizerDirection.Bottom, ResizerDirection.Right })
            {
                resizer = this.SafeQ(value.ToString().ToLower() + "-resize");
                resizer?.AddManipulator(new ElementResizer(this, value));
            }

            resizer = this.SafeQ(ResizerDirection.Bottom.ToString().ToLower() + "-" + ResizerDirection.Right.ToString().ToLower() + "-resize");
            resizer?.AddManipulator(new ElementResizer(this, ResizerDirection.Bottom | ResizerDirection.Right));

            pickingMode = PickingMode.Ignore;
            AddToClassList(ussClassName);
        }
    }
}
