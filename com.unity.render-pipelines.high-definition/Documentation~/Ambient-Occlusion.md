# Ambient occlusion

HDRP uses ambient occlusion to approximate the intensity and position of ambient light on a GameObject’s surface, based on the light in the Scene and the environment around the GameObject. In HDRP, ambient occlusion only affects indirect diffuse lighting (lighting from Lightsmaps, Light Probes, and Light Probe Proxy Volumes).

Note: Ambient occlusion in a Lit Shader using deferred rendering affects emission due to a technical constraint. Lit Shaders that use forward rendering do not have this constraint and do not affect emission.

HDRP calculates the ambient occlusion effect using a map. You create and apply this map using the green channel of the mask map. HDRP uses the ambient occlusion map for specular occlusion, which it applies on indirect specular lighting. HDRP does not expose specular occlusion options in the Lit Shader.Instead, it automatically calculates specular occlusion from the Camera’s view vector and the ambient occlusion.

| Property                        | Description                                                  |
| ------------------------------- | ------------------------------------------------------------ |
| **Mask Map - Green channel **   | Assign the ambient occlusion map in the green channel of the **Mask Map**. HDRP uses the green channel of this map to calculate ambient occlusion. |
| **Ambient Occlusion Remapping** | Remaps the ambient occlusion map in the green channel of the **Mask Map** between the minimum and maximum values you define on the slider. These values are between 0 and 1. Assign a texture to the **Mask Map** exposes this value. |