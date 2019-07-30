## Parametric

Select the __Parametric Light__ type to use a n-sided polygon as the Light.  The following additional properties are available to the __Parametric__ Light type.

![Parametric Light properties](images\image_16.png)



| Property          | Function                                                     |
| ----------------- | ------------------------------------------------------------ |
| Radius            | Set the radius of the Light.                                 |
| Sides             | Set the number of sides of the parametric shape.             |
| Angle Offset      | Set the rotation of the parametric shape.                    |
| Falloff           | Adjust the amount the blending from solid to transparent, starting from the center of the shape to its edges. |
| Falloff Intensity | Adjusts the falloff curve of the Light.                      |
| Falloff Offset    | Sets the offset for the outer falloff shape.                 |

| ![Parametric Light editing mode](images\image_17.png) | ![Resulting Light effect](images\image_18.png) |
| ----------------------------------------------------- | ---------------------------------------------- |
| Parametric Light in edit mode                         | Resulting Light Effect                         |



## Freeform

Select the __Freeform__ Light type to create a Light from an editable polygon with a spline editor. To begin editing your shape, select the Light and find the ![](images\image_20.png)button in its Inspector window. Select it to enable the shape editing mode.

Add new control points by clicking the mouse along the inner polygon’s outline. Remove control points selecting the point and pressing the Delete key.

The following additional properties are available to the __Freeform__ Light type.

![Freeform Properties](images\image_19.png)

| Property          | Function                                                     |
| ----------------- | ------------------------------------------------------------ |
| Falloff           | Adjust the amount the blending from solid to transparent, starting from the center of the shape to its edges. |
| Falloff Intensity | Adjusts the falloff curve of the Light.                      |
| Falloff Offset    | Sets the offset for the outer falloff shape.                 |

| ![Light Editing Mode](images\image_21.png) | ![Light Effect](images\image_22.png) |
| ------------------------------------------ | ------------------------------------ |
| Freeform Light in edit mode                | Resulting Light Effect               |



## Sprite

Select the __Sprite__ Light type to create a Light based on a selected Sprite by assigning the selected Sprite to the additional Sprite property.

![The Sprite property](images\image_23.png)

| Property | Function                             |
| -------- | ------------------------------------ |
| Sprite   | Select a Sprite as the Light source. |



| ![Selected Sprite](images\image_24.png) | ![Resulting Light effect](images\image_25.png) |
| --------------------------------------- | ---------------------------------------------- |
| Selected Sprite                         | Resulting Light effect                         |



## Point

Select the __Point__ Light type for great control over the angle and direction of the selected Light with the following additional properties.

![Point Light properties](images\image_26.png)

| Property     | Function                                                     |
| ------------ | ------------------------------------------------------------ |
| Inner Radius | Set the inner radius here or with the gizmo. Light within the inner radius will be at maximum [intensity](2DLightProperties#Intensity). |
| Outer Radius | Set the outer radius here or with the gizmo. Light intensity decreases to zero as it approaches the outer radius. |
| Inner Angle  | Set the angle with this slider or with the gizmo. Any light within the inner angle will be at the intensity specified by inner and outer radius. |
| Outer Angle  | Set the angle with this slider or with the gizmo. Light intensity decreases to zero as it approaches the outer angle. |

| ![Point Light editing Mode](images\image_27.png) | ![Resulting light effect](images\image_28.png) |
| ------------------------------------------------ | ---------------------------------------------- |
| Point Light in edit mode                         | Resulting Light effect                         |

### Light Cookies

You can assign a Sprite as a Light cookie, which acts as a mask for the intensity of the Light.

| ![Cookie Sprite](images\image_24.png) | ![Resulting Light effect](images\image_25.png) |
| ------------------------------------- | ---------------------------------------------- |
| Selected Sprite as a Light cookie     | Resulting Light effect                         |



## Global

Global Lights light all objects on the [targeted sorting layers](2DLightProperties.html#target-sorting-layers). Only one global Light can be used per [Blend Style](LightBlendStyles), and per sorting layer.
