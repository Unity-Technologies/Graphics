using System;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Searcher Adapter to search for enum values.
    /// </summary>
    class EnumValuesAdapter : SimpleSearcherAdapter
    {
        /// <summary>
        /// SearcherItem class to use when creating searchable enum values.
        /// </summary>
        public class EnumValueSearcherItem : SearcherItem
        {
            public EnumValueSearcherItem(Enum value, string help = "")
                : base(value.ToString(), help)
            {
                this.value = value;
            }

            public readonly Enum value;
        }

        /// <summary>
        /// Initializes a new instance of the EnumValuesAdapter class.
        /// </summary>
        /// <param name="title">The title to display when prompting for search.</param>
        public EnumValuesAdapter(string title)
            : base(title) {}
    }
}
