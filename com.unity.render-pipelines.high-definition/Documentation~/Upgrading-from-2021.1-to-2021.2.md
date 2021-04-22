# Upgrading HDRP from 2021.1 to 2021.2

## Shader code

From 2021.2, if material ambient occlusion needs to be applied to probe volume GI and the material is deferred, the material needs to define `HAS_PAYLOAD_WITH_UNINIT_GI` constant and a function `float GetUninitializedGIPayload(SurfaceData surfaceData)` that returns the AO factor that is desired to be applied. No action is needed for forward only materials or if no material AO needs to be applied to probe volume GI.

HDRP 2021.2 includes the "ForwardEmissiveForDeferred" shader pass and the associated SHADERPASS_FORWARD_EMISSIVE_FOR_DEFERRED define for Materials that have a GBuffer pass. You can see the new pass in Lit.shader. When you use the Deferred Lit shader mode, Unity uses "ForwardEmissiveForDeferred" to render the emissive contribution of a Material in a separate forward pass. Otherwise, Unity ignores "ForwardEmissiveForDeferred".

## Density Volumes

Density Volumes are now known as **Local Volumetric Fog**. If a Scene uses Density Volumes, HDRP automatically migrates the GameObjects to use the new component name, with all the same properties set for the Density Volume. However, if you reference a **Density Volume** through a C# script, a warning appears (**DensityVolume has been deprecated (UnityUpgradable) -> Local Volumetric Fog**) in the Console window. To resolve this, change your code to target the new component. This warning may stop your Project from compiling in future versions of HDRP.

## ClearFlag

ClearFlag.Depth does not implicitely clear stencil anymore. ClearFlag.Stencil added.

## HDRP Global Settings

From 2021.2, the HDRP Asset assigned in the Graphics Settings no longer acts as the default Asset for HDRP. A new HDRP Global Settings Asset now exists to save settings unrelated to which HDRP Asset is active.

## Materials

### Transparent Surface Type

From 2021.2, the range for **Sorting Priority** values has decreased from between -100 and 100 to between -50 and 50. If you used transparent materials (**Surface Type** set to **Transparent**) with a sorting priority lower than -50 or greater than 50, you must remap them to within the new range. HDRP does not clamp the Sorting Priority to the new range until you edit the Sorting Priority property.
