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
        VisualEffectAsset cachedEventAttributeAsset;

        public override void OnVFXEventAttribute(VFXEventAttribute eventAttribute, VisualEffect visualEffect)
        {
            if(component == null)
            {
                component = GetComponent<VisualEffect>();

                if(cachedEventAttribute == null 
                    || component.visualEffectAsset != cachedEventAttributeAsset)
                {
                    cachedEventAttribute = component.CreateVFXEventAttribute();
                    cachedEventAttributeAsset = component.visualEffectAsset;
                }
            }

            if(cachedEventAttribute != null)
            {
                cachedEventAttribute.CopyValuesFrom(eventAttribute);
                component.SendEvent(eventToSend, cachedEventAttribute);
            }
        }
    }
}
