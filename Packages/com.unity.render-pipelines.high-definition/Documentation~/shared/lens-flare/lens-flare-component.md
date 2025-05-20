# Add lens flares 

![Lens flare example.](../../images/shared/lens-flare/lens-flare-header.png)

Unity’s Scriptable Render Pipeline (SRP) includes the Lens Flare (SRP) component which renders a lens flare in your scene. This is the SRP equivalent of the Built-in Render Pipeline's [Lens Flare](https://docs.unity3d.com/Manual/class-LensFlare.html) component, which is incompatible with SRPs. You can attach a Lens Flare (SRP) component to any GameObject, but some properties only appear when you attach a Lens Flare (SRP) component to a light.

Use the Lens Flare (SRP) component to create lens flares for lights that have specific locations in your scene, for example bright bulbs. You can also create lens flares using the [Screen Space Lens Flare](Override-Screen-Space-Lens-Flare.md) volume override, or use both the Lens Flare (SRP) component and the Screen Space Lens Flare override in the same scene.

## Create a lens flare in SRP

The Lens Flare (SRP) component controls where the lens flare is as well as properties such as attenuation and whether the lens flare considers occlusion. For properties that define how the lens flare looks, SRP uses the [Lens Flare (SRP) Data](lens-flare-asset.md) asset. Each Lens Flare (SRP) component must reference a Lens Flare (SRP) data asset to display a lens flare on-screen.

To create a lens flare in a scene:

1. Create or select a GameObject to attach the lens flare to.
2. In the Inspector, click **Add Component**.
3. Select **Rendering** > **Lens Flare (SRP)**. Currently, the lens flare doesn't render in the scene because the component doesn't reference a Lens Flare (SRP) Data asset in its **Lens Flare Data** property.
4. Create a new Lens Flare (SRP) Data asset (menu: **Assets** > **Create** > **Lens Flare (SRP)**).
5. In the Lens Flare (SRP) component Inspector, assign the new Lens Flare (SRP) Data asset to the **Lens Flare Data** property.
6. Select the Lens Flare (SRP) Data asset and, in the Inspector, add a new element to the **Elements** list. A default white lens flare now renders at the position of the Lens Flare (SRP) component. For information on how to customize how the lens flare looks, see [Lens Flare (SRP) Data](lens-flare-asset.md).

Refer to the following for more information:

- [Lens Flare (SRP) reference](lens-flare-reference.md)
- [Lens Flare (SRP) Data Asset reference](lens-flare-asset.md)
