using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEditor.VFX
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    class VFXSettingAttribute : Attribute
    {
    }
}
