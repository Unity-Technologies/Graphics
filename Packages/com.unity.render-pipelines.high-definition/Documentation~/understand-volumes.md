# Understand Volumes

The High Definition Render Pipeline (HDRP) uses a Volume framework.

Volumes allow you to partition your Scene into areas so you can control lighting and effects at a finer level, rather than tuning an entire Scene. You can add as many volumes to your Scene as you want, to create different spaces, and then light them all individually for a realistic effect. Each volume has an environment, so you can adjust its sky, fog, and shadow settings. You can also create custom [Volume Profiles](create-a-volume-profile.md) and switch between them.

## Volumes

You can add a __Volume__ component to any GameObject, including a Camera, although it's good practice to create a dedicated GameObject for each Volume. The Volume component itself contains no actual data and instead references a [Volume Profile](create-a-volume-profile.md) which contains the values to interpolate between. The Volume Profile contains default values for every property and hides them by default. To view or alter these properties, you must add [Volume overrides](volume-component.md), which are structures containing overrides for the default values, to the Volume Profile.

A Scene can contain several Volumes so each Volume contains properties that control how it interacts with others in the Scene. **Global** Volumes affect the Camera wherever the Camera is in the Scene and **Local** Volumes affect the Camera if they encapsulate the Camera within the bounds of their Collider.

At runtime, HDRP looks at all the enabled Volumes attached to active GameObjects in the Scene and determines each Volume’s contribution to the final Scene settings. HDRP uses the Camera position and the Volume properties described above to calculate this contribution. It then uses all Volumes with a non-zero contribution to calculate interpolated final values for every property in every Volume override.

**Note**: For Volumes with the same priority, there is no guarantee on the order in which HDRP evaluates them. This means that, depending on creation order, a global Volume can take precedence over a local Volume. The result is that a Camera can go within the bounds of a local Volume but still exclusively use the Volume Override properties from a global Volume in the Scene.

Refer to [Set up a volume](set-up-a-volume.md) for more information.

## Volume Overrides

Volumes can contain different combinations of Volume overrides. For example, one Volume may hold a Physically Based Sky Volume override while other Volumes hold an Exponential Fog Volume override.

__Volume Overrides__ are structures which contain values that override the default properties in a [Volume Profile](create-a-volume-profile.md). The High Definition Render Pipeline (HDRP) uses these Profiles within the Volume framework. For example, you could use a [Fog Volume Override](create-a-global-fog-effect.md) in your Unity Project to render a different fog color in a certain area of your Scene.

Refer to [Configure volume overrides](configure-volume-overrides.md) for more information.

## Volume Profiles

A Volume Profile is a [Scriptable Object](https://docs.unity3d.com/Manual/class-ScriptableObject.html) which contains properties that Volumesuse to determine how to render the Scene environment for Cameras they affect. A Volume references a Volume Profile in its **Profile** field and uses values from the Volume Profile it references.

A Volume Profile organizes its properties into structures which control different environment settings. These structures all have default values that you can use, but you can use [Volume Overrides](volume-component.md) to override these values and customize the environment settings.

Refer to [Create a Volume Profile](create-a-volume-profile.md) for more information.

