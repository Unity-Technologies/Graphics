# Upgrading HDRP from 2021.1 to 2021.2

## Shader code

From 2021.2, if material ambient occlusion needs to be applied to probe volume GI and the material is deferred, the material needs to define `HAS_PAYLOAD_WITH_UNINIT_GI` constant and a function `float GetUninitializedGIPayload(SurfaceData surfaceData)` that returns the AO factor that is desired to be applied. No action is needed for forward only materials or if no material AO needs to be applied to probe volume GI.

HDRP 2021.2 includes the "ForwardEmissiveForDeferred" shader pass and the associated SHADERPASS_FORWARD_EMISSIVE_FOR_DEFERRED define for Materials that have a GBuffer pass. You can see the new pass in Lit.shader. When you use the Deferred Lit shader mode, Unity uses "ForwardEmissiveForDeferred" to render the emissive contribution of a Material in a separate forward pass. Otherwise, Unity ignores "ForwardEmissiveForDeferred".

From 2021.2, a decal function in shader code has been added. To handle normal flipping for double sided materials, calls to `GetDecalSurfaceData()` should be updated from this function:
`DecalSurfaceData GetDecalSurfaceData(PositionInputs posInput, float3 vtxNormal, inout float alpha)`
to:
`DecalSurfaceData GetDecalSurfaceData(PositionInputs posInput, FragInputs input, inout float alpha)`
