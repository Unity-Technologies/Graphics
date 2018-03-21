using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

public class VFXSphereBinding : VFXBindingBase
{
    [VFXBinding("UnityEditor.VFX.Sphere")]
    public string Parameter = "Sphere";
    public SphereCollider Target;


    public override void UpdateBinding(VisualEffect component)
    {
        if (Target != null && component.HasVector3(Parameter + "_center") && component.HasFloat(Parameter + "_radius"))
        {
            component.SetVector3(Parameter + "_center", Target.transform.position + Target.center);
            component.SetFloat(Parameter + "_radius", Target.radius * GetSphereColliderScale(Target.transform.localScale));
        }
    }

    public float GetSphereColliderScale(Vector3 scale)
    {
        return Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
    }

    public override string ToString()
    {
        return string.Format("Sphere : '{0}' -> {1}", Parameter, Target == null ? "(null)" : Target.name);
    }
}
