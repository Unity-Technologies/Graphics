using System.Collections.Generic;
using System.Text;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// A class that aggregates attributes and their usage.
    /// </summary>
    /*public*/ class AttributeSet
    {
        private HashSet<Attribute> readAttributes = new();
        private HashSet<Attribute> writeAttributes = new();
        /// <summary>
        /// An enumerable containing all the attributes that are read.
        /// </summary>
        public IEnumerable<Attribute> ReadAttributes => readAttributes;
        /// <summary>
        /// An enumerable containing all the attributes that are written to.
        /// </summary>
        public IEnumerable<Attribute> WriteAttributes => writeAttributes;

        /// <summary>
        /// Adds an attribute to the attribute set, with its usage.
        /// </summary>
        /// <param name="attribute"> The attribute to add to the set.</param>
        /// <param name="usage">The attribute usage of the added attribute.</param>
        public void AddAttribute(Attribute attribute, AttributeUsage usage)
        {
            if (usage.HasFlag(AttributeUsage.Read))
            {
                readAttributes.Add(attribute);
            }
            if (usage.HasFlag(AttributeUsage.Write))
            {
                writeAttributes.Add(attribute);
            }
        }

        /// <summary>
        /// Aggregates an attribute set to the current attribute set.
        /// </summary>
        /// <param name="attributeSet">The attribute set to whose elements will be added to the current attribute set.</param>
        public void Append(AttributeSet attributeSet)
        {
            foreach (var attribute in attributeSet.ReadAttributes)
                AddAttribute(attribute, AttributeUsage.Read);

            foreach (var attribute in attributeSet.WriteAttributes)
                AddAttribute(attribute, AttributeUsage.Write);
        }

        /// <summary>
        /// Checks if the attribute set contains any attribute.
        /// </summary>
        /// <returns>True if the attribute set does not contain any attribute.</returns>
        public bool IsEmpty()
        {
            return readAttributes.Count == 0 && writeAttributes.Count == 0;
        }

        /// <summary>
        /// Generates a string listing all the attributes that are read and written.
        /// </summary>
        /// <returns>A string listing all the attributes that are read and written.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var attribute in ReadAttributes)
            {
                sb.Append($"\tAttribute read : {attribute.Name}");
                sb.AppendLine();
            }

            foreach (var attribute in WriteAttributes)
            {
                sb.Append($"\tAttribute written : {attribute.Name}");
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
