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
            /// <summary>Usable as a port type and exposable parameter.</summary>
            Default = 1 << 0,
            /// <summary>Usable as a GraphicsBuffer data type.</summary>
            GraphicsBuffer = 1 << 1,
            /// <summary>Exclude from property and exposable parameters.</summary>
            ExcludeFromProperty = 1 << 2,
        }

        /// <summary>
        /// Initializes and returns an instance of VFXType.
        /// </summary>
        /// <param name="usages">Flags that set how the Visual Effect Graph can use the VFXType.</param>
        /// <param name="name">Custom name of the VFXType (can be null).</param>
        public VFXTypeAttribute(Usage usages = Usage.Default, string name = null)
        {
            this.usages = usages;
            this.name = name;
        }

        internal Usage usages { get; private set; }

        internal string name { get; private set; }
    }
}
