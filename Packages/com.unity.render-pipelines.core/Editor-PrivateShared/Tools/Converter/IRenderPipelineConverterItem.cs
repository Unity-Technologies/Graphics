using UnityEngine;

namespace UnityEditor.Rendering.Converter
{
    /// <summary>
    /// Represents a converter item used within a render pipeline conversion process.
    /// </summary>
    interface IRenderPipelineConverterItem
    {
        /// <summary>
        /// Gets the display name of the converter item.
        /// </summary>
        string name { get; }

        /// <summary>
        /// Gets a description or additional information about the converter item.
        /// </summary>
        string info { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the converter item is enabled.
        /// </summary>
        bool isEnabled { get; set; }

        /// <summary>
        /// Gets or sets the reason message shown when the converter item is disabled.
        /// </summary>
        string isDisabledMessage { get; set; }

        Texture2D icon => null;

        /// <summary>
        /// Invoked when the converter item is clicked or activated.
        /// </summary>
        void OnClicked();
    }

}
