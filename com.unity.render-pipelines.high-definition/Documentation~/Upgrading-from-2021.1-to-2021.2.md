# Upgrading HDRP from 2021.1 to 2021.2

## Shader code

From 2021.2, if material ambient occlusion needs to be applied to probe volume GI and the material is deferred, the material needs to define `HAS_PAYLOAD_WITH_UNINIT_GI` constant and a function `float GetUninitializedGIPayload(SurfaceData surfaceData)` that returns the AO factor that is desired to be applied. No action is needed for forward only materials or if no material AO needs to be applied to probe volume GI.

HDRP 2021.2 includes the "ForwardEmissiveForDeferred" shader pass and the associated SHADERPASS_FORWARD_EMISSIVE_FOR_DEFERRED define for Materials that have a GBuffer pass. You can see the new pass in Lit.shader. When you use the Deferred Lit shader mode, Unity uses "ForwardEmissiveForDeferred" to render the emissive contribution of a Material in a separate forward pass. Otherwise, Unity ignores "ForwardEmissiveForDeferred".

## Density Volumes

As **Density Volume** has been renamed to **Local Volumetric Fog**, you won't need to worry about the game objects using the old component, as the serialization is taking into account the GUID and not the component name. Whereas, if you are referencing **Density Volume** class through your scripts, you will notice a warning (**DensityVolume has been deprecated (UnityUpgradable) -> Local Volumetric Fog**) so change your code and target the new component. This might stop compiling your project in future versions.
