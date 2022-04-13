using System;
using UnityEngine;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Interface for storing the debug settings
    /// </summary>
    public interface IDebugDisplaySettings : IDebugDisplaySettingsQuery
    {
        /// <summary>
        /// Reset the stored debug settings
        /// </summary>
        void Reset();

        /// <summary>
        /// Executes an action for each element
        /// </summary>
        /// <param name="onExecute"></param>
        void ForEach(Action<IDebugDisplaySettingsData> onExecute);
    }
}
