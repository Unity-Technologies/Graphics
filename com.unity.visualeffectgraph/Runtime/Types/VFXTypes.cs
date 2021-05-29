using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEngine.VFX
{
    /// <summary>
    /// Defines a VFXType that you can use in Node Workspace.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class VFXTypeAttribute : Attribute
    {
        /// <summary>
        /// Options that define how the Visual Effect Graph can use a VFXType.
        /// </summary>
        [Flags]
        public enum Usage
        {
            /// <summary>Usable as a port type.</summary>
            Default,
            /// <summary>Usable as a GraphicsBuffer data type.</summary>
            GraphicsBuffer
        }

        /// <summary>
        /// Initializes and returns an instance of VFXType.
        /// </summary>
        /// <param name="usages">Flags that set how the Visual Effect Graph can use the VFXType.</param>
        public VFXTypeAttribute(Usage usages = Usage.Default)
        {
            this.usages = usages;
        }

        internal Usage usages { get; private set; }
    }
}
