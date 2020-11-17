# Volume Overrides

__Volume Overrides__ are structures which contain values that override the default properties in a [Volume Profile](Volume-Profile.md). The High Definition Render Pipeline (HDRP) uses these Profiles within the [Volume](Volumes.md) framework. For example, you could use a Volume Override in your Unity Project to render a different fog color in a certain area of your Scene.

[Fog](Override-Fog.md) is an example of a Volume Override:

![](Images/Override-VolumetricFog1.png)

Each Volume Override property has a checkbox on its left. Enable the checkbox to make that property editable. This also tells HDRP to use that property for this Volume component rather than the default value. If you disable the checkbox, HDRP ignores the property you set and uses the Volume’s default value for that property instead.

Override checkboxes allow you to override as many or as few values on a Volume component as you want. To quickly toggle all the properties between editable or not, click the __All__ or __None__ shortcuts in the top left of the Volume Override respectively. 

## Using Volume Overrides

To render a different fog color in a certain area of your Scene:

1. Create a global Volume (menu: __GameObject > Volume > Global Volume__).
2. Click the **New** button next to the **Profile** property to add a new Volume Profile to the Volume.
3. Select **Add Override > Fog** and leave it with the default settings.
4. Create a local Volume. To add a **Local** Volume with a box boundary, select __GameObject > Volume > Box Volume__.
5. Select **Add Override > Fog** then in the **Fog** Inspector, override the properties with your preferred values.

Now, whenever your Camera is within the bounds of the local Volume's Collider, HDRP uses the Fog values from that Volume. Whenever your Camera is outside the bounds of the local Volume's Collider, HDRP uses the Fog values from the global Volume
