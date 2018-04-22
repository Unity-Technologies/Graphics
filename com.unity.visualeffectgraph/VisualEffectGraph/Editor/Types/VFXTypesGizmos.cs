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
        IProperty<Vector3> m_Property;
        public override void RegisterEditableMembers(IContext context)
        {
            m_Property = context.RegisterProperty<Vector3>("position");
        }
        public override void OnDrawSpacedGizmo(Position position)
        {
            PositionGizmo(position.position,m_Property, true);
        }
    }
}
