using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Linq;
using System.Reflection;
using Type = System.Type;
using Delegate = System.Delegate;

namespace UnityEditor.VFX.UI
{
    public static class VFXGizmoUtility
    {
        static Dictionary<System.Type, VFXGizmo> s_DrawFunctions;

        internal class Property<T> : VFXGizmo.IProperty<T>
        {
            public Property(IPropertyRMProvider controller, bool editable)
            {
                m_Controller = controller;
                m_Editable = editable;
            }

            IPropertyRMProvider m_Controller;

            bool m_Editable;

            public bool isEditable
            {
                get { return m_Editable; }
            }
            public void SetValue(T value)
            {
                if (m_Editable)
                    m_Controller.value = value;
            }
        }

        internal class NullProperty<T> : VFXGizmo.IProperty<T>
        {
            public bool isEditable
            {
                get { return false; }
            }
            public void SetValue(T value)
            {
            }

            public static NullProperty<T> defaultProperty = new NullProperty<T>();
        }

        public abstract class Context : VFXGizmo.IContext
        {
            public abstract Type portType
            {
                get;
            }

            bool m_Prepared;

            public void Unprepare()
            {
                m_Prepared = false;
            }

            public bool Prepare()
            {
                if (m_Prepared)
                    return false;
                m_Prepared = true;
                m_Indeterminate = false;
                m_PropertyCache.Clear();
                InternalPrepare();
                return true;
            }

            protected abstract void InternalPrepare();

            public const string separator = ".";

            public abstract object value
            {
                get;
            }


            protected Dictionary<string, object> m_PropertyCache = new Dictionary<string, object>();

            public abstract VFXGizmo.IProperty<T> RegisterProperty<T>(string member);

            protected bool m_Indeterminate;

            public bool IsIndeterminate()
            {
                return m_Indeterminate;
            }
        }

        static VFXGizmoUtility()
        {
            s_DrawFunctions = new Dictionary<System.Type, VFXGizmo>();

            foreach (Type type in typeof(VFXGizmoUtility).Assembly.GetTypes()) // TODO put all user assemblies instead
            {
                Type gizmoedType = GetGizmoType(type);

                if (gizmoedType != null)
                {
                    s_DrawFunctions[gizmoedType] = (VFXGizmo)System.Activator.CreateInstance(type);
                }
            }
        }

        public static bool HasGizmo(Type type)
        {
            return s_DrawFunctions.ContainsKey(type);
        }

        static Type GetGizmoType(Type type)
        {
            if (type.IsAbstract)
                return null;
            Type baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && !baseType.IsGenericTypeDefinition && baseType.GetGenericTypeDefinition() == typeof(VFXGizmo<>))
                {
                    return baseType.GetGenericArguments()[0];
                }
                baseType = baseType.BaseType;
            }
            return null;
        }

        static internal void Draw(Context context, VisualEffect component)
        {
            VFXGizmo gizmo;
            if (s_DrawFunctions.TryGetValue(context.portType, out gizmo))
            {
                if (context.Prepare())
                {
                    gizmo.RegisterEditableMembers(context);
                }
                if (!context.IsIndeterminate())
                {
                    gizmo.component = component;
                    gizmo.CallDrawGizmo(context.value);
                    gizmo.component = null;
                }
            }
        }
    }
}
