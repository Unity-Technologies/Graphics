using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InspectorUseSetterMethodAttribute : Attribute
    {
        /// <summary>
        /// The name of the method to use.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectorUseSetterMethodAttribute"/> class.
        /// </summary>
        /// <param name="methodName">The name of the method to use.</param>
        public InspectorUseSetterMethodAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
