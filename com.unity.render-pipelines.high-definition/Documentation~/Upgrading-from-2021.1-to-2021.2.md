# Upgrading HDRP from 2021.1 to 2021.2

## Shader code

From 2021.2, if material ambient occlusion needs to be applied to probe volume GI and the material is deferred, the material needs to define `HAS_PAYLOAD_WITH_UNINIT_GI` constant and a function `float GetUninitializedGIPayload(SurfaceData surfaceData)` that returns the AO factor that is desired to be applied. No action is needed for forward only materials or if no material AO needs to be applied to probe volume GI.

## HDRP Global Settings

From 2021.2, the GraphicsSettings's assigned HDRP Asset no longer acts as the defaultAsset for HDRP. A new Asset called HDRP Global Settings has been added to save settings unrelated to which HDRP Asset is active.
