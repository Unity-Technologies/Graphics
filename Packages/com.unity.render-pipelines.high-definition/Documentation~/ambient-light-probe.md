## Understand the ambient light probe

HDRP uses the ambient Light Probe as the final fallback for indirect diffuse lighting. It affects:

- All Mesh Renderers if there is no indirect ambient light computed for the Scene (this applies when Unity has not computed any lightmaps or Light Probes for the Scene)
- Mesh Renderers that have their **Light Probe Mode** set to **Off**
- Volumetric fog if the Global Light Probe dimmer is set to a value above 0

The ambient Light Probe can be static (generated only once from the static lighting sky set in the HDRP **Environment (HDRP)**panel) or dynamic (updated at runtime from the sky currently in use).

***\*Note\****: If there is a ***\*Light Probe group\**** in your Scene and you have computed indirect ambient lighting, then the Ambient Light Probe only affects Mesh Renderers that have their ***\*Light Probe Mode\**** set to ***\*Off\****, and that have ***\*Volumetric fog\**** (if it’s enabled in the Scene).

### Limitations of dynamic ambient mode

The Ambient Light Probe always affects your scene one frame late after HDRP calculates it. This is because HDRP calculates Ambient Light Probes on the GPU and then uses asynchronous readback on the CPU.

As a result, the ambient lighting might not match the actual lighting and cause visual artifacts. This can happen when you use the dynamic ambient mode and use reflection probes that update on demand.