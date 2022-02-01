using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Flags]
    public enum ResizerDirection
    {
        Top = 1 << 0,
        Bottom = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
    }

    /// <summary>
    /// An element used to interactively resizes its parent.
    /// </summary>
    public class ResizableElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ResizableElement> {}

        public static readonly string ussClassName = "ge-resizable-element";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResizableElement"/> class.
        /// </summary>
        public ResizableElement()
        {
            GraphElementHelper.LoadTemplateAndStylesheet(this, "Resizable", ussClassName);

            foreach (ResizerDirection value in Enum.GetValues(typeof(ResizerDirection)))
            {
                var resizer = this.SafeQ(value.ToString().ToLower() + "-resize");
                if (resizer != null)
                    resizer.AddManipulator(new ElementResizer(this, value));
            }

            foreach (ResizerDirection vertical in new[] { ResizerDirection.Top, ResizerDirection.Bottom })
            {
                foreach (ResizerDirection horizontal in new[] { ResizerDirection.Left, ResizerDirection.Right })
                {
                    var resizer = this.SafeQ(vertical.ToString().ToLower() + "-" + horizontal.ToString().ToLower() + "-resize");
                    if (resizer != null)
                        resizer.AddManipulator(new ElementResizer(this, vertical | horizontal));
                }
            }

            pickingMode = PickingMode.Ignore;
            AddToClassList(ussClassName);
        }
    }
}
