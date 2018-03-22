using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [VFXBinder("Collider/Sphere")]
    public class VFXSphereBinder : VFXBinderBase
    {
        [VFXParameterBinding("UnityEditor.VFX.Sphere")]
        public string Parameter = "Sphere";
        public SphereCollider Target;

        private int Center;
        private int Radius;

        void OnValidate()
        {
            Center = GetParameter(Parameter + "_center");
            Radius = GetParameter(Parameter + "_radius");
        }

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3(Center) && component.HasFloat(Radius);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetVector3(Center, Target.transform.position + Target.center);
            component.SetFloat(Radius, Target.radius * GetSphereColliderScale(Target.transform.localScale));
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
}
