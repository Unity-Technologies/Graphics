using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [VFXBinder("Transform/Transform")]
    public class VFXTransformnBinder : VFXBinderBase
    {
        [VFXParameterBinding("UnityEditor.VFX.Transform")]
        public string Parameter = "Transform";
        public Transform Target;

        private int Position;
        private int Angles;
        private int Scale;

        void OnValidate()
        {
            Position = GetParameter(Parameter + "_position");
            Angles = GetParameter(Parameter + "_angles");
            Scale = GetParameter(Parameter + "_scale");
        }

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3(Position) && component.HasVector3(Angles) && component.HasVector3(Scale);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetVector3(Position, Target.transform.position);
            component.SetVector3(Angles, Target.transform.eulerAngles);
            component.SetVector3(Scale, Target.transform.localScale);
        }

        public override string ToString()
        {
            return string.Format("Transform : '{0}' -> {1}", Parameter, Target == null ? "(null)" : Target.name);
        }
    }
}
