# Depth Of Field

The Depth Of Field component applies a depth of field effect, which simulates the focus properties of a camera lens. In real life, a camera can only focus sharply on an object at a specific distance; objects nearer or farther from the camera are out of focus. The blurring gives a visual cue about an object’s distance, and introduces Bokeh, which refers to visual artifacts that appear around bright areas of the image as they fall out of focus.

## Using Depth Of Field

**Depth Of Field** uses the [Volume](Volumes.html) framework, so to enable and modify **Depth Of Field** properties, you must add a **Depth Of Field** override to a [Volume](Volumes.html) in your Scene. To add **Depth Of Field** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Post-processing** and click on **Depth Of Field**. HDRP now applies **Depth Of Field** to any Camera this Volume affects.

Depth Of Field includes [more options](More-Options.html) that you must manually expose.


## Properties

![](Images/Post-ProcessingDepthOfField1.png)

| **Property**       | **Description**                                              |
| ------------------ | ------------------------------------------------------------ |
| **Focus Mode**     | Use the drop-down to select the mode that HDRP uses to set the focus for the depth of field effect.<br />&#8226; **Off**: Select this option to disable depth of field.<br />&#8226; **Use Physical Camera**: Select this option to use the physical [Camera](HDRP-Camera.html) to set focusing properties for the depth of field effect. For information on what Camera properties affect depth of field, see [Physical Camera settings](#PhysicalCameraSettings).<br />&#8226; **Manual**: Select this option to use custom values to set the focus of the depth of field effect. |
| **Focus Distance** | Set the distance to the focus point from the Camera.<br />This property only appears when you select **Use Physical Camera** from the **Focus Mode** drop-down. |

### Near Blur

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Start**        | Set the distance from the Camera at which the near field blur begins to decrease in intensity.<br />This property only appears when you select **Manual** from the **Focus Mode** drop-down. |
| **End**          | Set the distance from the Camera at which the near field does not blur anymore.<br />This property only appears when you select **Manual** from the **Focus Mode** drop-down. |
| **Sample Count** | Set the number of samples to use for the near field. Lower values result in better performance at the cost of visual accuracy. |
| **Max Radius**   | Set the maximum radius the near blur can reach.              |

### Far Blur

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Start**        | Set the distance from the Camera at which the far field starts blurring.<br />This property only appears when you select **Manual** from the **Focus Mode** drop-down. |
| **End**          | Set the distance from the Camera at which the far field blur reaches its maximum blur radius.<br />This property only appears when you select **Manual** from the **Focus Mode** drop-down. |
| **Sample Count** | Set the number of samples to use for the far field. Lower values result in better performance at the cost of visual accuracy. |
| **Max Radius**   | Set the maximum radius the far blur can reach.               |

### Advanced Tweaks

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Resolution**             | Use the drop-down to set the resolution at which HDRP processes the depth of field effect. If you target consoles that use a very high resolution (for example, 4k), select **Quarter,** because it is less resource intensive.<br />&#8226; **Quarter**: Uses quarter the screen resolution.<br />&#8226; **Half**: Uses half the screen resolution.<br />This property only appears when you enable [more options](More-Options.html). |
| **High Quality Filtering** | Enable the checkbox to make HDRP use bicubic filtering instead of bilinear filtering. This increases the resource intensity of the Depth Of Field effect, but results in smoother visuals.<br />This property only appears when you enable [more options](More-Options.html). |

<a name="PhysicalCameraSettings"></a>

## Physical Camera settings

Here is a list of the physical [Camera](HDRP-Camera.html) properties that affect the Depth of Field effect when you select **Use Physical Camera** from the **Focus Mode** drop-down.

| **Property**     | **Effect**                                                   |
| ---------------- | ------------------------------------------------------------ |
| **Aperture**     | The larger this value, the larger the [bokeh](Glossary.html#Bokeh) and overall blur effect. |
| **Blades Count** | This determines the shape of the bokeh. For more information on the effect this property has, see the example below. |
| **Curvature**    | Determines how much of the blades are visible. Use this to change the roundness of bokeh in the blur. For more information on the effect this property has, see the example below. |

This example shows how the **Blade Count** and **Curvature** properties affect the shape of the bokeh:

* On the left side, there is a five blade iris that is slightly open; producing a pentagonal bokeh.
* On the right side, there is a five blade iris that is wide open; producing a circular bokeh.

![](Images/Post-ProcessingDepthofField2.png)