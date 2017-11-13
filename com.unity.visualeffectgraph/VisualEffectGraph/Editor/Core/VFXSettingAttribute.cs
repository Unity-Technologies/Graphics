using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEditor.VFX
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    class VFXSettingAttribute : Attribute
    {
        public VFXSettingAttribute(bool inspectorOnly = false)
        {
            this.inspectorOnly = inspectorOnly;
        }

        public readonly bool inspectorOnly;
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

            return owner.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(f =>
                {
                    return f.GetCustomAttributes(typeof(VFXSettingAttribute), true).Length == 1 &&
                    !(f.GetCustomAttributes(typeof(VFXSettingAttribute), true)[0] as VFXSettingAttribute).inspectorOnly &&
                    IsTypeSupported(f.FieldType);
                });
        }
    }
}
