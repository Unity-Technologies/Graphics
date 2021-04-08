using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEngine.VFX
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class VFXTypeAttribute : Attribute
    {
        [Flags]
        public enum Usage
        {
            Default,
            GraphicsBuffer
        }
        public VFXTypeAttribute(Usage usages = Usage.Default)
        {
            this.usages = usages;
        }

        internal Usage usages { get; private set; }
    }
}
