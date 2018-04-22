using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXPlaneGizmo : VFXSpaceableGizmo<Plane>
    {
        IProperty<Vector3> m_PositionProperty;
        IProperty<Vector3> m_NormalProperty;
        public override void RegisterEditableMembers(IContext context)
        {
            m_PositionProperty = context.RegisterProperty<Vector3>("position");
            m_NormalProperty = context.RegisterProperty<Vector3>("normal");
        }

        public override void OnDrawSpacedGizmo(Plane plane)
        {
            Vector3 normal = plane.normal.normalized;
            if( normal == Vector3.zero)
            {
                normal = Vector3.up;
            }
            Quaternion normalQuat = Quaternion.FromToRotation(Vector3.forward, plane.normal);
            Handles.RectangleHandleCap(0, plane.position, normalQuat, 10, Event.current.type);

            Handles.ArrowHandleCap(0, plane.position, normalQuat, 5, Event.current.type);

            if (m_PositionProperty.isEditable && PositionGizmo(ref plane.position, false))
            {
                m_PositionProperty.SetValue(plane.position);
            }

            if(m_NormalProperty.isEditable && RotationGizmo(plane.position, ref normalQuat, false))
            {
                Vector3 newNormal = normalQuat * Vector3.forward;
                m_NormalProperty.SetValue(newNormal);
            }   
        }
    }
}
