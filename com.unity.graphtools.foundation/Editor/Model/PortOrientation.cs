namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The orientation of a port.
    /// </summary>
    public enum PortOrientation
    {
        /// <summary>
        /// The port is placed on the left or right side of the node. Edges connected to this port emerge from the left and the right side of the port.
        /// </summary>
        Horizontal,

        /// <summary>
        /// The port is placed on the top or the bottom of the node. Edges connected to this port emerge from the top and bottom of the port.
        /// </summary>
        Vertical
    }
}
