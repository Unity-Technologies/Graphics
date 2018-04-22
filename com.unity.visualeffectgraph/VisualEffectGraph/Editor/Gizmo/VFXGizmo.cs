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
            bool isEditable { get; }

            void SetValue(T value);
        }

        public interface IContext
        {
            IProperty<T> RegisterProperty<T>(string memberPath);
        }
        public abstract void RegisterEditableMembers(IContext context);
        public abstract void CallDrawGizmo(object value);

        protected const float handleSize = 0.1f;
        protected const float arcHandleSizeMultiplier = 1.25f;

        protected CoordinateSpace m_CurrentSpace;
        public VisualEffect component {get;set;}

        public bool PositionGizmo(ref Vector3 position, bool always)
        {
            if(always || Tools.current == Tool.Move || Tools.current == Tool.Transform)
            {
                EditorGUI.BeginChangeCheck();
                position = Handles.PositionHandle(position, m_CurrentSpace == CoordinateSpace.Local ? component.transform.rotation : Quaternion.identity);
                return EditorGUI.EndChangeCheck();
            }
            return false;
        }

        public bool PositionGizmo(Vector3 position, IProperty<Vector3> positionProperty, bool always)
        {
            if (positionProperty.isEditable && PositionGizmo(ref position, true))
            {
                positionProperty.SetValue(position);
                return true;
            }
            return false;
        }

        public bool RotationGizmo(Vector3 position, Vector3 rotation, IProperty<Vector3> anglesProperty, bool always)
        {
            if( anglesProperty.isEditable && RotationGizmo(position,ref rotation, always))
            {
                anglesProperty.SetValue(rotation);
                return true;
            }
            return false;
        }

        public bool RotationGizmo(Vector3 position, ref Vector3 rotation,bool always)
        {
            Quaternion quaternion = Quaternion.Euler(rotation);

            bool result = RotationGizmo(position, ref quaternion, always);
            if(result)
            {
                rotation = quaternion.eulerAngles;
                return true;
            }
            return false;
        }
        public bool RotationGizmo(Vector3 position, ref Quaternion rotation,bool always)
        {
            if (always || Tools.current == Tool.Rotate || Tools.current == Tool.Transform)
            {
                EditorGUI.BeginChangeCheck();

                rotation = Handles.RotationHandle(rotation, position);

                return EditorGUI.EndChangeCheck();
            }
            return false;
        }

        public bool ArcGizmo(Vector3 center, float radius, float degArc,IProperty<float> arcProperty, Quaternion rotation, bool always)
        {
            // Arc handle control
            if (arcProperty.isEditable && (always || Tools.current == Tool.Rotate || Tools.current == Tool.Transform))
            {
                using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Translate(center) * Matrix4x4.Rotate(rotation)))
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 arcHandlePosition =  Quaternion.AngleAxis(degArc, Vector3.up) * Vector3.forward * radius;
                    arcHandlePosition = Handles.Slider2D(
                                arcHandlePosition,
                                Vector3.up,
                                Vector3.forward,
                                Vector3.right,
                                handleSize * arcHandleSizeMultiplier * HandleUtility.GetHandleSize(arcHandlePosition),
                                DefaultAngleHandleDrawFunction,
                                0
                                );
                    if (EditorGUI.EndChangeCheck())
                    {
                        float newArc = Vector3.Angle(Vector3.forward, arcHandlePosition) * Mathf.Sign(Vector3.Dot(Vector3.right, arcHandlePosition));
                        degArc += Mathf.DeltaAngle(degArc, newArc);
                        degArc = Mathf.Repeat(degArc, 360.0f);
                        arcProperty.SetValue(degArc * Mathf.Deg2Rad);
                        return true;
                    }
                }
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
        public override void CallDrawGizmo(object value)
        {
            m_CurrentSpace = CoordinateSpace.Global;
            OnDrawGizmo((T)value);
        }
        public abstract void OnDrawGizmo(T value);

    }
    public abstract class VFXSpaceableGizmo<T> : VFXGizmo<T> where T : ISpaceable
    {
        public override void OnDrawGizmo(T value)
        {
            m_CurrentSpace = value.space;
            Matrix4x4 oldMatrix = Handles.matrix;

            if (value.space == CoordinateSpace.Local)
            {
                if (component == null) return;
                Handles.matrix = component.transform.localToWorldMatrix;
            }

            OnDrawSpacedGizmo(value);

            Handles.matrix = oldMatrix;
        }
        public abstract void OnDrawSpacedGizmo(T value);
    }
}



