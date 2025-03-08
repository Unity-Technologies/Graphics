using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(Position))]
    class VFXPositionGizmo : VFXSpaceableGizmo<Position>
    {
        IProperty<Position> m_Property;
        public override void RegisterEditableMembers(IContext context)
        {
            m_Property = context.RegisterProperty<Position>("");
        }

        public override void OnDrawSpacedGizmo(Position position)
        {
            if (!VFXTypeUtility.IsFinite(position.position))
                return;

            PositionOnlyGizmo(position, m_Property);
        }

        public override Bounds OnGetSpacedGizmoBounds(Position value)
        {
            return new Bounds(value.position, Vector3.one);
        }
    }
    [VFXGizmo(typeof(DirectionType))]
    class VFXDirectionGizmo : VFXSpaceableGizmo<DirectionType>
    {
        IProperty<DirectionType> m_Property;
        public override void RegisterEditableMembers(IContext context)
        {
            m_Property = context.RegisterProperty<DirectionType>("");
        }

        public override void OnDrawSpacedGizmo(DirectionType direction)
        {
            if (!VFXTypeUtility.IsFinite(direction.direction))
                return;

            direction.direction.Normalize();
            if (direction.direction == Vector3.zero)
            {
                direction.direction = Vector3.up;
            }

            var normalQuat = Quaternion.FromToRotation(Vector3.forward, direction.direction);

            var ray = HandleUtility.GUIPointToWorldRay(Vector2.one * 200);
            var position = ray.origin + ray.direction * 2;

            using (new Handles.DrawingScope(Matrix4x4.TRS(position, Handles.matrix.rotation, Vector3.one)))
            {
                Handles.ArrowHandleCap(currentHashCode, Vector3.zero, normalQuat, HandleUtility.GetHandleSize(Vector3.zero), Event.current.type);
                if (m_Property.isEditable && NormalGizmo(Vector3.zero, ref direction.direction, true))
                {
                    direction.direction.Normalize();
                    m_Property.SetValue(direction);
                }
            }
        }

        Quaternion m_PrevQuaternion;

        public override Bounds OnGetSpacedGizmoBounds(DirectionType value)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }
    }
    [VFXGizmo(typeof(Vector))]
    class VFXVectorGizmo : VFXSpaceableGizmo<Vector>
    {
        IProperty<Vector> m_Property;
        public override void RegisterEditableMembers(IContext context)
        {
            m_Property = context.RegisterProperty<Vector>("");
        }

        public override void OnDrawSpacedGizmo(Vector vector)
        {
            if (!VFXTypeUtility.IsFinite(vector.vector))
                return;

            if (vector.vector == Vector3.zero)
            {
                vector.vector = Vector3.up;
            }

            if (m_Property.isEditable && NormalGizmo(Vector3.zero, ref vector.vector, true))
            {
                m_Property.SetValue(vector);
            }

            Handles.DrawLine(Vector3.zero, vector.vector);

            // Prevent gizmo highlight when not editable
            var prevNearestControl = HandleUtility.nearestControl;
            if (!m_Property.isEditable)
            {
                HandleUtility.nearestControl = -1;
            }

            if (float.IsFinite(vector.vector.sqrMagnitude))
            {
                EditorGUI.BeginChangeCheck();
                var result = Handles.Slider(vector.vector, vector.vector, handleSize * 2 * HandleUtility.GetHandleSize(vector.vector), CustomConeHandleCap, 0);
                var changed = EditorGUI.EndChangeCheck();

                if (changed && m_Property.isEditable)
                {
                    vector.vector = result;
                    m_Property.SetValue(vector);
                }
            }

            if (!m_Property.isEditable)
            {
                HandleUtility.nearestControl = prevNearestControl;
            }
        }

        public override Bounds OnGetSpacedGizmoBounds(Vector value)
        {
            return new Bounds(Vector3.zero, Vector3.one * value.vector.magnitude * 2);
        }
    }
}
