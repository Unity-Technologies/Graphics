using System;
using System.Linq;
using UnityEngine;

// TODO: Remove after migration
namespace UnityEditor.VFX
{
    class VFXSourceAttributeParameter : VFXAttributeParameter
    {
        VFXSourceAttributeParameter()
        {
            location = VFXAttributeLocation.Source;
        }

        public override void Sanitize()
        {
            // Create new operator
            var attrib = ScriptableObject.CreateInstance<VFXAttributeParameter>();
            attrib.SetSettingValue("location", VFXAttributeLocation.Source);
            attrib.SetSettingValue("attribute", attribute);

            // Transfer links
            var links = GetOutputSlot(0).LinkedSlots.ToArray();
            GetOutputSlot(0).UnlinkAll();
            foreach (var s in links)
                attrib.GetOutputSlot(0).Link(s);

            // Replace operator
            var parent = GetParent();
            Detach();
            attrib.Attach(parent);
        }
    }
}
