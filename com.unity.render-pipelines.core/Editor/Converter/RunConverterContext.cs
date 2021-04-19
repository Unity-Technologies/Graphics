using System;
using System.Collections.Generic;

namespace UnityEditor.Rendering
{
    public struct RunItemContext
    {
        ConverterItemInfo m_Item;
        public ConverterItemInfo item => m_Item;
        public bool didFail { get; set; }
        public string info { get; set; }
        internal bool hasConverted { get; set; }

        public RunItemContext(ConverterItemInfo item)
        {
            m_Item = item;
            didFail = false;
            info = "";
            hasConverted = false;
        }
    }

    /// <summary>
    /// A structure needed for the conversion part of the converter.
    /// This holds the index for failed and successful items converted.
    /// </summary>
    public struct RunConverterContext
    {
        List<ConverterItemInfo> m_Items;

        List<FailedItem> m_FailedItems;
        List<SuccessfulItem> m_SuccessfulItems;

        /// <summary>
        /// These are the items that the converter needs to iterate over.
        /// The UI will make this list of items only the ones that are ticked and should be converted.
        /// </summary>
        public IEnumerable<ConverterItemInfo> items => m_Items;

        internal int failedCount => m_FailedItems.Count;
        internal int successfulCount => m_SuccessfulItems.Count;

        // DidFailed
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
}
