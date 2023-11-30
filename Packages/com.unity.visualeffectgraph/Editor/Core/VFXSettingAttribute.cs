using System;
using System.Reflection;

using UnityEditor.VFX.UI;

namespace UnityEditor.VFX
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    class VFXSettingFieldTypeAttribute : Attribute
    {
        public Type type { get; }

        public VFXSettingFieldTypeAttribute(Type type)
        {
            if (type.IsSubclassOf(typeof(PropertyRM)))
            {
                this.type = type;
            }
            else
            {
                throw new InvalidOperationException("When you specify the setting property editor the type must derive from `PropertyRM`");
            }
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    class VFXSettingAttribute : Attribute
    {
        [Flags]
        public enum VisibleFlags
        {
            InInspector = 1 << 0,
            InGraph = 1 << 1,
            InGeneratedCodeComments = 1 << 2,
            ReadOnly = 1 << 3,
            Default = InGraph | InInspector | InGeneratedCodeComments,
            None = 0
        }

        public VFXSettingAttribute(VisibleFlags flags = VisibleFlags.Default)
        {
            visibleFlags = flags;
        }

        public readonly VisibleFlags visibleFlags;
    }

    struct VFXSetting
    {
        public readonly FieldInfo field;
        public readonly object instance;

        public VFXSetting(FieldInfo field, object instance, VFXSettingAttribute.VisibleFlags visibility = VFXSettingAttribute.VisibleFlags.Default)
        {
            this.field = field;
            this.instance = instance;
            this.visibility = visibility;
            this.valid = field != null && instance != null;
            this.name = field?.Name ?? null;
        }

        public bool valid { get; }
        public string name { get; }
        public object value => field.GetValue(instance);
        public VFXSettingAttribute.VisibleFlags visibility { get; private set; }

        public void SetReadOnly()
        {
            visibility |= VFXSettingAttribute.VisibleFlags.ReadOnly;
        }

        public override string ToString()
        {
            return field != null && value != null
                ? $"{field.Name}:{value}"
                : string.Empty;
        }
    }
}
