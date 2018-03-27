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
            attrib.position = position;

            // Transfer links
            VFXSlot.TransferLinks(attrib.GetOutputSlot(0), GetOutputSlot(0), true);
            
            // Replace operator
            var parent = GetParent();
            Detach();
            attrib.Attach(parent);
        }
    }
}
