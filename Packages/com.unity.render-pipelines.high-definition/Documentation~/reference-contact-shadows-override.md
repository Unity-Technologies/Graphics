## Contact shadows override reference

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| Property                  | Description                                                  |
| :------------------------ | :----------------------------------------------------------- |
| __State__                 | When set to **Enabled**, HDRP processes Contact Shadows for this [Volume](understand-volumes.md). |
| __Length__                | Use the slider to set the length of the rays, in meters, that HDRP uses for tracing. It also functions as the maximum distance at which the rays can captures details. |
| __Distance Scale Factor__ | HDRP scales Contact Shadows up with distance. Use the slider to set the value that HDRP uses to dampen the scale to avoid biasing artifacts with distance. |
| __Min Distance__          | The distance from the Camera, in meters, at which HDRP begins to fade in Contact Shadows. |
| __Max Distance__          | The distance from the Camera, in meters, at which HDRP begins to fade Contact Shadows out to zero. |
| __Fade In Distance__      | The distance, in meters, over which HDRP fades Contact Shadows in when past the **Min Distance**. |
| __Fade Out Distance__     | The distance, in meters, over which HDRP fades Contact Shadows out when at the __Max Distance__. |
| __Opacity__               | Use the slider to set the opacity of the Contact Shadows. Lower values result in softer, less prominent shadows. |
| **Bias**                  | Controls the bias applied to the screen space ray cast to get contact shadows. Higher values can reduce self shadowing, however too high values might lead to peter-panning that can be especially undesirable with contact shadows. |
| **Thickness**             | Controls the thickness of the objects found along the ray, essentially thickening the contact shadows. It can be used to fill holes in the shadows, however might also lead to overly wide shadows. |
| **Quality**               | Specifies the quality level to use for this effect. Each quality level applies different preset values. Unity also stops you from editing the properties that the preset overrides. If you want to set your own values for every property, select **Custom**. |
| __Sample Count__          | Use the slider to set the number of samples HDRP uses for ray casting. Increasing this increases quality at the cost of performance. |