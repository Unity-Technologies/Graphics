using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.VFX.Utility
{
    /// <summary>
    /// A ClassAttribute for use with VFXBinderBase in order to set the Add Menu Path in Inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class VFXBinderAttribute : PropertyAttribute
    {
        /// <summary>
        /// The Specified Menu Path for this VFXBinder
        /// </summary>
        public string MenuPath;

        /// <summary>
        /// Specifies a Add Menu Path for this VFXBinder.
        /// </summary>
        /// <param name="menuPath"></param>
        public VFXBinderAttribute(string menuPath)
        {
            MenuPath = menuPath;
        }
    }
}
