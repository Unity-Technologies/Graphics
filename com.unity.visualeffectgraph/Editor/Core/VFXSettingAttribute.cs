using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEditor.VFX
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    class VFXSettingAttribute : Attribute
    {
        [Flags]
        public enum VisibleFlags
        {
            InInspector = 1 << 0,
            InGraph = 1 << 1,
            Default = InGraph | InInspector,
            All = 0xFFFF,
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
        public FieldInfo field;
        public VFXModel instance;

        public VFXSetting(FieldInfo field, VFXModel instance)
        {
            this.field = field;
            this.instance = instance;
        }

        public bool valid => field != null && instance != null;
        public string name => field != null ? field.Name : null;

        public object value
        {
            get
            {
                return field.GetValue(instance);
            }

            set
            {
                field.SetValue(instance, value);
                instance.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
            }
        }

    }
}
