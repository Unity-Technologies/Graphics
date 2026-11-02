using System.Collections.Generic;
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

    /// <summary>
    /// Represents a folder item that can contain other converter items in a custom hierarchy.
    /// Converters can return folder items to provide their own organizational structure
    /// instead of using the default path-based organization.
    /// </summary>
    interface IFolderRenderPipelineConverterItem : IRenderPipelineConverterItem
    {
        /// <summary>
        /// Gets the child items contained in this folder.
        /// Children can be regular items or other folder items for nested hierarchies.
        /// </summary>
        IList<IRenderPipelineConverterItem> children { get; }
    }

}
