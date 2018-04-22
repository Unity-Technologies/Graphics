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
    public class VFXGizmoUtility
    {
        static Dictionary<System.Type,VFXGizmo> s_DrawFunctions;

        internal class Property<T> : VFXGizmo.IProperty<T>
        {
            public Property(IPropertyRMProvider controller,bool editable)
            {
                m_Controller = controller;
                m_Editable = editable;
            }
            IPropertyRMProvider m_Controller;

            bool m_Editable;

            public bool isEditable
            {
                get{ return m_Editable;}
            }
            public void SetValue(T value)
            {
                if( m_Editable)
                    m_Controller.value = value;
            }
        }

        internal class NullProperty<T> : VFXGizmo.IProperty<T>
        {
             public bool isEditable
            {
                get{ return false;}
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
                if( m_Prepared)
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


            protected Dictionary<string,object> m_PropertyCache = new Dictionary<string,object>();

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

        static Type GetGizmoType(Type type)
        {
            if( type.IsAbstract ) 
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
                if(context.Prepare())
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
#if false

        static void OnDrawCylinderDataAnchorGizmo(Context context, VisualEffect component)
        {
            Cylinder cylinder = (Cylinder)context.value;

            Vector3 center = cylinder.center;
            Vector3 normal = Vector3.up;

            Vector3 worldNormal = normal;

            Vector3 topCap = cylinder.height * 0.5f * Vector3.up;
            Vector3 bottomCap = -cylinder.height * 0.5f * Vector3.up;

            Vector3[] extremities = new Vector3[8];

            extremities[0] = topCap + Vector3.forward * cylinder.radius;
            extremities[1] = topCap - Vector3.forward * cylinder.radius;

            extremities[2] = topCap + Vector3.left * cylinder.radius;
            extremities[3] = topCap - Vector3.left * cylinder.radius;

            extremities[4] = bottomCap + Vector3.forward * cylinder.radius;
            extremities[5] = bottomCap - Vector3.forward * cylinder.radius;

            extremities[6] = bottomCap + Vector3.left * cylinder.radius;
            extremities[7] = bottomCap - Vector3.left * cylinder.radius;


            Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, normal);

            for (int i = 0; i < extremities.Length; ++i)
            {
                extremities[i] = normalRotation * extremities[i];
            }

            topCap = normalRotation * topCap;
            bottomCap = normalRotation * bottomCap;

            for (int i = 0; i < extremities.Length; ++i)
            {
                extremities[i] = center + extremities[i];
            }

            topCap += center;
            bottomCap += center;


            if (cylinder.space == CoordinateSpace.Local)
            {
                if (component == null) return;
                Matrix4x4 mat = component.transform.localToWorldMatrix;

                center = mat.MultiplyPoint(center);
                topCap = mat.MultiplyPoint(topCap);
                bottomCap = mat.MultiplyPoint(bottomCap);

                worldNormal = mat.MultiplyVector(normal).normalized;

                for (int i = 0; i < extremities.Length; ++i)
                {
                    extremities[i] = mat.MultiplyPoint(extremities[i]);
                }
            }

            Handles.DrawWireDisc(topCap, worldNormal, cylinder.radius);
            Handles.DrawWireDisc(bottomCap, worldNormal, cylinder.radius);

            for (int i = 0; i < extremities.Length / 2; ++i)
            {
                Handles.DrawLine(extremities[i], extremities[i + extremities.Length / 2]);
            }

            if (PositionGizmo(component, cylinder.space, ref cylinder.center))
            {
                context.value = cylinder;
            }

            Vector3 result;
            for (int i = 0; i < extremities.Length / 2; ++i)
            {
                EditorGUI.BeginChangeCheck();

                Vector3 pos = (extremities[i] + extremities[i + +extremities.Length / 2]) * 0.5f;
                result = Handles.Slider(pos, pos - center, handleSize * HandleUtility.GetHandleSize(pos), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    cylinder.radius = (result - center).magnitude;
                    context.value = cylinder;
                }

                EditorGUI.EndChangeCheck();
            }

            EditorGUI.BeginChangeCheck();

            result = Handles.Slider(topCap, topCap - center, handleSize * HandleUtility.GetHandleSize(topCap), Handles.CubeHandleCap, 0);

            if (GUI.changed)
            {
                cylinder.height = (result - center).magnitude * 2;
                context.value = cylinder;
            }

            EditorGUI.EndChangeCheck();

            EditorGUI.BeginChangeCheck();

            result = Handles.Slider(bottomCap, bottomCap - center, handleSize * HandleUtility.GetHandleSize(bottomCap), Handles.CubeHandleCap, 0);

            if (GUI.changed)
            {
                cylinder.height = (result - center).magnitude * 2;
                context.value = cylinder;
            }

            EditorGUI.EndChangeCheck();
        }

        static bool OnDrawBoxDataAnchorGizmo(Context context, VisualEffect component, CoordinateSpace space, ref Vector3 center, ref Vector3 size, Vector3 additionnalRotation)
        {
            var saveMatrix = Handles.matrix;
            if (space == CoordinateSpace.Local)
            {
                if (component == null) return false;
                Matrix4x4 addMat = Matrix4x4.Rotate(Quaternion.Euler(additionnalRotation));

                addMat *= component.transform.localToWorldMatrix;
                Handles.matrix = addMat;
            }
            Vector3[] points = new Vector3[8];


            points[0] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[1] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[2] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[3] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[4] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[5] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);

            points[6] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[7] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);


            Matrix4x4 mat = Matrix4x4.identity;

            Handles.DrawLine(points[0], points[1]);
            Handles.DrawLine(points[2], points[3]);
            Handles.DrawLine(points[4], points[5]);
            Handles.DrawLine(points[6], points[7]);

            Handles.DrawLine(points[0], points[2]);
            Handles.DrawLine(points[0], points[4]);
            Handles.DrawLine(points[1], points[3]);
            Handles.DrawLine(points[1], points[5]);

            Handles.DrawLine(points[2], points[6]);
            Handles.DrawLine(points[3], points[7]);
            Handles.DrawLine(points[4], points[6]);
            Handles.DrawLine(points[5], points[7]);

            bool changed = false;

            EditorGUI.BeginChangeCheck();

            Handles.color = Color.blue;
            {
                // axis +Z
                Vector3 middle = (points[0] + points[1] + points[2] + points[3]) * 0.25f;
                Vector3 othermiddle = (points[4] + points[5] + points[6] + points[7]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.z = (middleResult - othermiddle).magnitude;
                    center = (middleResult + othermiddle) * 0.5f;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis -Z
                Vector3 middle = (points[4] + points[5] + points[6] + points[7]) * 0.25f;
                Vector3 othermiddle = (points[0] + points[1] + points[2] + points[3]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.z = (middleResult - othermiddle).magnitude;
                    center = (middleResult + othermiddle) * 0.5f;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();


            Handles.color = Color.red;
            EditorGUI.BeginChangeCheck();
            {
                // axis +X
                Vector3 middle = (points[0] + points[1] + points[4] + points[5]) * 0.25f;
                Vector3 othermiddle = (points[2] + points[3] + points[6] + points[7]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.x = (middleResult - othermiddle).magnitude;
                    center = (middleResult + othermiddle) * 0.5f;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis -X
                Vector3 middle = (points[2] + points[3] + points[6] + points[7]) * 0.25f;
                Vector3 othermiddle = (points[0] + points[1] + points[4] + points[5]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.x = (middleResult - othermiddle).magnitude;
                    center = (middleResult + othermiddle) * 0.5f;
                    changed = true;
                }
            }

            EditorGUI.EndChangeCheck();

            Handles.color = Color.green;
            EditorGUI.BeginChangeCheck();
            {
                // axis +Y
                Vector3 middle = (points[0] + points[2] + points[4] + points[6]) * 0.25f;
                Vector3 othermiddle = (points[1] + points[3] + points[5] + points[7]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.y = (middleResult - othermiddle).magnitude;
                    center = (middleResult + othermiddle) * 0.5f;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();
            EditorGUI.BeginChangeCheck();
            {
                // axis -Y
                Vector3 middle = (points[1] + points[3] + points[5] + points[7]) * 0.25f;
                Vector3 othermiddle = (points[0] + points[2] + points[4] + points[6]) * 0.25f;
                Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    size.y = (middleResult - othermiddle).magnitude;
                    center = (middleResult + othermiddle) * 0.5f;
                    changed = true;
                }
            }
            EditorGUI.EndChangeCheck();


            if (PositionGizmo(component, space, ref center))
            {
                changed = true;
            }


            Handles.matrix = saveMatrix;
            return changed;
        }

        static void OnDrawArcConeDataAnchorGizmo(Context context, VisualEffect component)
        {
            ArcCone cone = (ArcCone)context.value;

            Vector3 center = cone.center;
            Vector3 normal = Vector3.up;

            Vector3 worldNormal = normal;

            Vector3 topCap = cone.height * Vector3.up;
            Vector3 bottomCap = Vector3.zero;

            Vector3[] extremities = new Vector3[8];

            extremities[0] = topCap + Vector3.forward * cone.radius1;
            extremities[1] = topCap - Vector3.forward * cone.radius1;

            extremities[2] = topCap + Vector3.left * cone.radius1;
            extremities[3] = topCap - Vector3.left * cone.radius1;

            extremities[4] = bottomCap + Vector3.forward * cone.radius0;
            extremities[5] = bottomCap - Vector3.forward * cone.radius0;

            extremities[6] = bottomCap + Vector3.left * cone.radius0;
            extremities[7] = bottomCap - Vector3.left * cone.radius0;

            Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, normal);

            for (int i = 0; i < extremities.Length; ++i)
            {
                extremities[i] = normalRotation * extremities[i];
            }

            topCap = normalRotation * topCap;
            bottomCap = normalRotation * bottomCap;

            for (int i = 0; i < extremities.Length; ++i)
            {
                extremities[i] = center + extremities[i];
            }

            topCap += center;
            bottomCap += center;

            if (cone.space == CoordinateSpace.Local)
            {
                if (component == null) return;
                Matrix4x4 mat = component.transform.localToWorldMatrix;

                center = mat.MultiplyPoint(center);
                topCap = mat.MultiplyPoint(topCap);
                bottomCap = mat.MultiplyPoint(bottomCap);

                worldNormal = mat.MultiplyVector(normal).normalized;

                for (int i = 0; i < extremities.Length; ++i)
                {
                    extremities[i] = mat.MultiplyPoint(extremities[i]);
                }
            }

            Handles.DrawWireDisc(topCap, worldNormal, cone.radius1);
            Handles.DrawWireDisc(bottomCap, worldNormal, cone.radius0);

            for (int i = 0; i < extremities.Length / 2; ++i)
            {
                Handles.DrawLine(extremities[i], extremities[i + extremities.Length / 2]);
            }

            if (PositionGizmo(component, cone.space, ref cone.center))
            {
                context.value = cone;
            }

            Vector3 result;
            for (int i = 0; i < extremities.Length; ++i)
            {
                EditorGUI.BeginChangeCheck();

                Vector3 pos = extremities[i];
                result = Handles.Slider(pos, pos - center, handleSize * HandleUtility.GetHandleSize(pos), Handles.CubeHandleCap, 0);

                if (GUI.changed)
                {
                    if (i >= extremities.Length / 2)
                        cone.radius0 = (result - center).magnitude;
                    else
                        cone.radius1 = (result - topCap).magnitude;
                    context.value = cone;
                }

                EditorGUI.EndChangeCheck();
            }

            EditorGUI.BeginChangeCheck();

            result = Handles.Slider(topCap, topCap - center, handleSize * HandleUtility.GetHandleSize(topCap), Handles.CubeHandleCap, 0);

            if (GUI.changed)
            {
                cone.height = (result - center).magnitude;
                context.value = cone;
            }

            EditorGUI.EndChangeCheck();
        }

#endif
    }
}



