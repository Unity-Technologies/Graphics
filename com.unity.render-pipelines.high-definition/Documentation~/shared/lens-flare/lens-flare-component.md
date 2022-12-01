# Lens Flare (SRP) component

![](../../images/shared/lens-flare/lens-flare-header.png)

Unity’s Scriptable Render Pipeline (SRP) includes the Lens Flare (SRP) component which renders a lens flare in your scene. This is the SRP equivalent of the Built-in Render Pipeline's [Lens Flare](https://docs.unity3d.com/Manual/class-LensFlare.html) component, which is incompatible with SRPs. You can attach a Lens Flare (SRP) component to any GameObject, but some properties only appear when you attach a Lens Flare (SRP) component to a light.

![](../../images/shared/lens-flare/lens-flare-comp.png)

## Creating lens flares in SRP

The Lens Flare (SRP) component controls where the lens flare is as well as properties such as attenuation and whether the lens flare considers occlusion. For properties that define how the lens flare looks, SRP uses the [Lens Flare (SRP) Data](lens-flare-asset.md) asset. Each Lens Flare (SRP) component must reference a Lens Flare (SRP) data asset to display a lens flare on-screen.

To create a lens flare in a scene:

1. Create or select a GameObject to attach the lens flare to.
2. In the Inspector, click **Add Component**.
3. Select **Rendering** > **Lens Flare (SRP)**. Currently, the lens flare doesn't render in the scene because the component doesn't reference a Lens Flare (SRP) Data asset in its **Lens Flare Data** property.
4. Create a new Lens Flare (SRP) Data asset (menu: **Assets** > **Create** > **Lens Flare (SRP)**).
5. In the Lens Flare (SRP) component Inspector, assign the new Lens Flare (SRP) Data asset to the **Lens Flare Data** property.
6. Select the Lens Flare (SRP) Data asset and, in the Inspector, add a new element to the **Elements** list. A default white lens flare now renders at the position of the Lens Flare (SRP) component. For information on how to customize how the lens flare looks, see [Lens Flare (SRP) Data](lens-flare-asset.md).

## Properties

### General

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| Lens Flare Data | Select the [Lens Flare (SRP) Data](lens-flare-asset.md) asset this component controls. |
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
| Volumetric Cloud Occlusion | When enabled, HDRP uses the volumetric cloud texture to occlude the lens flare. HDRP always considers the lens flare to be behind the volumetric clouds because it calculates occlusion in screen space. |
| Occlusion Remap Curve | Specifies the curve used to remap the occlusion of the flare. By default, the occlusion is linear, between 0 and 1. This can be specifically useful to occlude flare more drastically when behind clouds. |
| Volumetric Cloud Occlusion | When enabled, HDRP uses the volumetric cloud texture to occlude the lens flare. HDRP always considers the lens flare to be behind the volumetric clouds because it calculates occlusion in screen space. |
| Allow Off Screen | Enable this property to allow lens flares outside the Camera's view to affect the current field of view. |
