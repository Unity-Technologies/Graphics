using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Attribute used to tell the model inspector to use a property to get and set the field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InspectorUsePropertyAttribute : Attribute
    {
        /// <summary>
        /// The name of the property to use.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectorUsePropertyAttribute"/> class.
        /// </summary>
        /// <param name="propertyName">The name of the property to use.</param>
        public InspectorUsePropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}
