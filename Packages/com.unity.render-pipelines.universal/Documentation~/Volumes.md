# Understand volumes

The Universal Render Pipeline (URP) uses volumes for [post-processing](integration-with-post-processing.md#post-proc-how-to) effects. Volumes can override or extend scene properties depending on the camera position relative to each volume.

You can create the following dedicated volume GameObjects:

- Global Volume
- Box Volume
- Sphere Volume
- Convex Mesh Volume

You can also add a Volume component to any GameObject. A scene can contain multiple GameObjects with Volume components. You can add multiple Volume components to a GameObject.

At runtime, URP goes through all the enabled Volume components attached to active GameObjects in the scene, and determines each volume's contribution to the final scene settings. URP uses the camera position and the Volume component properties to calculate the contribution. URP interpolates values from all volumes with a non-zero contribution to calculate the final property values.

## Global and local volumes

There are two types of volume:

- Global volumes affect the camera everywhere in the scene.
- Local volumes affect the camera only if the camera is near the bounds of the collider on the parent GameObject.

Refer to [Set up a volume](set-up-a-volume.md) for more information.

## Volume Profiles and Volume Overrides

Each Volume component references a Volume Profile, which contains scene properties in one or more Volume Overrides. Each Volume Override controls different settings. 

![Vignette post-processing effect in the URP Template SampleScene](Images/post-proc/post-proc-as-volume-override.png)
A GameObject with a global volume. The Volume Profile has **Vignette** and **Tonemapping** Volume Overrides.

Refer to the following for more information: 

- [Create a Volume Profile](Volume-Profile.md)
- [Configure Volume Overrides](VolumeOverrides.md)

## Default volumes

All URP scenes have two default global volumes:

- The Default Volume for your whole project, which uses the Volume Profile set in **Project Settings** > **Graphics** > **URP** > **Default Volume Profile**.
- The global volume for the active quality level, which uses the Volume Profile set in the active [URP Asset](universalrp-asset.md) > **Volumes** > **Volume Profile**.

URP evaluates the default volumes only when you first load a scene or when you change the [quality level](https://docs.unity3d.com/Manual/class-QualitySettings.html), instead of every frame. If you use only the default volumes in a scene, URP has less work to do at runtime.

URP sets the default volumes to the lowest priority, so any volume you add to a scene overrides them.

Refer to the following for more information:

- [Configure the Default Volume](set-up-a-volume.md#configure-the-default-volume)
- [Configure the global volume for a quality level](set-up-a-volume.md#configure-the-global-volume-for-a-quality-level)
