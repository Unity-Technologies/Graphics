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
        public override void OnDrawSpacedGizmo(Position position, VisualEffect component)
        {
            if (m_Property.isEditable && PositionGizmo(component,  ref position.position))
            {
                m_Property.SetValue(position);
            }
        }
    }
}
