using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class VFXPositionBinding : VFXBindingBase
{
    [VFXBinding("UnityEditor.VFX.Position", "UnityEngine.Vector3")]
    public string Parameter = "Position";
    public Transform Target;

    public override void UpdateBinding(VisualEffect component)
    {
        if (Target != null && component.HasVector3(Parameter))
            component.SetVector3(Parameter, Target.transform.position);
    }

    public override string ToString()
    {
        return string.Format("Position : '{0}' -> {1}", Parameter, Target == null ? "(null)" : Target.name);
    }
}
