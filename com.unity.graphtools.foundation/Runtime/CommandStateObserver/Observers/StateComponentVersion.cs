using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// The version of a state component.
    /// </summary>
    public struct StateComponentVersion : IEquatable<StateComponentVersion>
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

        /// <inheritdoc />
        public bool Equals(StateComponentVersion other)
        {
            return HashCode == other.HashCode && Version == other.Version;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is StateComponentVersion other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return System.HashCode.Combine(HashCode, Version);
        }
    }
}
