using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.VFX.Utility
{
    [RequireComponent(typeof(VisualEffect))]
    public class VFXOutputEventPrefabAttributeHandler_VFXEvent : VFXOutputEventPrefabAttributeHandler
    {
        public ExposedProperty eventToSend = "OnStart";

        VisualEffect component;
        VFXEventAttribute cachedEventAttribute;

        public override void OnVFXEventAttribute(VFXEventAttribute eventAttribute, VisualEffect visualEffect)
        {
            if(component == null)
            {
                component = GetComponent<VisualEffect>();
                cachedEventAttribute = component.CreateVFXEventAttribute();
            }

            cachedEventAttribute.CopyValuesFrom(eventAttribute);
            component.SendEvent(eventToSend, cachedEventAttribute);
        }
    }
}
