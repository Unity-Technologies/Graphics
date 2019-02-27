# HD Shadow Settings

The HD Shadow Settings Volume component override control the maximum distance at which HDRP renders shadow cascades and shadows from [punctual lights](Glossary.html#PunctualLight). It uses cascade splits to control the quality of shadows cast by Directional Lights over distance from the Camera.

The **HD Shadow Settings** override comes as default when you create a __Scene Settings__ GameObject (Menu: __GameObject > Rendering > Scene Settings__). You can also manually add a **HD Shadow Settings** override to any [Volume](Volumes.html). Click on the Volume's **Add Override** button and select **HD Shadow Settings** from the list of overrides to add a **HD Shadow Settings** override to the Volume.

## Properties

![](Images/SceneSettingsHDShadowSettings1.png)

| Property          | Description                                                  |
| ----------------- | ------------------------------------------------------------ |
| **Max Distance**  | The maximum distance at which HDRP renders shadows. HDRP uses this for punctual Lights and as the last boundary for the final cascade. |
| **Cascade Count** | The number of cascades for Direction Lights that can cast shadows. Cascades work as a shadow levels of detail (LOD). The quality loss in the cascades further from the Camera occurs because each cascade has its own shadow map and the cascades get progressively larger. This means that HDRP spread the same resolution shadow map over a larger area. |
| **Split 1**       | The limit between the first and second cascade split (expressed as a percentage of Max Distance). |
| **Split 2**       | The limit between the second and third cascade split (expressed as a percentage of Max Distance). |
| **Split 3**       | The limit between the third and final split (expressed as a percentage of Max Distance). |