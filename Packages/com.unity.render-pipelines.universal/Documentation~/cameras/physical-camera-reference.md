# Physical Camera reference

The physical camera properties enable the URP camera to simulate a real-world camera. These properties correspond to features of real-world cameras and work in the same way.

For more information about how to use some of these properties to create the camera effect you desire, refer to [Using Physical Cameras](xref:PhysicalCameras).

> [!NOTE]
> When the Physical Camera is in use, Unity calcualtes the Field of View with the following properties:
>
> * **Sensor Size**
> * **Focal Length**
> * **Shift**

The Physical Camera properties are split into the following sections:

* [Camera Body](#camera-body)
* [Lens](#lens)
* [Aperture Shape](#aperture-shape)

## Camera Body

| **Property** | **Description** |
| ------------ | --------------- |
| **Sensor Type** | Specify the real-world camera format you want the camera to simulate. When you choose a camera format, Unity sets the the **Sensor Size** > **X** and **Y** properties to the correct values automatically.<br/><br/>URP offers the following camera format presets:<ul><li>**8mm**:<ul><li>**X**: 4.8</li><li>**Y**: 3.5</li></ul></li><li>**Super 8mm**:<ul><li>**X**: 5.79</li><li>**Y**: 4.01</li></ul></li><li>**16mm**:<ul><li>**X**: 10.26</li><li>**Y**: 7.49</li></ul></li><li>**Super 16mm**:<ul><li>**X**: 12.522</li><li>**Y**: 7.417</li></ul></li><li>**35mm 2-perf**:<ul><li>**X**: 21.95</li><li>**Y**: 9.35</li></ul></li><li>**35mm Academy**:<ul><li>**X**: 21.946</li><li>**Y**: 16.002</li></ul></li><li>**Super-35**:<ul><li>**X**: 24.89</li><li>**Y**: 18.66</li></ul></li><li>**35mm TV Projection**:<ul><li>**X**: 20.726</li><li>**Y**: 15.545</li></ul></li><li>**35mm Full Aperture**:<ul><li>**X**: 24.892</li><li>**Y**: 18.669</li></ul></li><li>**35mm 1.85 Projection**:<ul><li>**X**: 20.955</li><li>**Y**: 11.328</li></ul></li><li>**35mm Anamorphic**:<ul><li>**X**: 21.946</li><li>**Y**: 18.593</li></ul></li><li>**65mm ALEXA**:<ul><li>**X**: 54.12</li><li>**Y**: 25.59</li></ul></li><li>**70mm**:<ul><li>**X**: 52.476</li><li>**Y**: 23.012</li></ul></li><li>**70mm IMAX**:<ul><li>**X**: 70.41</li><li>**Y**: 52.63</li></ul></li><li>**Custom**:<ul><li>Set the **X** and **Y** values manually</li></ul></li><ul/><br/>If you change the **Sensor Size** values manually, Unity automatically sets this property to **Custom**. |
| **Sensor Size** | Set the size, in millimeters, of the camera sensor. <br/><br/>Unity sets the **X** and **Y** values automatically when you choose the **Sensor Type**. |
| &#160;&#160;&#160;&#160;**X** | The horizontal size of the camera sensor. |
| &#160;&#160;&#160;&#160;**Y** | The vertical size of the camera sensor. |
| **ISO** | The light sensitivity of the camera sensor. |
| **Shutter Speed** | The amount of time the camera sensor captures light. |
| &#160;&#160;&#160;&#160;**Unit** | The unit of measurement for **Shutter Speed**.<br/><br/>Available options:<ul><li>**Second**</li><li>**1/Second**</li></ul> |
| **Gate Fit** | Options for changing the size of the **resolution gate** (size/aspect ratio of the game view) relative to the **film gate** (size/aspect ratio of the Physical Camera sensor).<br/><br/>For more information about resolution gate and film gate, refer to the documentation on [Physical Cameras](https://docs.unity3d.com/Manual/PhysicalCameras.html). |
| &#160;&#160;&#160;&#160;**Vertical** | Fits the resolution gate to the height of the film gate.<br/><br/>If the sensor aspect ratio is larger than the game view aspect ratio, Unity crops the rendered image at the sides.<br/><br/>If the sensor aspect ratio is smaller than the game view aspect ratio, Unity overscans the rendered image at the sides.<br/><br/>When you choose this setting, any change to the sensor width (**Sensor Size** > **X**) has no effect on the rendered image. |
| &#160;&#160;&#160;&#160;**Horizontal** | Fits the resolution gate to the width of the film gate.<br/><br/>If the sensor aspect ratio is larger than the game view aspect ratio, Unity overscans the rendered image on the top and bottom.<br/><br/>If the sensor aspect ratio is smaller than the game view aspect ratio, Unity crops the rendered image on the top and bottom.<br/><br/>When you choose this setting, any change to the sensor height (**Sensor Size** > **Y**) has no effect on the rendered image. |
| &#160;&#160;&#160;&#160;**Fill** | Fits the resolution gate to either the width or height of the film gate, whichever is smaller. This crops the rendered image. |
| &#160;&#160;&#160;&#160;**Overscan** | Fits the resolution gate to either the width or height of the film gate, whichever is larger. This overscans the rendered image. |
| &#160;&#160;&#160;&#160;**None** | Ignores the resolution gate and uses the film gate only. This stretches the rendered image to fit the game view aspect ratio. |

## Lens

| **Property** | **Description** |
| ------------ | --------------- |
| **Focal Length** | The distance, in millimeters, between the camera sensor and the camera lens.<br/><br/>Lower values result in a wider **Field of View**, and vice versa.<br/><br/>When you change this value, Unity automatically updates the **Field of View** property accordingly. |
| **Shift** | Shifts the lens horizontally or vertically from center. Values are multiples of the sensor size; for example, a shift of 0.5 along the X axis offsets the sensor by half its horizontal size.<br/><br/>You can use lens shifts to correct distortion that occurs when the camera is at an angle to the subject (for example, converging parallel lines).<br/><br/>Shift the lens along either axis to make the camera frustum [oblique](https://docs.unity3d.com/Manual/ObliqueFrustum.html). |
| &#160;&#160;&#160;&#160;**X** | The lens's horizontal offset from the camera sensor. |
| &#160;&#160;&#160;&#160;**Y** | The lens's vertical offset from the camera sensor |
| **Aperture** | The f-stop (f-number) of the lens. A lower value gives a wider lens aperture. |
| **Focus Distance** | The distance from the camera where objects appear sharp when you enable Depth of Field. |

> [!NOTE]
> When Physical Camera properties are in use at the same time as [Depth of Field](../post-processing-depth-of-field.md) post-processing, the Lens properties directly affect the Depth of Field effect. This requires you to adjust both the Depth of Field properties and the Lens properties to create the effect you want.

## Aperture Shape

| **Property** | **Description** |
| ------------ | --------------- |
| **Blade Count** | The number of blades in the lens aperture. A higher value gives a rounder aperture shape. |
| **Curvature** | The curvature of the lens aperture blades. |
| **Barrel Clipping** | The self-occlusion of the lens. A higher value creates a cat's eye effect. |
| **Anamorphism** | The amount of vertical stretch of the camera sensor to make the sensor taller or shorter. A higher value increases the stretch of the sensor to simulate an anamorphic look. |
