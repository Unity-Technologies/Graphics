using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class VFXPlaneBinding : VFXBindingBase
{
    [VFXBinding("UnityEditor.VFX.Plane")]
    public string Parameter = "Plane";
    public Transform Target;


    public override void UpdateBinding(VisualEffect component)
    {
        if (Target != null && component.HasVector3(Parameter + "_position") && component.HasVector3(Parameter + "_normal"))
        {
            component.SetVector3(Parameter + "_position", Target.transform.position);
            component.SetVector3(Parameter + "_normal", Target.transform.up);
        }
    }

    public override string ToString()
    {
        return string.Format("Plane : '{0}' -> {1}", Parameter, Target == null ? "(null)" : Target.name);
    }
}
