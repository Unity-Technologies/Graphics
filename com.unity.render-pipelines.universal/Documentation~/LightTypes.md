## Freeform

![Freeform Properties](Images/2D/LightType_Freeform.png)

Select the __Freeform__ Light type to create a Light from an editable polygon with a spline editor. To begin editing your shape, select the Light and find the ![](Images/2D/image_20.png)button in its Inspector window. Select it to enable the shape editing mode.

Add new control points by clicking the mouse along the inner polygon’s outline. Remove control points selecting the point and pressing the Delete key.

The following additional properties are available to the __Freeform__ Light type.

| Property              | Function                                                     |
| --------------------- | ------------------------------------------------------------ |
| __Falloff__           | Adjust the amount the blending from solid to transparent, starting from the center of the shape to its edges. |
| __Falloff Intensity__ | Adjusts the falloff curve of the Light.                      |
| __Falloff Offset__    | Sets the offset for the outer falloff shape.                 |

| ![Light Editing Mode](Images/2D/image_21.png) | ![Light Effect](Images/2D/image_22.png) |
| ------------------------------------------ | ------------------------------------ |
| Freeform Light in edit mode                | Resulting Light Effect               |


When creating a __Freeform__ Light, take care to avoid self-intersection as this may cause unintended lighting results. Self-intersection may occur by creating outlines where edges cross one another, or by enlarging falloff until it overlaps itself. To prevent such issues, it is recommended to edit the shape of the Light until the conditions creating the self-intersection no longer occur.

| ![Freeform Self Intersection](Images/2D/2D_FreeformOutlineIntersection0.png) | ![Freeform Self Intersection](Images/2D/2D_FreeformOutlineIntersection1.png) |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| Outline self-intersection in Edit mode.                      | Light effect with a black triangular artifact                |

| ![Freeform Self Intersection](Images/2D/2D_FreeformFalloffIntersection0.png) | ![Freeform Self Intersection](Images/2D/2D_FreeformFalloffIntersection1.png) |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| Falloff overlap in Edit mode                                 | Light effect with double lighted areas with overlapping falloff |


## Parametric

The __Parametric__ light type has been deprecated. To convert existing Parametric lights to __Freeform__ lights, Edit->Render Pipeline->Universal Render Pipeline->Upgrade Project/Scene Parametric Lights to Freeform


## Sprite

Select the __Sprite__ Light type to create a Light based on a selected Sprite by assigning the selected Sprite to the additional Sprite property.

![The Sprite property](Images/2D/LightType_Sprite.png)

| Property   | Function                             |
| ---------- | ------------------------------------ |
| __Sprite__ | Select a Sprite as the Light source. |


| ![Selected Sprite](Images/2D/image_24.png) | ![Resulting Light effect](Images/2D/image_25.png) |
| --------------------------------------- | ---------------------------------------------- |
| Selected Sprite                         | Resulting Light effect                         |


## Spot

Select the __Spot__ Light type for great control over the angle and direction of the selected Light with the following additional properties.

![Point Light properties](Images/2D/LightType_Point.png)

| Property         | Function                                                     |
| ---------------- | ------------------------------------------------------------ |
| __Radius Inner__ | Set the inner radius here or with the gizmo. Light within the inner radius will be at maximum [intensity](2DLightProperties#Intensity). |
| __Radius Outer__ | Set the outer radius here or with the gizmo. Light intensity decreases to zero as it approaches the outer radius. |
| __Inner / Outer Spot Angle__  | Set the angles with this slider or with the gizmo. Light within the inner and outer angles will be at the intensity specified by inner and outer radius. |


| ![Point Light editing Mode](Images/2D/image_27.png) | ![Resulting light effect](Images/2D/image_28.png) |
| ------------------------------------------------ | ---------------------------------------------- |
| Point Light in Edit mode                         | Resulting Light effect                         |


## Global

Global Lights light all objects on the [targeted sorting layers](2DLightProperties.html#target-sorting-layers). Only one global Light can be used per [Blend Style](LightBlendStyles), and per sorting layer.
