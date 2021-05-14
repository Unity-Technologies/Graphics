using System.Collections.Generic;

namespace UnityEditor.Rendering.Universal.Converters
{
    /// <summary>
    /// A structure needed for the initialization step of the converter.
    /// Stores data to be visible in the UI.
    /// </summary>
    internal struct InitializeConverterContext
    {
        /// <summary>
        /// Stores the list of ConverterItemDescriptor that will be filled in during the initialization step.
        /// </summary>
        internal List<ConverterItemDescriptor> items;

        /// <summary>
        /// Add to the list of assets to be converted.
        /// This will be used to display information to the user in the UI.
        /// </summary>
        /// <param name="item">The item to add to the list items to convert</param>
        internal void AddAssetToConvert(ConverterItemDescriptor item)
        {
            items.Add(item);
        }
    }
}
