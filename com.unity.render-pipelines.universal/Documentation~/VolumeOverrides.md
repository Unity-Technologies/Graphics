# Volume Overrides

__Volume Overrides__ are structures which contain values that override the default properties in a [Volume Profile](Volume-Profile.html). The Universal Render Pipeline (URP) uses these Profiles within the [Volume](Volumes.html) framework. For example, you could use a Volume Override in your Unity Project to darken the outside edges of yours Scene. 

![__Vignette__ is an example of a Volume Override.](Images/Inspectors/Vignette.png)

Each Volume Override property has a checkbox on its left. Enable the checkbox to make that property editable. This also tells URP to use that property for this Volume component rather than the default value. If you disable the checkbox, URP ignores the property you set and uses the Volume’s default value for that property instead.

Override checkboxes allow you to override as many or as few values on a Volume component as you want. To quickly toggle all the properties between editable or not, click the __All__ or __None__ shortcuts in the top left of the Volume Override respectively. 

## Using Volume Overrides

To render both a global vignette and a local vignette in a certain area of your Scene:

1. Create a global Volume (menu: __GameObjec > Volume > Global Volume__).
2. Click the __New__ button next to the __Profile__ property to add a new Volume Profile to the Volume.
3. Select __Add Override > Vignette__, and leave it with the default settings.
4. Create a local Volume. To add a **Local** Volume with a box boundary, select __GameObject > Volume > Box Volume__.
5. Select __Add Override > Vignette__.Then, in the __Vignette__ Inspector, override the properties them with your preferred values.

Now, whenever your Camera is within the bounds of the local Volume's Collider, URP uses the __Vignette__ values from that Volume. Whenever your Camera is outside the bounds of the local Volume's Collider, URP uses the __Vignette__ values from the global Volume