using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEditor.VFX
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    class VFXSettingAttribute : Attribute
    {
        public bool hidden
        {
            get;
            set;
        }

        public static IEnumerable<FieldInfo> Collect(Object owner, bool listHidden = false)
        {
            if (owner == null)
                return Enumerable.Empty<FieldInfo>();

            return owner.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(f =>
                {
                    var attrArray = f.GetCustomAttributes(typeof(VFXSettingAttribute), true);
                    if (attrArray.Length == 1)
                    {
                        var attr = attrArray[0] as VFXSettingAttribute;
                        if (!attr.hidden || listHidden)
                        {
                            return true;
                        }
                    }
                    return false;
                });
        }
    }
}
