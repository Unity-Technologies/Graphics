using System.Collections.Generic;
using System;

namespace UnityEditor.Rendering.Converter
{
    /// <summary>
    /// Represents a converter that processes render pipeline conversion items.
    /// </summary>
    interface IRenderPipelineConverter
    {
        /// <summary>
        /// Gets a value indicating whether the converter is enabled and can be used.
        /// </summary>
        bool isEnabled => true;

        /// <summary>
        /// Gets or sets the reason message shown when the converter item is disabled.
        /// </summary>
        string isDisabledMessage => string.Empty;

        /// <summary>
        /// Scans for available render pipeline converter items.
        /// </summary>
        /// <param name="onScanFinish">
        /// A callback invoked when the scan is complete, providing the list of converter items.
        /// </param>
        void Scan(Action<List<IRenderPipelineConverterItem>> onScanFinish);

        /// <summary>
        /// Called before the conversion process begins.
        /// </summary>
        void BeforeConvert() { }

        /// <summary>
        /// Performs the conversion on a given converter item.
        /// </summary>
        /// <param name="item">The converter item to be processed.</param>
        /// <param name="message">
        /// An output message providing additional details about the conversion result.
        /// </param>
        /// <returns>
        /// A <see cref="Status"/> value representing the outcome of the conversion.
        /// </returns>
        Status Convert(IRenderPipelineConverterItem item, out string message);

        /// <summary>
        /// Called after the conversion process is completed.
        /// </summary>
        void AfterConvert() { }
    }
}
