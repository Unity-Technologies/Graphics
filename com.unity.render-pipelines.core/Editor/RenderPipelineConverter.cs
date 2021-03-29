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
    /// Path = The Path to the asset being converted.
    /// InitialInfo = If there are some issues with the converter that we already know about.
    ///     Example: If we know it is a custom shader, we can not convert it so we add the information here.
    /// HelpLink = Link to the documentation of how to convert this asset. Useful if the conversion failed or if we know we can not convert this asset automatically.
    /// ID is for indexing if needed.
    /// </summary>
    public struct ConverterItemDescriptor
    {
        public string name;
        public string path;
        public string initialInfo;
        public string helpLink;
        // public int id;
        // internal int index;
        // internal bool failed;
    }

    public struct ConverterItemInfo
    {
        public ConverterItemDescriptor descriptor { get; internal set; }
        public int index { get; internal set; }
    }

    // Storing the index of the failed converters so that we can show that in the UI.
    internal struct FailedItem
    {
        public int index;
        public string message;
    }

    internal struct SuccessfulItem
    {
        public int index;
    }

    public struct InitializeConverterContext
    {
        public List<ConverterItemDescriptor> m_Items;

        public void AddAssetToConvert(ConverterItemDescriptor item)
        {
            //if (!string.IsNullOrEmpty(item.initialInfo))
            //{
            //    item.
            //}
            m_Items.Add(item);
        }
    }

    public struct RunConverterContext
    {
        internal List<ConverterItemInfo> m_Items;
        internal List<FailedItem> m_FailedItems;
        internal List<SuccessfulItem> m_SuccessfulItems;
        public IEnumerable<ConverterItemInfo> items => m_Items;

        //public string m_ProcessingInfo;
        public int m_ProcessingID;
        public RunConverterContext(List<ConverterItemInfo> items)
        {
            m_Items = items;
            m_FailedItems = new List<FailedItem>();
            m_SuccessfulItems = new List<SuccessfulItem>();
            m_ProcessingID = 0;
        }

        public void MarkFailed(int index)
        {
            MarkFailed(index, "Failed");
        }

        public void MarkFailed(int index, string message)
        {
            m_FailedItems.Add(new FailedItem(){index = index, message = message});
        }

        public void MarkSuccessful(int index)
        {
            m_SuccessfulItems.Add(new SuccessfulItem(){index = index});
        }

        public void Processing(int index)
        {
            m_ProcessingID = index;
        }
    }

// Might need to change this name before making it public
    public abstract class RenderPipelineConverter
    {
        // Name of the converter
        public abstract string name { get; }
        // The information when hovering over the converter
        public abstract string info { get; }
        // A check if the converter is enabled or not.
        public virtual bool Enabled()
        {
            return true;
        }

        public virtual void OnClicked(int index)
        {
        }

        // This is so that we can have different segment in out UI, example Unity converters, your custom converters etc..
        public virtual string category { get; }
        // This is in which drop down item the converter belongs to.
        public abstract Type conversion { get; }
        // This runs when initializing the converter. To gather data for the UI and also for the converter if needed.
        public abstract void OnInitialize(InitializeConverterContext ctx);

        /// <summary>
        /// The method that will be run when converting the assets.
        /// Takes a RunConverterContext.
        /// </summary>
        public abstract void OnRun(RunConverterContext ctx);
    }
}
