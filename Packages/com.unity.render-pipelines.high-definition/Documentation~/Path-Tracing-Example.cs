using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Class to modify or add a Volume Override from a script
public class ModifyVolumeComponent : MonoBehaviour
{
   public VolumeProfile volumeProfile;    // Set this in the inspector

   // Start is called once before the first execution of Update after the MonoBehaviour is created
   void Start()
   {
       // Check that a Volume Profile is set in the Inspector
       if (volumeProfile != null)
       {
           // Get or create the path tracing Volume Override in the Volume Profile
           GetOrCreateComponent(volumeProfile, out PathTracing pathTracingComponent);

           // Make sure that the Volume Override is active
           pathTracingComponent.active = true;

           // Enable the path tracing effect
           pathTracingComponent.enable.value = true;

           // Override the state of the enable field
           pathTracingComponent.enable.overrideState = true;

           // Set the value for the maximumDepth field
           pathTracingComponent.maximumDepth.value = 4;

           // Override the state of the maximumDepth field
           pathTracingComponent.maximumDepth.overrideState = true;
       }
   }

   // Get a Volume Override from a Volume Profile. Create it if it doesn't exist 
   private static void GetOrCreateComponent<T>(in VolumeProfile volumeProfile, out T component) where T : VolumeComponent
   {
       if (volumeProfile == null)
       {
           component = null;
           return;
       }

       // Try to get the component of type T from the Volume Profile
       if (!volumeProfile.TryGet<T>(out component))
       {
           Debug.Log($"{typeof(T).Name} component not found. Creating a new component.");

           // Add the component if it does not exist
           volumeProfile.Add<T>(true);

           // Try to get the component again
           volumeProfile.TryGet<T>(out component);
       }
   }
}
