using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// A structure holding the information for each Item that needs to be Converted.
    /// Name = The Name of the asset that is being converted.
    /// Info = Information that can be used to store some data. This will also be shown in the UI.
    /// WarningMessage = If there are some issues with the converter that we already know about.
    ///     Example: If we know it is a custom shader, we can not convert it so we add the information here.
    /// HelpLink = Link to the documentation of how to convert this asset. Useful if the conversion failed or if we know we can not convert this asset automatically.
    /// </summary>
    public struct ConverterItemDescriptor
    {
        public string name;
        public string info;
        public string warningMessage;
        public string helpLink;
    }

    /// <summary>
    /// A structure holding the information for each Item that needs to be Converted.
    /// Descriptor = The ConverterItemDescriptor this item contain.
    /// Index = The index for this item in the list of converter items.
    /// </summary>
    public struct ConverterItemInfo
    {
        public ConverterItemDescriptor descriptor { get; internal set; }
        public int index { get; internal set; }
    }

    // Storing the index and message of the failed item so that we can show that in the UI.
    internal struct FailedItem
    {
        public int index;
        public string message;
    }

    internal struct SuccessfulItem
    {
        public int index;
    }

    /// <summary>
    /// A structure needed for the initialization step of the converter.
    /// Stores data to be visible in the UI.
    /// </summary>
    public struct InitializeConverterContext
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
        public void AddAssetToConvert(ConverterItemDescriptor item)
        {
            items.Add(item);
        }
    }

    /// <summary>
    /// A structure needed for the conversion part of the converter.
    /// This holds the index for failed and successful items converted.
    /// </summary>
    public struct RunConverterContext
    {
        List<ConverterItemInfo> m_Items;

        internal List<FailedItem> m_FailedItems;
        internal List<SuccessfulItem> m_SuccessfulItems;

        /// <summary>
        /// These are the items that the converter needs to iterate over.
        /// The UI will make this list of items only the ones that are ticked and should be converted.
        /// </summary>
        public IEnumerable<ConverterItemInfo> items => m_Items;

        internal int failedCount => m_FailedItems.Count;
        internal int successfulCount => m_SuccessfulItems.Count;

        internal FailedItem GetFailedItemAtIndex(int index)
        {
            if (index < 0 || index > failedCount)
                throw new ArgumentOutOfRangeException();
            return m_FailedItems[index];
        }

        internal SuccessfulItem GetSuccessfulItemAtIndex(int index)
        {
            if (index < 0 || index > successfulCount)
                throw new ArgumentOutOfRangeException();
            return m_SuccessfulItems[index];
        }



        /// <summary>
        /// These are the items that the converter needs to iterate over.
        /// The UI will make this list of items only the ones that are ticked and should be converted.
        /// </summary>
        /// <param name="items">The items the converter will operate on.</param>
        public RunConverterContext(List<ConverterItemInfo> items)
        {
            m_Items = items;

            m_FailedItems = new List<FailedItem>();
            m_SuccessfulItems = new List<SuccessfulItem>();
        }

        /// <summary>
        /// Mark the converter item index as failed.
        /// </summary>
        /// <param name="index">The index that will be marked as failed.</param>
        public void MarkFailed(int index)
        {
            MarkFailed(index, "Failed");
        }

        /// <summary>
        /// Mark the converter item index as failed.
        /// </summary>
        /// <param name="index">The index that will be marked as failed.</param>
        /// <param name="message">The message that will be shown for the failed item.</param>
        public void MarkFailed(int index, string message)
        {
            m_FailedItems.Add(new FailedItem(){index = index, message = message});
        }

        /// <summary>
        /// Mark the converter item index as successful.
        /// </summary>
        /// <param name="index">The index that will be marked as successful.</param>
        public void MarkSuccessful(int index)
        {
            m_SuccessfulItems.Add(new SuccessfulItem(){index = index});
        }
    }

    // Might need to change this name before making it public
    public abstract class RenderPipelineConverter
    {
        /// <summary>
        /// Name of the converter.
        /// </summary>
        public abstract string name { get; }

        /// <summary>
        /// The information when hovering over the converter.
        /// </summary>
        public abstract string info { get; }

        /// <summary>
        /// A check if the converter is enabled or not. Can be used to do a check if prerequisites are met to have it enabled or disabled.
        /// </summary>
        public virtual bool Enabled()
        {
            return true;
        }

        /// <summary>
        /// This method getting triggered when clicking the listview item in the UI.
        /// </summary>
        public virtual void OnClicked(int index)
        {
        }

        // This is so that we can have different segment in our UI, example Unity converters, your custom converters etc..
        // This is not implemented yet
        public virtual string category { get; }

        // This is in which drop down item the converter belongs to.
        // Not properly implemented yet
        public abstract Type conversion { get; }

        /// <summary>
        /// This runs when initializing the converter. To gather data for the UI and also for the converter if needed.
        /// </summary>
        /// <param name="context">The context that will be used to initialize data for the converter.</param>
        public abstract void OnInitialize(InitializeConverterContext context);

        /// <summary>
        /// The method that will be run when converting the assets.
        /// </summary>
        /// <param name="context">The context that will be used when executing converter.</param>
        public abstract void OnRun(RunConverterContext context);
    }
}
