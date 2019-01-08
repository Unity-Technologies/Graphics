# Volumes

HDRP’s Volume framework uses local boundaries, that contain parameters HDRP interpolates between to calculate a final value. It allows you to define sets of local and global parameters that HDRP interpolates between depending on the position of the Camera. For example, you can use local Volumes to change environment settings, such as fog color and density, to alter the mood of different areas of your Scene. 

You can add a __Volume__ component to any GameObject, including a Camera, although it is good practice to create a dedicated GameObject for each Volume. The Volume component itself contains hidden default values to interpolate between. To alter them, you must add [Volume Components](Volume-Components.html) which are structures containing overrides for the defaults. 

Volumes also contain parameters that control how they interact with other volumes. A Scene can contain many Volumes. Volumes affect the Camera if they have __IsGlobal __enabled or they encapsulate the Camera within the bounds of their Collider.

![image alt text](Images/Volumes1.png)

| Property| Description |
|:---|:---|
| **Is Global** | Enable this checkbox to apply this Volume to the entire Scene, so the Volume has no boundaries.  |
| **Blend Distance** | The furthest distance from the Volume’s Collider that HDRP starts blending from. A value of 0 means HDRP applies this Volume’s overrides immediately upon entry. |
| **Weight** | The amount of influence the Volume has on the Scene. HDRP applies this multiplier to the value it calculates using the Camera position and Blend Distance.  |
| **Priority** | HDRP uses this value to determine which Volume it uses when Volumes have an equal amount of influence on the Scene. HDRP uses Volumes with higher priorities first. |
| **Profile** | A Volume Profile Asset that contains the Volume Components that store the properties HDRP uses to handle this Volume. |



If your Volume has __IsGlobal__ disabled, you must attach a Trigger Collider to it to define its boundaries. Click the Volume to open it in the Inspector and then select __Add Component > Physics > Box Collider__. On the new __Box Collider__, component, enable __Is Trigger__. To define the boundary of the Volume, adjust the __Size__ field of the Box Collider, and the __Scale__ field of the Transform.  You can use any type of 3D collider, from simple Box Colliders to more complex convex Mesh Colliders. However, for performance reasons, you should use simple colliders because traversing Mesh Colliders with many vertices is resource intensive. Local volumes also have a __Blend Distance__ that represents the outer distance from the Collider surface where HDRP begins to blend the settings for that Volume with the others affecting the Camera.

The __Profile__ field stores a Volume Profile, which is an Asset that contains Volume Components. HDRP creates a Volume Profile with every new __Scene Settings__ GameObject you create. You can change this field by assigning a different Volume Profile. You can also create a Volume Profile or clone the current one by clicking the __New__ and __Clone__ buttons respectively.

At run time, HDRP looks at  all of the enabled Volumes attached to active GameObjects in the Scene and determines each Volume’s contribution to the final Scene settings. HDRP uses the Camera position and the Volume properties described above to calculate this contribution. It then uses all Volumes with a non-zero contribution to calculate interpolated final values for every property in all Volume Components.

Volumes can contain different combinations of Volume Components. For example, one Volume may hold a Procedural Sky Volume Component while other Volumes hold an Exponential Fog Volume Component.

