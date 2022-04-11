# Film Grain

![Film Grain off](Images/post-proc/film-grain-off.png)
<br/>_Scene with Film Grain effect turned off._

![Film Grain on](Images/post-proc/film-grain.png)
<br/>_Scene with Film Grain effect turned on._

The Film Grain effect simulates the random optical texture of photographic film, usually caused by small particles being present on the physical film.

## Using Film Grain

**Film Grain** uses the [Volume](Volumes.md) framework, so to enable and modify **Film Grain** properties, you must add a **Film Grain** override to a [Volume](Volumes.md) in your Scene. To add **Film Grain** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Post-processing** and click on **Film Grain**. URP now applies **Film Grain** to any Camera this Volume affects.

## Properties

![](Images/Inspectors/FilmGrain.png)

| **Property**  | **Description**                                              |
| ------------- | ------------------------------------------------------------ |
| **Type**      | Use the drop-down to select the type of grain to use. You can select from a list of presets that URP includes, or select **Custom** to provide your own grain Texture. |
| **Texture**   | Assign a Texture that this effect uses as a custom grain Texture.This property is only available when **Type** is set to **Custom**. |
| **Intensity** | Use the slider to set the strength of the Film Grain effect. |
| **Response**  | Use the slider to set the noisiness response curve. The higher you set this value, the less noise there is in brighter areas. ![Film Grain Response](Images/post-proc/film-grain-response.png)<br/>_Response value 1 (left), and 0 (right)._ |
