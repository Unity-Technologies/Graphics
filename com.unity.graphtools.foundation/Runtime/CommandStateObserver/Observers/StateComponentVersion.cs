using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// The version of a state component.
    /// </summary>
    public struct StateComponentVersion
    {
        /// <summary>
        /// The hash code of the state component.
        /// </summary>
        public int HashCode;
        /// <summary>
        /// The version number of the state component.
        /// </summary>
        public uint Version;

        /// <inheritdoc />
        public override string ToString() => $"{HashCode}.{Version}";
    }
}
