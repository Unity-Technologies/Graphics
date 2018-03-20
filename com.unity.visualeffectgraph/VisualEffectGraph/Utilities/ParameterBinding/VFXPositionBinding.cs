using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class VFXPositionBinding : VFXBindingBase
{
    public Transform Target;
    public string Parameter = "Position";

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
