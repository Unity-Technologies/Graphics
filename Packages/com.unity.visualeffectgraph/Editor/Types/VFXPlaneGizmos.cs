using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(Plane))]
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
            if (!VFXTypeUtility.IsFinite(plane))
                return;

            Vector3 normal = plane.normal.normalized;
            if (normal == Vector3.zero)
            {
                normal = Vector3.up;
            }

            var normalQuat = Quaternion.FromToRotation(Vector3.forward, normal);

            var planeSize = 5.0f;
            var arrowSize = 1.0f;
            var scale = 1.0f; //Could be relative to camera size using GetHandleSize
            planeSize *= scale;
            arrowSize *= scale;
            Vector3[] points = new Vector3[]
            {
                new Vector3(planeSize, planeSize, 0),
                new Vector3(planeSize, -planeSize, 0),
                new Vector3(-planeSize, -planeSize, 0),
                new Vector3(-planeSize, planeSize, 0),
                new Vector3(planeSize, planeSize, 0),
            };

            var worldPositon = Handles.matrix.MultiplyPoint(plane.position);
            var worldRotation = Handles.matrix.rotation * normalQuat;
            using (new Handles.DrawingScope(Matrix4x4.TRS(worldPositon, worldRotation, Vector3.one)))
            {
                Handles.DrawPolyLine(points);
                Handles.ArrowHandleCap(0, Vector3.zero, Quaternion.identity, arrowSize, Event.current.type);
            }

            PositionOnlyGizmo(plane.position, m_PositionProperty);
            if (m_NormalProperty.isEditable && NormalGizmo(plane.position, ref normal, false))
            {
                normal.Normalize();
                m_NormalProperty.SetValue(normal);
            }
        }

        public override Bounds OnGetSpacedGizmoBounds(Plane value)
        {
            return new Bounds(value.position, Vector3.one);
        }
    }
}
