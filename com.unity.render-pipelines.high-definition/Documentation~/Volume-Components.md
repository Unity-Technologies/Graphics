# Volume Overrides

__Volume Overrides__ are structures which contain values that override the default properties in a [Volume Profile](Volume-Profile.html). The High Definition Render Pipeline (HDRP) uses these Profiles within the [Volume](Volumes.html) framework. For example, you could use a Volume Override in your Unity Project to render a different fog color in a certain area of your Scene. 

__Exponential Fog__ is an example of a Volume Override:

![](Images/VolumeComponents1.png)

Each Volume Override property has a checkbox on its left. Enable the checkbox to make that property editable. This also tells HDRP to use that property for this Volume component rather than the default value. If you disable the checkbox, HDRP ignores the property and uses the Volume’s default value for that property instead.

Override checkboxes allow you to override as many or as few values on a Volume component as you want. To quickly toggle all the properties between editable or not, click the __All__ or __None__ shortcuts in the top left of the Volume Override respectively. 

As an example, to render a different fog color in a certain area of your Scene:

1. Create a Scene Settings GameObject (menu: __GameObject > Rendering > Scene Settings__).

2. In the Inspector under the Volume component section, enable the __IsGlobal__ property. 

New Scene Settings GameObjects come with some default Volume Overrides, including Exponential Fog. These default Volume Overrides are editable by default. Either leave these settings as default, or override them with your preferred values.

3. Create another Scene Settings GameObject and disable the __IsGlobal__ property. 

4. Add a Collider to this GameObject (open the GameObject in the Inspector, navigate to __Add Component > Physics__ and select one of the 3D Colliders from the list). 

5. Override the values of the Exponential Fog Volume Override. 

Now, whenever your Camera is within the bounds of this Collider, HDRP uses the Exponential Fog values from the Volume Override on the GameObject with that Collider.
