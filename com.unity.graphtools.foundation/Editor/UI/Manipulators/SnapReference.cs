using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// What part of the element is used as the reference for snapping.
    /// </summary>
    enum SnapReference
    {
        /// <summary>
        /// Snap the left edge of the element.
        /// </summary>
        LeftEdge,

        /// <summary>
        /// Snap the horizontal center of the element.
        /// </summary>
        HorizontalCenter,

        /// <summary>
        /// Snap the right edge of the element.
        /// </summary>
        RightEdge,

        /// <summary>
        /// Snap the top edge of the element.
        /// </summary>
        TopEdge,

        /// <summary>
        /// Snap the vertical center of the element.
        /// </summary>
        VerticalCenter,

        /// <summary>
        /// Snap the bottom edge of the element.
        /// </summary>
        BottomEdge
    }
}
