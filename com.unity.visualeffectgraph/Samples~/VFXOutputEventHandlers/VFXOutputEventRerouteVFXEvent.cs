using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.VFX.Utility
{
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    public class VFXOutputEventRerouteVFXEvent : VFXOutputEventHandler
    {
        public override bool canExecuteInEditor => true;

        [SerializeField]
        [Tooltip("The Visual Effect to Reroute the event to")]
        protected VisualEffect targetVisualEffect;
        [SerializeField]
        [Tooltip("The event sent to the target Visual Effect")]
        protected ExposedProperty eventToReroute;

        VisualEffect m_VFXComponent;
        VisualEffectAsset referenceAsset;
        VFXEventAttribute cachedEventAttribute;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_VFXComponent = GetComponent<VisualEffect>();
            UpdateEventAttribute();
        }

        private void UpdateEventAttribute()
        {
            referenceAsset = m_VFXComponent.visualEffectAsset;
            cachedEventAttribute = targetVisualEffect.CreateVFXEventAttribute();
            referenceAsset = targetVisualEffect.visualEffectAsset;
        }

        public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
        {
            if (targetVisualEffect == null)
                return;

            if (referenceAsset != targetVisualEffect.visualEffectAsset)
                UpdateEventAttribute();

            cachedEventAttribute.CopyValuesFrom(eventAttribute);

            if (targetVisualEffect != null)
            {
                targetVisualEffect.SendEvent(eventToReroute, cachedEventAttribute);
            }
        }
    }

}
