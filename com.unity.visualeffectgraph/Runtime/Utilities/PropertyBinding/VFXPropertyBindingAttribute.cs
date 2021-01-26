using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.VFX.Utility
{
    /// <summary>
    /// A ClassAttribute for use with ExposedProperty in order to specify the compatible type of a bound property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class VFXPropertyBindingAttribute : PropertyAttribute
    {
        /// <summary>
        /// The array of compatible editor types, expressed as string including namespace.
        /// For instance, the value returned by `typeof(float).FullName` (System.Single).
        /// </summary>
        public string[] EditorTypes;

        /// <summary>
        /// Specify the compatible type of a bound property.
        /// </summary>
        /// <param name="editorTypes">The array of compatible editor types, expressed as string including namespace. For instance, the value returned by `typeof(float).FullName` (System.Single). </param>
        public VFXPropertyBindingAttribute(params string[] editorTypes)
        {
            EditorTypes = editorTypes;
        }
    }
}
