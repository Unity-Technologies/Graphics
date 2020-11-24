using System;
using UnityEngine;

namespace UnityEditor.GUIFramework
{
    /// <summary>
    /// An interface that represents the default UI control.
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
        /// <param name="name">The name of the default control.</param>
        public DefaultControl(string name) : base(name)
        {
        }

        /// <summary>
        /// Overrides the Control.OnBeginLayout function.
        /// </summary>
        /// <remarks>
        ///  Sets the LayoutData.distance to DefaultControl.kPickDistance.
        /// </remarks>
        /// <param name="data">The layout data.</param>
        /// <param name="guiState">The current state of the custom editor.</param>
        /// <returns>Returns the modified layout data.</returns>
        protected override LayoutData OnBeginLayout(LayoutData data, IGUIState guiState)
        {
            data.distance = kPickDistance;
            return data;
        }
    }
}
