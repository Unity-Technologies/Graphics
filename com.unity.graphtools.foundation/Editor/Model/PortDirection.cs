using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Port directions.
    /// </summary>
    [Flags]
    public enum PortDirection
    {
        /// <summary>
        /// The port does not have a specific direction. It can receive or send information.
        /// </summary>
        None = 0,

        /// <summary>
        /// The port is used to receive information.
        /// </summary>
        Input = 1,

        /// <summary>
        /// The port is used to send information.
        /// </summary>
        Output = 2
    }
}
