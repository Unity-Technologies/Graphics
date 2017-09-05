using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEditor.VFX
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    class VFXSettingAttribute : Attribute
    {
        public static bool IsTypeSupported(Type type)
        {
            return type.IsEnum ||
                type == typeof(bool) ||
                type == typeof(string);
        }

        public static IEnumerable<FieldInfo> Collect(Object owner)
        {
            if (owner == null)
                return Enumerable.Empty<FieldInfo>();

            return owner.GetType().GetFields().Where(f =>
                {
                    return !f.IsStatic &&
                    f.GetCustomAttributes(typeof(VFXSettingAttribute), true).Length == 1 &&
                    IsTypeSupported(f.FieldType);
                });
        }
    }
}
