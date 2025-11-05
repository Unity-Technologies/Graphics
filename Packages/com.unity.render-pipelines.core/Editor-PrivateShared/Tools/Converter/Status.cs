using System;

namespace UnityEditor.Rendering.Converter
{
    /// <summary>
    /// Represents the conversion state of a render pipeline converter item.
    /// </summary>
    [Serializable]
    enum Status
    {
        /// <summary>
        /// The item is waiting to be processed.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// The item has a potential issue that may require attention.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// The item encountered an error during processing.
        /// </summary>
        Error = 2,

        /// <summary>
        /// The item was successfully processed without issues.
        /// </summary>
        Success = 3,
    }
}
