using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Linq;
using System.Reflection;
using Type = System.Type;
using Delegate = System.Delegate;

namespace UnityEditor.VFX
{
    public abstract class VFXGizmo
    {
        public interface IProperty<T>
        {
            bool isEditable{get;}

            void SetValue(T value);
        }

        public interface IContext
        {
            IProperty<T> RegisterProperty<T>(string memberPath);
        }
        public abstract void RegisterEditableMembers(IContext context);
        public abstract void CallDrawGizmo(object value, VisualEffect component);

        protected const float handleSize = 0.1f;
        protected const float arcHandleSizeMultiplier = 1.25f;

        protected CoordinateSpace m_CurrentSpace;

        public bool PositionGizmo(VisualEffect component, ref Vector3 position)
        {
            EditorGUI.BeginChangeCheck();
            position = Handles.PositionHandle(position, m_CurrentSpace == CoordinateSpace.Local ? component.transform.rotation : Quaternion.identity);
            return EditorGUI.EndChangeCheck();
        }
        public bool RotationGizmo(VisualEffect component, Vector3 position, ref Vector3 rotation)
        {
            EditorGUI.BeginChangeCheck();

            Quaternion modifiedRotation = Handles.RotationHandle(Quaternion.Euler(rotation), position);

            if (EditorGUI.EndChangeCheck())
            {
                rotation = modifiedRotation.eulerAngles;
                return true;
            }
            return false;
        }
        public static void DefaultAngleHandleDrawFunction(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            Handles.DrawLine(Vector3.zero, position);

            // draw a cylindrical "hammer head" to indicate the direction the handle will move
            Vector3 worldPosition = Handles.matrix.MultiplyPoint3x4(position);
            Vector3 normal = worldPosition - Handles.matrix.MultiplyPoint3x4(Vector3.zero);
            Vector3 tangent = Handles.matrix.MultiplyVector(Quaternion.AngleAxis(90f, Vector3.up) * position);
            rotation = Quaternion.LookRotation(tangent, normal);
            Matrix4x4 matrix = Matrix4x4.TRS(worldPosition, rotation, (Vector3.one + Vector3.forward * arcHandleSizeMultiplier));
            using (new Handles.DrawingScope(matrix))
                Handles.CylinderHandleCap(controlID, Vector3.zero, Quaternion.identity, size, eventType);
        }
    }

    public abstract class VFXGizmo<T> : VFXGizmo
    {
        public override void CallDrawGizmo(object value, VisualEffect component)
        {
            m_CurrentSpace = CoordinateSpace.Global;
            OnDrawGizmo((T)value,component);
        }
        public abstract void OnDrawGizmo(T value, VisualEffect component);

    }
    public abstract class VFXSpaceableGizmo<T> : VFXGizmo<T> where T : ISpaceable
    {
        public override void OnDrawGizmo(T value, VisualEffect component)
        {
            m_CurrentSpace = value.space;
            Matrix4x4 oldMatrix = Handles.matrix;

            if (value.space == CoordinateSpace.Local)
            {
                if (component == null) return;
                Handles.matrix = component.transform.localToWorldMatrix;
            }

            OnDrawSpacedGizmo(value,component);

            Handles.matrix = oldMatrix;
        }
        public abstract void OnDrawSpacedGizmo(T value, VisualEffect component);
    }
}



