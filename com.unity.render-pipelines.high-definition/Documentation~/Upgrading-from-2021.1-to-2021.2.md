# Upgrading HDRP from 2021.1 to 2021.2

## Shader code

From 2021.2, if material ambient occlusion needs to be applied to probe volume GI and the material is deferred, the material needs to define `HAS_PAYLOAD_WITH_UNINIT_GI` constant and a function `float GetUninitializedGIPayload(SurfaceData surfaceData)` that returns the AO factor that is desired to be applied. No action is needed for forward only materials or if no material AO needs to be applied to probe volume GI.

From 2021.2, there is a new shader pass "ForwardEmissiveForDeferred" with new associated define SHADERPASS_FORWARD_EMISSIVE_FOR_DEFERRED for Material which have a GBuffer pass. The new pass can be see in Lit.shader. It is used to render the emissive contributoin of a Material in a separate forward pass in case of Deferred Lit shader mode.
