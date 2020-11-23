using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Default Control
    /// </summary>
    public abstract class DefaultControl : Control
    {
        /// <summary>
        /// Default kPickDistance == 5.0f
        /// </summary>
        public static readonly float kPickDistance = 5f;

        /// <summary>
        /// Initializes and returns an instance of DefaultControl
        /// </summary>
        /// <param name="name">Name of the default control</param>
        public DefaultControl(string name) : base(name)
        {
        }

        /// <summary>
        /// Override of the On Begin Layout, and modify LayoutData.distance with DefaultControl.kPickDistance
        /// </summary>
        /// <param name="data">Layout Data</param>
        /// <param name="guiState">The GUI State</param>
        /// <returns>The Layout Data</returns>
        protected override LayoutData OnBeginLayout(LayoutData data, IGUIState guiState)
        {
            data.distance = kPickDistance;
            return data;
        }
    }
}
