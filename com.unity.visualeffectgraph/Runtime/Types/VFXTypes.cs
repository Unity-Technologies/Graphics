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
        public enum Flags
        {
            Default,
            GraphicsBuffer
        }
        public VFXTypeAttribute(Flags flags = Flags.Default)
        {
            this.flags = flags;
        }

        public Flags flags { get; private set; }
    }
}
