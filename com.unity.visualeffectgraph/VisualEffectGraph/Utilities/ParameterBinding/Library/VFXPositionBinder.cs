using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [VFXBinder("Transform/Position")]
    public class VFXPositionBinder : VFXBinderBase
    {
        [VFXParameterBinding("UnityEditor.VFX.Position", "UnityEngine.Vector3")]
        public string Parameter = "Position";
        public Transform Target;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3(GetParameter(Parameter));
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetVector3(GetParameter(Parameter), Target.transform.position);
        }

        public override string ToString()
        {
            return string.Format("Position : '{0}' -> {1}", Parameter, Target == null ? "(null)" : Target.name);
        }
    }
}
