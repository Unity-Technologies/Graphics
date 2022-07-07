#Volume Profile

A Volume Profile is a [Scriptable Object](https://docs.unity3d.com/Manual/class-ScriptableObject.html) which contains properties that [Volumes](Volumes.md) use to determine how to render the Scene environment for Cameras they affect. A Volume references a Volume Profile in its **Profile** field and uses values from the Volume Profile it references.

A Volume Profile organizes its properties into structures which control different environment settings. These structures all have default values that you can use, but you can use [Volume Overrides](VolumeOverrides.md) to override these values and customize the environment settings.

##Create and custom a Volume Profile

There are multiple ways to create a Volume Profile. Unity creates, and links, a Volume Profile automatically when you create a **Scene Settings** GameObject (menu: **Rendering > Scene Settings**). You can also create a Volume Profile manually. Navigate to menu: **Assets > Create > Volume Profile**.

Open the Volume Profile in the Inspector to edit its properties. To do this, you can either:

&#8226; Select the Volume Profile in the Assets folder.

![](Images/Inspectors/VolumeProfile.png)

&#8226; Select a GameObject with a Volume component that has a Volume Profile set in its **Profile** field.

![](Images/Inspectors/Volume1.png)

When you view the Volume Profile in the Inspector, you can only see values from the Volume overrides that the Volume Profile includes; the Volume Profile hides all other values. You must add Volume override components in order to edit the default properties of the Volume Profile.

To add a Volume override component, click on the **Add Override** button, and select which Volume override you want to add to the Volume Profile.

For example, click on the **Add Override** button and select the **Motion Blur** Volume override. This exposes properties relating to the [Motion Blur](Post-processing-Motion-Blur.md) effect in URP.
