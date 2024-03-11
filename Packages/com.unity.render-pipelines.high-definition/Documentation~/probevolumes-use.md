# Use Probe Volumes

This page provides the basic workflow you need to use Probe Volumes in your project.

## Add and bake a Probe Volume

### Enable Probe Volumes

1. From the main menu, select **Edit** > **Project Settings** > **Quality** > **HDRP**.
2. Expand **Lighting** > **Light Probe Lighting**.
3. Set **Light Probe System** to **Probe Volumes**.
4. Select the **Graphics** > **HDRP Global Settings** tab.
5. Go to **Frame Settings**.
6. Expand **Camera** > **Lighting** and enable **Probe Volumes**.

To make sure Reflection Probes also capture lighting data from Probe Volumes, you should also do the following:

1. Expand **Realtime Reflection** > **Lighting** and enable **Probe Volumes**.
2. Expand **Baked or Custom Reflection** > **Lighting** and enable **Probe Volumes**.

### Add a Probe Volume to the Scene

1. From the main menu, select **GameObject** > **Light** > **Probe Volumes** > **Probe Volume**.
2. In the Inspector for the Probe Volume, set **Mode** to **Global** to make this Probe Volume cover your entire Scene.

### Adjust your Light and Mesh Renderer settings

1. To include a Light in a Probe Volume's baked lighting data, open the Inspector for the Light then set the **Light Mode** to **Mixed** or **Baked**.
2. To include a GameObject in a Probe Volume's baked lighting data, open the Inspector for the GameObject and enable **Contribute Global Illumination**.
3. To make a GameObject receive baked lighting, open the Inspector for the GameObject and set **Receive Global Illumination** to **Light Probes**. 

### Bake your lighting

1. From the main menu, select **Window** > **Rendering** > **Lighting**.
2. Select the **Probe Volumes** panel.
3. Set **Baking Mode** to **Single Scene**.
4. Select **Generate Lighting**.

If no scene in the Baking Set contains a Probe Volume, Unity asks if you want to create a Probe Volume automatically.

You can change baking settings in the Lighting window's [Lightmapping Settings](https://docs.unity3d.com/Documentation/Manual/class-LightingSettings.html#LightmappingSettings).

Refer to [Bake different lighting setups with Probe Volumes](probevolumes-usebakingsets.md) for more information about Baking Sets.

If there are visual artefacts in baked lighting, such as dark blotches or light leaks, refer to [Fix issues with Probe Volumes](probevolumes-fixissues.md).

## Configure a Probe Volume

You can use the following to configure a Probe Volume:

- Use the [Probe Volumes panel](probevolumes-lighting-panel-reference.md) in the Lighting window to change the probe spacing and behaviour in all the Probe Volumes in a Baking Set.
- Use the settings in the [Probe Volume Inspector window](probevolumes-inspector-reference.md) to change the Probe Volume size and probe density.
- Add a [Probe Adjustment Volume component](probevolumes-adjustment-volume-component-reference.md) to the Probe Volume, to make probes invalid in a small area or fix other lighting issues.
- Add a [Volume](understand-volumes.md) to your scene with a [Probe Volumes Options Override](probevolumes-options-override-reference.md), to change the way HDRP samples Probe Volume data when the camera is inside the volume. This doesn't affect baking.

## Additional resources

- [Bake multiple scenes together with Baking Sets](probevolumes-usebakingsets.md)
- [Bake different lighting setups with Lighting Scenarios](probevolumes-bakedifferentlightingsetups.md)
- [Fix issues with Probe Volumes](probevolumes-fixissues.md)
- [Work with multiple Scenes in Unity](https://docs.unity3d.com/Documentation/Manual/MultiSceneEditing.html)
