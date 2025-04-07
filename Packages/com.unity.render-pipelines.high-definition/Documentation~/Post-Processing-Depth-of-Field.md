# Depth Of Field

The Depth Of Field component applies a depth of field effect, which simulates the focus properties of a camera lens. In real life, a camera can only focus sharply on an object at a specific distance; objects nearer or farther from the camera are out of focus. The blurring gives a visual cue about an object’s distance, and introduces Bokeh, which refers to visual artifacts that appear around bright areas of the image as they fall out of focus.

## Using Depth Of Field

**Depth Of Field** uses the [Volume](understand-volumes.md) framework, so to enable and modify **Depth Of Field** properties, you must add a **Depth Of Field** override to a [Volume](understand-volumes.md) in your Scene. To add **Depth Of Field** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** > **Post-processing** and select **Depth Of Field**. HDRP now applies **Depth Of Field** to any Camera this Volume affects.

Depth Of Field includes [advanced properties](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest?subfolder=/manual/advanced-properties.html). that you must manually expose.

[!include[](snippets/volume-override-api.md)]


## Properties

| **Property**       | **Description**                                              |
|-|-|
| **Focus Mode**     | Use the drop-down to select the mode that HDRP uses to set the focus for the depth of field effect. The options are: <ul><li>**Off**: Select this option to disable depth of field.</li><li>**Physical Camera**: Select this option to use the physical [Camera](hdrp-camera-component-reference.md) to set focusing properties for the depth of field effect. For more information about what Camera properties affect depth of field, refer to [Physical Camera settings](#PhysicalCameraSettings).</li><li>**Manual Ranges**: Select this option to use custom values to set the near and far range of the depth of field effect.</li></ul> |
| **Focus Distance Mode** | Use the drop-down to select where the focus distance is specified. The options are: <ul><li>**Volume**: Reads the focus distance from the Volume.</li><li>**Camera**: Reads the focus distance from the physical camera.</li></ul> This property only appears when you select **Physical Camera** from the **Focus Mode** drop-down. |
| **Focus Distance** | Set the distance to the focus plane from the Camera.<br />This property only appears when you select **Volume** from the **Distance Mode** drop-down. |

### Near Blur

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Start**        | Set the distance from the Camera at which the near field blur begins to decrease in intensity.<br />This property only appears when you select **Manual** from the **Focus Mode** drop-down. |
| **End**          | Set the distance from the Camera at which the near field doesn't blur anymore.<br />This property only appears when you select **Manual** from the **Focus Mode** drop-down. |
| **Sample Count** | Set the number of samples to use for the near field. Lower values result in better performance at the cost of visual accuracy. |
| **Max Radius**   | Set the maximum radius the near blur can reach.              |

### Far Blur

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Start**        | Set the distance from the Camera at which the far field starts blurring.<br />This property only appears when you select **Manual** from the **Focus Mode** drop-down. |
| **End**          | Set the distance from the Camera at which the far field blur reaches its maximum blur radius.<br />This property only appears when you select **Manual** from the **Focus Mode** drop-down. |
| **Sample Count** | Set the number of samples to use for the far field. Lower values result in better performance at the cost of visual accuracy. |
| **Max Radius**   | Set the maximum radius the far blur can reach.               |

<a name="PhysicalCameraSettings"></a>

## Physical Camera settings

Here is a list of the physical [Camera](hdrp-camera-component-reference.md) properties that affect the Depth of Field effect when you select **Use Physical Camera** from the **Focus Mode** drop-down.

| **Property**     | **Effect**                                                   |
| ---------------- | ------------------------------------------------------------ |
| **Aperture**     | The larger this value, the larger the [bokeh](Glossary.md#Bokeh) and overall blur effect. |
| **Blades Count** | This determines the shape of the bokeh. For more information on the effect this property has, see the example below. |
| **Curvature**    | Determines how much of the blades are visible. Use this to change the roundness of bokeh in the blur. For more information on the effect this property has, see the example below. |

![This example shows how the **Blade Count** and **Curvature** properties affect the shape of the bokeh. On the left side, there's a five blade iris that's slightly open, producing a pentagonal bokeh. On the right side, there's a five blade iris that's wide open, producing a circular bokeh.](Images/Post-ProcessingDepthofField2.png)

This example shows how the **Blade Count** and **Curvature** properties affect the shape of the bokeh:

* On the left side, there is a five blade iris that's slightly open, producing a pentagonal bokeh.
* On the right side, there is a five blade iris that's wide open, producing a circular bokeh.

## Path-traced depth of field

If you enable [path tracing](Ray-Tracing-Path-Tracing.md) and set **Focus Mode** to **Use Physical Camera**, HDRP computes depth of field directly during path tracing instead of as a post-processing effect.

Path-traced depth of field produces images without any artifacts, apart from noise when using insufficient path-tracing samples. To reduce the noise level, increase the number of samples from the [Path Tracing](Ray-Tracing-Path-Tracing.md) settings or de-noise the final frame.

HDRP computes path-traced depth of field at full resolution and ignores any quality settings from the Volume.

![Two paint pots with a depth of field effect that makes surfaces increasingly blurry towards and away from the camera.](Images/Path-traced-DoF.png)
