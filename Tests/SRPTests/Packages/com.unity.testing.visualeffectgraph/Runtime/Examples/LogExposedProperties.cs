using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

[ExecuteInEditMode]
class LogExposedProperties : MonoBehaviour
{
	// Called when the script or GameObject is enabled
    void OnEnable()
    {
        VisualEffect vfx = GetComponent<VisualEffect>();
        if (vfx != null && vfx.visualEffectAsset != null)
        {
            VisualEffectAsset vfxAsset = vfx.visualEffectAsset;
            var exposedProperties = new List<VFXExposedProperty>();

            // Retrieve all exposed properties from the VisualEffectAsset and store them in the list
            vfxAsset.GetExposedProperties(exposedProperties);

            if (exposedProperties.Count == 0)
            {
                Debug.Log($"There are no exposed properties for asset: {vfxAsset}");
            }

            foreach (var exposedProperty in exposedProperties)
            {
                // Retrieve additional details about the exposed property
                VFXSpace space = vfxAsset.GetExposedSpace(exposedProperty.name);
                TextureDimension texDim = vfxAsset.GetTextureDimension(exposedProperty.name);

                string log = $"{exposedProperty.name}, {exposedProperty.type}";
                if (space != VFXSpace.None)
                {
                    log += $", {space}";
                }
                if (texDim != TextureDimension.Unknown)
                {
                    log += $", {texDim}";
                }
                Debug.Log(log);
            }
        }
        else
        {
            Debug.Log("Unable to retrieve VisualEffect component or VisualEffectAsset is null.");
        }
    }
}
