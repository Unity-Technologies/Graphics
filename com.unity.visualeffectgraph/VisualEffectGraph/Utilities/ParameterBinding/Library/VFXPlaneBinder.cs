using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

[VFXBinder("Utility/Plane")]
public class VFXPlaneBinder : VFXBinderBase
{
    [VFXParameterBinding("UnityEditor.VFX.Plane")]
    public string Parameter = "Plane";
    public Transform Target;

    int Position;
    int Normal;

    void OnValidate()
    {
        Position = GetParameter(Parameter + "_position");
        Normal = GetParameter(Parameter + "_normal");
    }

    public override bool IsValid(VisualEffect component)
    {
        return Target != null && component.HasVector3(Position) && component.HasVector3(Normal);
    }

    public override void UpdateBinding(VisualEffect component)
    {
        component.SetVector3(Position, Target.transform.position);
        component.SetVector3(Normal, Target.transform.up);
    }

    public override string ToString()
    {
        return string.Format("Plane : '{0}' -> {1}", Parameter, Target == null ? "(null)" : Target.name);
    }
}
