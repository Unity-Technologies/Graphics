using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXPositionGizmo : VFXSpaceableGizmo<Position>
    {
        IProperty<Position> m_Property;
        public override void RegisterEditableMembers(IContext context)
        {
            m_Property = context.RegisterProperty<Position>("");
        }
        public override void OnDrawSpacedGizmo(Position position)
        {
            if( m_Property.isEditable && PositionGizmo(ref position.position, true))
            {
                m_Property.SetValue(position);
            }
        }
    }
    class VFXDirectionGizmo : VFXSpaceableGizmo<DirectionType>
    {
        IProperty<DirectionType> m_Property;
        public override void RegisterEditableMembers(IContext context)
        {
            m_Property = context.RegisterProperty<DirectionType>("");
        }
        public override void OnDrawSpacedGizmo(DirectionType direction)
        {
            Quaternion normalQuat = Quaternion.FromToRotation(Vector3.forward, direction.direction);

            Handles.ArrowHandleCap(0, Vector3.zero, normalQuat, HandleUtility.GetHandleSize(Vector3.zero) * 1, Event.current.type);

            if (m_Property.isEditable && RotationGizmo(Vector3.zero, ref normalQuat, true))
            {
                direction.direction = (normalQuat * Vector3.forward).normalized;
                m_Property.SetValue(direction);
            }
        }
    }
    class VFXVectorGizmo : VFXSpaceableGizmo<Vector>
    {
        IProperty<Vector> m_Property;
        public override void RegisterEditableMembers(IContext context)
        {
            m_Property = context.RegisterProperty<Vector>("");
        }
        public override void OnDrawSpacedGizmo(Vector vector)
        {
            Quaternion normalQuat = Quaternion.FromToRotation(Vector3.forward, vector.vector);

            float length = vector.vector.magnitude;


            if (m_Property.isEditable && RotationGizmo(Vector3.zero, ref normalQuat, true))
            {
                vector.vector = (normalQuat * Vector3.forward).normalized * length;
                m_Property.SetValue(vector);
            }

            if(m_Property.isEditable)
            {
                Handles.DrawLine(Vector3.zero,vector.vector);
                EditorGUI.BeginChangeCheck();
                Vector3 result = Handles.Slider(vector.vector, vector.vector, handleSize* 2 * HandleUtility.GetHandleSize(vector.vector), Handles.ConeHandleCap, 0);
                if (EditorGUI.EndChangeCheck())
                {
                    vector.vector = vector.vector.normalized * result.magnitude;
                    m_Property.SetValue(vector);
                }
            }
            else
            {
                Handles.ArrowHandleCap(0, Vector3.zero, normalQuat, length, Event.current.type);
            }
        }
    }
}
