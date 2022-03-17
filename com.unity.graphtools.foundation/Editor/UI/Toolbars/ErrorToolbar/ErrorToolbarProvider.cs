#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Class that defines the content of the error toolbar.
    /// </summary>
    public class ErrorToolbarProvider : IOverlayToolbarProvider
    {
        /// <inheritdoc />
        public virtual IEnumerable<string> GetElementIds()
        {
            return new[] { ErrorCountLabel.id, PreviousErrorButton.id, NextErrorButton.id };
        }
    }
}
#endif
