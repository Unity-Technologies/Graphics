using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEngine.VFX
{
    /// <summary>
    /// Attribute to define a VFXType that will be usable in VFX Graph view.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class VFXTypeAttribute : Attribute
    {
        /// <summary>Usage of VFXType.</summary>
        [Flags]
        public enum Usage
        {
            /// <summary>Usable as a port type.</summary>
            Default,
            /// <summary>Usable as a GraphicsBuffer data type.</summary>
            GraphicsBuffer
        }

        /// <summary>Constructor of VFXType.</summary>
        /// <param name="usages">Usage of the VFXType.</param>
        public VFXTypeAttribute(Usage usages = Usage.Default)
        {
            this.usages = usages;
        }

        internal Usage usages { get; private set; }
    }
}
