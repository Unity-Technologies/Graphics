using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.VFX.Utility
{
    [RequireComponent(typeof(Collider))]
    public class VFXTriggerEventBinder : VFXEventBinderBase
    {
        public enum Activation
        {
            OnEnter,
            OnExit,
            OnStay
        }

        public List<Collider> targets = new List<Collider>();
        
        public Activation activation = Activation.OnEnter;

        private ExposedParameter positionParameter = "position";

        protected override void SetEventAttribute(VisualEffect target, VFXEventAttribute attribute, Object[] parameters)
        {
            Collider collider = (Collider)parameters[0];
            attribute.SetVector3(positionParameter, collider.transform.position);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (activation != Activation.OnEnter) return;
            if (!targets.Contains(other)) return;

            SendEventToVisualEffects(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (activation != Activation.OnExit) return;
            if (!targets.Contains(other)) return;

            SendEventToVisualEffects(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (activation != Activation.OnStay) return;
            if (!targets.Contains(other)) return;

            SendEventToVisualEffects(other);
        }
    }
}

