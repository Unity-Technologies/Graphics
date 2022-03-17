using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods to build value editors for constants.
    /// </summary>
    [GraphElementsExtensionMethodsCache(typeof(RootView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    public static class ConstantEditorExtensions
    {
        /// <summary>
        /// Factory method to create default constant editors.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="constant">The constant for which to build an editor.</param>
        /// <returns>The editor.</returns>
        public static BaseModelPropertyField BuildDefaultConstantEditor(this IConstantEditorBuilder builder, IConstant constant)
        {
            // Disable warning for use of obsolete builder.OnValueChanged
#pragma warning disable 612
            return new ConstantField(constant, builder.ConstantOwner, builder.CommandTarget, builder.OnValueChanged, builder.Label);
#pragma warning restore 612
        }
    }
}
