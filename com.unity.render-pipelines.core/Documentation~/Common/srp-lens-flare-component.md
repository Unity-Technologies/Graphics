# Lens Flare (SRP) Component

![](images/LensFlareHeader.png)

Unity’s Scriptable Render Pipeline (SRP) includes the SRP Lens Flare Override component to control a [Lens Flare (SRP) Data](srp-lens-flare-asset.md) asset. You can attach an Lens Flare (SRP) Component to any GameObject.
Some properties only appear when you attach this component to a light.

![](images/LensFlareComp.png)

## Properties

### General

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Lens Flare Data | Select the [Lens Flare (SRP) Asset](srp-lens-flare-asset.md) asset this component controls. |
| Intensity     | Multiplies the intensity of the lens flare. |
| Scale         | Multiplies the scale of the lens flare. |
| Attenuation by Light Shape | Enable this property to automatically change the appearance of the lens flare based on the type of light you attached this component to.<br/>For example, if this component is attached to a spot light and the camera is looking at this light from behind, the lens flare will not be visible. <br/>This property is only available when this component is attached to a light. |
| Attenuation Distance |The distance between the start and the end of the Attenuation Distance Curve.<br/>This value operates between 0 and 1 in world space.  |
| Attenuation Distance Curve | Fades out the appearance of the lens flare over the distance between the GameObject this asset is attached to, and the Camera. |
| Scale Distance | The distance between the start and the end of the **Scale Distance Curve**.<br/>This value operates between 0 and 1 in world space. |
| Scale Distance Curve | Changes the size of the lens flare over the distance between the GameObject this asset is attached to, and the Camera. |
| Screen Attenuation Curve | Reduces the effect of the lens flare based on its distance from the edge of the screen. You can use this to display a lens flare at the edge of your screen |

### Occlusion

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Enable | Enable this property to partially obscure the lens flare based on the depth buffer |
| Occlusion Radius | Defines how far from the light source Unity occludes the lens flare. This value is in world space. |
| Sample Count | The number of random samples the CPU uses to generate the **Occlusion Radius.** |
| Occlusion Offset | Offsets the plane that the occlusion operates on. A higher value moves this plane closer to Camera. This value is in world space. <br/>For example, if a lens flare is inside the light bulb, you can use this to sample occlusion outside the light bulb. |
| Allow Off Screen | Enable this property to allow lens flares outside the Camera's view to affect the current field of view. |
