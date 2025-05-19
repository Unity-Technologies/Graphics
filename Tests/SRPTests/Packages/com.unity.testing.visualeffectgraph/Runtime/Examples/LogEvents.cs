using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteInEditMode]
class LogEventNames : MonoBehaviour
{
	// Called when the script or GameObject is enabled
    void OnEnable()
    {
        VisualEffect vfx = GetComponent<VisualEffect>();
        if (vfx != null && vfx.visualEffectAsset != null)
        {
            VisualEffectAsset vfxAsset = vfx.visualEffectAsset;
            var eventNames = new List<string>();

            // Retrieve all events from the VisualEffectAsset and store them in the list
            vfxAsset.GetEvents(eventNames);
            if (eventNames.Count == 0)
            {
                Debug.Log($"There are no events listed for asset: {vfxAsset}");
            }

            foreach (var eventName in eventNames)
            {
                Debug.Log($"Event: {eventName}");
            }
        }
        else
        {
            Debug.Log("Unable to retrieve VisualEffect component or VisualEffectAsset is null.");
        }
    }
}
