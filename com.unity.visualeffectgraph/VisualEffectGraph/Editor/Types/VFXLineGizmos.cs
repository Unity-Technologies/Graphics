using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXLineGizmo : VFXSpaceableGizmo<Line>
    {
        IProperty<Vector3> m_StartProperty;
        IProperty<Vector3> m_EndProperty;
        
        public override void RegisterEditableMembers(IContext context)
        {
            m_StartProperty = context.RegisterProperty<Vector3>("start");
            m_EndProperty = context.RegisterProperty<Vector3>("end");
        }

        public override void OnDrawSpacedGizmo(Line line)
        {
            Handles.DrawLine(line.start,line.end);

            PositionGizmo(line.start,m_StartProperty,true);
            PositionGizmo(line.end,m_EndProperty,true);
        }
    }
}
